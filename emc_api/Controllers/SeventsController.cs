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
            return Ok(result);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _repository.GetSeventByIdAsync(id);
            return Ok(result);
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