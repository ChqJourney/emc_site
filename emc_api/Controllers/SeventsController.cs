using Microsoft.AspNetCore.Mvc;
using emc_api.Models;
using emc_api.Repositories;
using emc_api.Services;

namespace emc_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeventsController : ControllerBase
    {
        private readonly IBizRepository _repository;
        private readonly ILoggerService _logger;

        public SeventsController(IBizRepository repository, ILoggerService logger)
        {
            _repository = repository;
            _logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _repository.GetAllSeventsAsync();
            // await _logger.LogInformationAsync(result.ToString());
            return result==null?NotFound("No events found"):Ok(result);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _repository.GetSeventByIdAsync(id);
            return result==null?Ok(new List<SeventDto>()) :Ok(result);
        }
        [HttpGet("station/{id}")]
        public async Task<IActionResult> GetByStation(int id)
        {
            var result = await _repository.GetSeventsByStationIdAsync(id);
            return Ok(result); // 直接返回Ok，因为Repository已保证即使为空也返回空集合
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SeventDto sevent)
        {
            var result = await _repository.CreateSeventAsync(sevent);
            return Ok(result);
        }
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] SeventDto sevent)
        {
            var result = await _repository.UpdateSeventAsync(sevent);
            return Ok(result);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _repository.DeleteSeventAsync(id);
            return Ok(result);
        }
    }
}