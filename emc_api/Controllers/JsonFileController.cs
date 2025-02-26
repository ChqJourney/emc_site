 using Microsoft.AspNetCore.Mvc;
using emc_api.Models;
using emc_api.Repositories;
using System.Threading.Tasks;

namespace emc_api.Controllers
{
    [ApiController]
    [Route("api/general")]
    public class JsonFileController : ControllerBase
    {
        private readonly IBizRepository _repository;

        public JsonFileController(IBizRepository repository)
        {
            _repository = repository;
        }
        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            var result =await _repository.GetSettingsAsync();
            Console.WriteLine("apidddd");
            Console.WriteLine(result);
            return Ok(result);
        }
    }
}