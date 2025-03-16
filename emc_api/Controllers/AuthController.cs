using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using emc_api.Services;

namespace emc_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepo;
        private readonly ILoggerService _loggerService;

        public AuthController(IUserRepository userRepo, IConfiguration configuration, ILogger<AuthController> logger, ILoggerService loggerService)
        {
            _logger = logger;
            _configuration = configuration;
            _userRepo=userRepo;
            _loggerService = loggerService;
        }

        
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginRequest request)
        {
            try
            {
                await _loggerService.LogInformationAsync($"用户登录尝试: {request.username}");
                
                var user = await _userRepo.GetByUserNameAsync(request.username);

                if (user == null || !BCrypt.Net.BCrypt.Verify(request.password, user.PasswordHash))
                {
                    await _loggerService.LogWarningAsync($"用户登录失败: {request.username} - 凭据无效");
                    return Unauthorized("Invalid credentials");
                }
                
                var token = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = ((DateTimeOffset)DateTime.UtcNow.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays"))).ToUnixTimeMilliseconds();
                // _logger.LogInformation(user.RefreshTokenExpiryTime.ToString());
                await _userRepo.UpdateRefreshTokenAsync(user.Id, refreshToken, user.RefreshTokenExpiryTime);
                
                await _loggerService.LogInformationAsync($"用户登录成功: {user.UserName}, 角色: {user.Role}");
                
                return Ok(new
                {
                    AccessToken = token,
                    RefreshToken = refreshToken,
                    ExpiresIn=((DateTimeOffset)DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes"))).ToUnixTimeMilliseconds()
                });
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"用户登录过程中发生错误: {request.username}, 错误: {ex.Message}", ex);
                return StatusCode(500, "登录过程中发生错误，请稍后再试");
            }
        }
        [Authorize]
        [HttpPost("me")]
        public async Task<IActionResult> Me()
        {
            try
            {
                var username = User.Identity?.Name;
                await _loggerService.LogInformationAsync($"用户信息请求: {username}");
                
                var user = await _userRepo.GetByUserNameAsync(username);
                
                if (user == null)
                {
                    await _loggerService.LogWarningAsync($"用户信息请求失败: 未找到用户 {username}");
                    return NotFound($"未找到用户信息");
                }
                
                await _loggerService.LogInformationAsync($"用户信息已返回: {username}");
                
                return Ok(new
                {
                    username=user.UserName,
                    role=user.Role,
                    machinename=user.MachineName,
                    englishname=user.FullName,
                    team=user.Team
                });
            }
            catch (Exception ex)
            {
                var username = User.Identity?.Name ?? "未知用户";
                await _loggerService.LogErrorAsync($"获取用户信息时发生错误: {username}, 错误: {ex.Message}", ex);
                return StatusCode(500, "获取用户信息时发生错误");
            }
        }
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(TokenModel tokenModel)
        {
            try
            {
                // _logger.LogInformation($"Refresh token attempt for token: {tokenModel.accessToken?.Substring(0, 20)}...");
                var principal = GetPrincipalFromExpiredToken(tokenModel.accessToken);
                var username = principal?.Identity?.Name;
                
                await _loggerService.LogInformationAsync($"刷新令牌尝试: {username}");
                
                if(username==null){
                    await _loggerService.LogWarningAsync($"刷新令牌失败: 无效令牌");
                    return BadRequest("Invalid token");
                }
                var user = await _userRepo.GetByUserNameAsync(username);
                // _logger.LogInformation(user?.RefreshToken??"no refresh token in database");
                // _logger.LogInformation(tokenModel.refreshToken);
                // _logger.LogInformation(user?.RefreshTokenExpiryTime.ToString()??"no refresh token expiry time in database");
                // _logger.LogInformation(((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds().ToString());
                if (user == null || user.RefreshToken != tokenModel.refreshToken ||
                    user.RefreshTokenExpiryTime <= ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds()){

                    await _loggerService.LogWarningAsync($"刷新令牌失败: {username} - 令牌已过期或无效");
                    return BadRequest("Invalid token");
                    }
                // _logger.LogInformation("Token is valid");
                var newAccessToken = GenerateJwtToken(user);
                var newRefreshToken = GenerateRefreshToken();
                var refreshTokenExpiryTime=((DateTimeOffset)DateTime.UtcNow.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays"))).ToUnixTimeMilliseconds();
                var accessTokenExpiraryTime=((DateTimeOffset)DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes"))).ToUnixTimeMilliseconds();
                await _userRepo.UpdateRefreshTokenAsync(user.Id, newRefreshToken, refreshTokenExpiryTime);
                
                await _loggerService.LogInformationAsync($"令牌刷新成功: {username}");
                
                return Ok(new
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresIn=accessTokenExpiraryTime
                });
            }
            catch (SecurityTokenException ex)
            {
                await _loggerService.LogErrorAsync($"刷新令牌时发生安全令牌错误: {ex.Message}", ex);
                return BadRequest("无效的令牌格式");
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"刷新令牌时发生错误: {ex.Message}", ex);
                return StatusCode(500, "刷新令牌时发生错误");
            }
        }
        // [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(TokenModel tokenModel)
        {
            try
            {
                var principal = GetPrincipalFromExpiredToken(tokenModel.accessToken);
                var username = principal?.Identity?.Name;
                
                await _loggerService.LogInformationAsync($"用户注销请求: {username}");
                
                if(username==null){
                    await _loggerService.LogWarningAsync($"注销失败: 无效令牌");
                    return BadRequest("Invalid token");
                }
                var user = await _userRepo.GetByUserNameAsync(username);

                if (user == null || user.RefreshToken != tokenModel.refreshToken ||
                    user.RefreshTokenExpiryTime <= ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds()){

                    await _loggerService.LogWarningAsync($"注销失败: {username} - 令牌已过期或无效");
                    return BadRequest("Invalid token");
                    }
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = 0;
                await _userRepo.UpdateRefreshTokenAsync(user.Id, "", 0);
                
                await _loggerService.LogInformationAsync($"用户已成功注销: {username}");
                
                return Ok();
            }
            catch (SecurityTokenException ex)
            {
                await _loggerService.LogErrorAsync($"注销时发生安全令牌错误: {ex.Message}", ex);
                return BadRequest("无效的令牌格式");
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"注销时发生错误: {ex.Message}", ex);
                return StatusCode(500, "注销时发生错误");
            }
        }
        
        [Authorize(Roles = "Admin")]
        [HttpGet("list")]
        public async Task<IActionResult> List()
        {
            try
            {
                var username = User.Identity?.Name;
                await _loggerService.LogInformationAsync($"管理员 {username} 请求用户列表");
                
                var users = await _userRepo.GetAllUsersAsync();
                
                await _loggerService.LogInformationAsync($"已返回 {users.Count()} 个用户的列表");
                
                return Ok(users);
            }
            catch (Exception ex)
            {
                var username = User.Identity?.Name ?? "未知管理员";
                await _loggerService.LogErrorAsync($"获取用户列表时发生错误: 管理员 {username}, 错误: {ex.Message}", ex);
                return StatusCode(500, "获取用户列表时发生错误");
            }
        }
        [HttpPost("create")]
        public async Task<IActionResult> Create(UserDto user)
        {
            try
            {
                var requestingUser = User.Identity?.Name ?? "系统";
                await _loggerService.LogInformationAsync($"用户创建请求: {requestingUser} 尝试创建用户 {user.username}");
                
                if (user == null || string.IsNullOrEmpty(user.username) || string.IsNullOrEmpty(user.machinename))
                {
                    await _loggerService.LogWarningAsync($"用户创建失败: 请求数据无效");
                    return BadRequest("请求数据无效");
                }
                
                var existed=await _userRepo.GetByUserNameAsync(user.username);
                if(existed!=null){
                    await _loggerService.LogWarningAsync($"用户创建失败: {user.username} 已存在");
                    return BadRequest("user existed already");
                }
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(user.machinename);
                await _userRepo.CreateUserAsync(user,passwordHash);
                
                await _loggerService.LogInformationAsync($"用户创建成功: {user.username}, 角色: {user.role}, 创建者: {requestingUser}");
                
                return Ok();
            }
            catch (Exception ex)
            {
                var requestingUser = User.Identity?.Name ?? "系统";
                await _loggerService.LogErrorAsync($"创建用户时发生错误: {requestingUser} 尝试创建用户 {user?.username}, 错误: {ex.Message}", ex);
                return StatusCode(500, "创建用户时发生错误");
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("remove/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var adminUser = User.Identity?.Name;
                await _loggerService.LogInformationAsync($"用户删除请求: 管理员 {adminUser} 尝试删除用户ID {id}");
                
                await _userRepo.RemoveUserAsync(id);
                
                await _loggerService.LogInformationAsync($"用户删除成功: ID {id}, 操作者: {adminUser}");
                
                return Ok();
            }
            catch (Exception ex)
            {
                var adminUser = User.Identity?.Name ?? "未知管理员";
                await _loggerService.LogErrorAsync($"删除用户时发生错误: 管理员 {adminUser} 尝试删除用户ID {id}, 错误: {ex.Message}", ex);
                return StatusCode(500, "删除用户时发生错误");
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(string username)
        {
            try
            {
                var adminUser = User.Identity?.Name;
                await _loggerService.LogInformationAsync($"密码重置请求: 管理员 {adminUser} 尝试重置用户 {username} 的密码");
                
                if (string.IsNullOrEmpty(username))
                {
                    await _loggerService.LogWarningAsync($"密码重置失败: 用户名为空");
                    return BadRequest("用户名不能为空");
                }
                
                var user = await _userRepo.GetByUserNameAsync(username);
                if (user == null)
                {
                    await _loggerService.LogWarningAsync($"密码重置失败: 用户 {username} 不存在");
                    return NotFound("User not found");
                }
                
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(user.MachineName);
                await _userRepo.UpdatePasswordAsync(user.Id, passwordHash);
                
                await _loggerService.LogInformationAsync($"密码重置成功: 用户 {username}, 操作者: {adminUser}");
                
                return Ok();
            }
            catch (Exception ex)
            {
                var adminUser = User.Identity?.Name ?? "未知管理员";
                await _loggerService.LogErrorAsync($"重置密码时发生错误: 管理员 {adminUser} 尝试重置用户 {username} 的密码, 错误: {ex.Message}", ex);
                return StatusCode(500, "重置密码时发生错误");
            }
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
        [Authorize]
        [HttpPost("change-pwd")]
        [Consumes("application/json")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await _loggerService.LogWarningAsync($"密码修改失败: 请求模型无效 {dto.UserName}");
                    return BadRequest(ModelState);
                }

                _logger.LogInformation($"Attempting change password for user: {dto.UserName}");
                await _loggerService.LogInformationAsync($"密码修改请求: 用户 {dto.UserName}");
                
                var user=await _userRepo.GetByUserNameAsync(dto.UserName);
                
                if (user == null)
                {
                    await _loggerService.LogWarningAsync($"密码修改失败: 用户 {dto.UserName} 不存在");
                    return NotFound("用户不存在");
                }
                
                if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash)){
                    await _loggerService.LogWarningAsync($"密码修改失败: 用户 {dto.UserName} 凭据无效");
                    return Unauthorized("Invalid credentials");
                }
                
                if (string.IsNullOrEmpty(dto.NewPassword))
                {
                    await _loggerService.LogWarningAsync($"密码修改失败: 用户 {dto.UserName} 新密码为空");
                    return BadRequest("新密码不能为空");
                }
                
                var newHash=BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                await _userRepo.UpdatePasswordAsync(user.Id, newHash);
                
                await _loggerService.LogInformationAsync($"密码修改成功: 用户 {dto.UserName}");
                
                return Ok();
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"修改密码时发生错误: 用户 {dto?.UserName}, 错误: {ex.Message}", ex);
                return StatusCode(500, "修改密码时发生错误");
            }
        }
    }
    public record UserRegisterRequest(string Username, string Password, string Role);
    public record UserLoginRequest(string username, string password);
    public record TokenModel(string accessToken, string refreshToken);
    public record ChangePasswordDto(string UserName, string Password, string NewPassword);
    public record ControlledUser(string username,string machinename,string role,string team,string englishname);

}