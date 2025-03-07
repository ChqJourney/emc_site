using Microsoft.AspNetCore.Mvc;
using emc_api.Models;
using emc_api.Repositories;

namespace emc_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _repository;

        public UserController(IUserRepository repository)
        {
            _repository = repository;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UserDto dto)
        {
            var user = await _repository.GetUserAsync(dto.UserName, dto.MachineName);
            if (user != null)
            {
                return BadRequest("User already exists");
            }

            dto.LoginAt = DateTime.Now;
            var newUser = await _repository.SetUserAsync(dto);
            if (newUser != null)
            {
                return Ok(newUser);
            }
            
            return Problem("Failed to create user");
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromHeader(Name = "username")] string username, [FromHeader(Name = "machinename")] string machinename)
        {
            var user = await _repository.GetUserAsync(username, machinename);
            return user is null ? NotFound("User not found") : Ok(user);
        }
        
    }
}
