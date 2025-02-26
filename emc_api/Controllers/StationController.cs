using Microsoft.AspNetCore.Mvc;
using emc_api.Models;
using emc_api.Repositories;

namespace emc_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StationsController : ControllerBase
    {
        private readonly IBizRepository _repository;

        public StationsController(IBizRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _repository.GetAllStationsAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _repository.GetStationByIdAsync(id);
            return Ok(result);
        }

        [HttpGet("{id}/shortname")]
        public async Task<IActionResult> GetShortName(int id)
        {
            var result = await _repository.GetStationShortNameByIdAsync(id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Station station)
        {
            var result = await _repository.CreateStationAsync(station);
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] Station station)
        {
            var result = await _repository.UpdateStationAsync(station);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _repository.DeleteStationAsync(id);
            return Ok(result);
        }
    }
}
