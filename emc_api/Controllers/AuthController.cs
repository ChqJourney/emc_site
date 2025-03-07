using Microsoft.AspNetCore.Mvc;
using emc_api.Models;
using emc_api.Repositories;
using emc_api.Middleware;

namespace emc_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _auth = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        [Consumes("application/json")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation($"Attempting login for user: {dto.UserName}");

            try 
            {
                var user = await _auth.ValidateUserAsync(dto.UserName, dto.Password);
                if (user != null)
                {
                    return Ok(user);
                }
                return Unauthorized(new { message = "Invalid username or password" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed");
                return StatusCode(500, new { message = "Internal server error" });
            }
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

            try 
            {
                var user = await _auth.ValidateUserAsync(dto.UserName, dto.Password);
                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid username or password" });
                }
                await _auth.ChangePasswordAsync(dto.UserName, dto.NewPassword);
                return Ok(new { message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
    public class LoginDto{
        public string UserName { get; set; }
        public string Password { get; set; }
    }
    public class ChangePasswordDto{
        public string UserName { get; set; }
        public string Password { get; set; }
        public string NewPassword { get; set; }
    }
}