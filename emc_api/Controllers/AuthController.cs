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

namespace emc_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepo;

        public AuthController(IAuthService authService,IUserRepository userRepo, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _auth = authService;
            _logger = logger;
            _configuration = configuration;
            _userRepo=userRepo;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterRequest request)
        {
            // 实现用户注册逻辑，包括密码哈希存储
            var user = new User
            {
                UserName = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role
            };
            await _userRepo.CreateUserAsync(user);
            return Ok();
        }
        
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginRequest request)
        {
            var user = await _userRepo.GetByUserNameAsync(request.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials");
            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays"));
            await _userRepo.UpdateRefreshTokenAsync(user.Id, refreshToken, user.RefreshTokenExpiryTime);
            return Ok(new
            {
                AccessToken = token,
                RefreshToken = refreshToken
            });
        }
        
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(TokenModel tokenModel)
        {
            var principal = GetPrincipalFromExpiredToken(tokenModel.AccessToken);
            var username = principal.Identity.Name;

            var user = await _userRepo.GetByUserNameAsync(username);

            if (user == null || user.RefreshToken != tokenModel.RefreshToken ||
                user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return BadRequest("Invalid token");
            var newAccessToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();
            user.RefreshToken = newRefreshToken;
            await _userRepo.UpdateRefreshTokenAsync(user.Id, newRefreshToken, user.RefreshTokenExpiryTime);
            return Ok(new
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }
        
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(TokenModel tokenModel)
        {
            var principal = GetPrincipalFromExpiredToken(tokenModel.AccessToken);
            var username = principal.Identity.Name;

            var user = await _userRepo.GetByUserNameAsync(username);

            if (user == null || user.RefreshToken != tokenModel.RefreshToken ||
                user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return BadRequest("Invalid token");
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _userRepo.UpdateRefreshTokenAsync(user.Id, null, null);
            return Ok();
        }
        
        // [Authorize(Roles = "Admin")]
        [HttpGet("list")]
        public async Task<IActionResult> List()
        {
            return Ok(await _userRepo.GetAllUsersAsync());
        }
        
        
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(string username)
        {
            var user = await _userRepo.GetByUserNameAsync(username);
            if (user == null)
                return NotFound("User not found");
            var userList=await _auth.GetUsersAsync();
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
            new Claim(ClaimTypes.Role, user.Role)
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

        [HttpPost("changepwd")]
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
    public record UserLoginRequest(string Username, string Password);
    public record TokenModel(string AccessToken, string RefreshToken);
    public record ChangePasswordDto(string UserName, string Password, string NewPassword);

}