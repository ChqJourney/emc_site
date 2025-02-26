using Microsoft.AspNetCore.Mvc;
using emc_api.Repositories;

namespace emc_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IBizRepository _repository;

        public HealthController(IBizRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> Check()
        {
            var result = await _repository.CheckDatabaseConnectionAsync();
            return result ? Ok("Database connection is successful") : NotFound("Database connection failed");
        }
    }
}
