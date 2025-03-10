using Microsoft.AspNetCore.Mvc;
using emc_api.Models;
using emc_api.Repositories;
using emc_api.Middleware;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace emc_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepo;

        public AuthController(IUserRepository userRepo, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _logger = logger;
            _configuration = configuration;
            _userRepo=userRepo;
        }

        // [HttpPost("register")]
        // public async Task<IActionResult> Register(UserRegisterRequest request)
        // {
        //     // 实现用户注册逻辑，包括密码哈希存储
        //     var user = new UserDto
        //     {
        //         UserName = request.Username,
        //         PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        //         Role = request.Role
        //     };
        //     await _userRepo.CreateUserAsync(user);
        //     return Ok();
        // }
        
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginRequest request)
        {
            var user = await _userRepo.GetByUserNameAsync(request.username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.password, user.PasswordHash))
                return Unauthorized("Invalid credentials");
            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = ((DateTimeOffset)DateTime.UtcNow.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays"))).ToUnixTimeMilliseconds();
            _logger.LogInformation(user.RefreshTokenExpiryTime.ToString());
            await _userRepo.UpdateRefreshTokenAsync(user.Id, refreshToken, user.RefreshTokenExpiryTime);
            return Ok(new
            {
                AccessToken = token,
                RefreshToken = refreshToken,
                ExpiresIn=((DateTimeOffset)DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes"))).ToUnixTimeMilliseconds()
            });
        }
        [Authorize]
        [HttpPost("me")]
        public async Task<IActionResult> Me()
        {
            var user = await _userRepo.GetByUserNameAsync(User.Identity.Name);
            return Ok(new
            {
                username=user.UserName,
                role=user.Role,
                machinename=user.MachineName,
                englishname=user.FullName,
                team=user.Team
            });
        }
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(TokenModel tokenModel)
        {
            _logger.LogInformation($"Refresh token attempt for token: {tokenModel.accessToken?.Substring(0, 20)}...");
            var principal = GetPrincipalFromExpiredToken(tokenModel.accessToken);
            var username = principal.Identity.Name;

            var user = await _userRepo.GetByUserNameAsync(username);
            _logger.LogInformation(user.RefreshToken);
            _logger.LogInformation(tokenModel.refreshToken);
            _logger.LogInformation(user.RefreshTokenExpiryTime.ToString());
            _logger.LogInformation(((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds().ToString());
            if (user == null || user.RefreshToken != tokenModel.refreshToken ||
                user.RefreshTokenExpiryTime <= ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds()){

                return BadRequest("Invalid token");
                }
            _logger.LogInformation("Token is valid");
            var newAccessToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();
            var refreshTokenExpiryTime=((DateTimeOffset)DateTime.UtcNow.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays"))).ToUnixTimeMilliseconds();
            var accessTokenExpiraryTime=((DateTimeOffset)DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes"))).ToUnixTimeMilliseconds();
            await _userRepo.UpdateRefreshTokenAsync(user.Id, newRefreshToken, refreshTokenExpiryTime);
            return Ok(new
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn=accessTokenExpiraryTime
            });
        }
        
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(TokenModel tokenModel)
        {
            var principal = GetPrincipalFromExpiredToken(tokenModel.accessToken);
            var username = principal.Identity.Name;

            var user = await _userRepo.GetByUserNameAsync(username);

            if (user == null || user.RefreshToken != tokenModel.refreshToken ||
                user.RefreshTokenExpiryTime <= ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds()){

                return BadRequest("Invalid token");
                }
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = 0;
            await _userRepo.UpdateRefreshTokenAsync(user.Id, "", 0);
            return Ok();
        }
        
        // [Authorize(Roles = "Admin")]
        [HttpGet("list")]
        public async Task<IActionResult> List()
        {
            return Ok(await _userRepo.GetAllUsersAsync());
        }
        [HttpPost("create")]
        public async Task<IActionResult> Create(UserDto user)
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(user.machinename);
            await _userRepo.CreateUserAsync(user,passwordHash);
            return Ok();
        }
        
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(string username)
        {
            var user = await _userRepo.GetByUserNameAsync(username);
            if (user == null)
                return NotFound("User not found");
            var userList=new List<ControlledUser>();
            if (userList.Count == 0){
                throw new Exception("controlled user listed empty or error");
            }
            var defaultPwd=userList.FirstOrDefault(u=>u.username==username)?.machinename;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPwd);
            await _userRepo.UpdatePasswordAsync(user.Id, user.PasswordHash);
            return Ok();
        }
        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("englishname", user.FullName),
            new Claim("team", user.Team)
        };
            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes")),
                signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        private static string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }
        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
                ValidateLifetime = false
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");
            return principal;
        }
        // [Authorize]
        [HttpPost("change-pwd")]
        [Consumes("application/json")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation($"Attempting change password for user: {dto.UserName}");
            var user=await _userRepo.GetByUserNameAsync(dto.UserName);
            
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash)){

                return Unauthorized("Invalid credentials");
            }
            var newHash=BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _userRepo.UpdatePasswordAsync(user.Id, newHash);
            return Ok();
        }
    }
    public record UserRegisterRequest(string Username, string Password, string Role);
    public record UserLoginRequest(string username, string password);
    public record TokenModel(string accessToken, string refreshToken);
    public record ChangePasswordDto(string UserName, string Password, string NewPassword);
    public record ControlledUser(string username,string machinename,string role,string team,string englishname);

}