using Microsoft.AspNetCore.Mvc;
using emc_api.Models;
using emc_api.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace emc_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationsController : ControllerBase
    {
        private readonly IBizRepository _repository;

        public ReservationsController(IBizRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("{date}")]
        public async Task<IActionResult> GetByDate(string date)
        {
            var result = await _repository.GetReservationsByDateAsync(date);
            return Ok(result);
        }

        [HttpGet("month/{month}")]
        public async Task<IActionResult> GetByMonth(string month, [FromQuery] string? projectEngineer)
        {
            var result = await _repository.GetReservationsByMonthAsync(month, projectEngineer);
            return Ok(result);
        }

        [HttpGet("year/{year}")]
        public async Task<IActionResult> GetByYear(string year, [FromQuery] string? projectEngineer)
        {
            var result = await _repository.GetReservationsByYearAsync(year, projectEngineer);
            return Ok(result);
        }
        [HttpGet("station/{id}/{month}")]
        public async Task<IActionResult> GetByStationAndMonth([FromQuery] int id, [FromQuery] string month)
        {
            var result = await _repository.GetReservationsByStationAndMonthAsync(id, month);
            return Ok(result);
        }
        [Authorize(Roles = "Admin,Engineer")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string timeRange, [FromQuery] string? projectEngineer, [FromQuery] string? createdBy,
            [FromQuery] int? pageNumber = null, [FromQuery] int? pageSize = null)
        {
            try
            {
                // 验证时间范围参数是否有效
                if (string.IsNullOrWhiteSpace(timeRange))
                {
                    return BadRequest("timeRange参数是必需的");
                }
                
                // 如果提供了分页参数，使用分页方法
                if (pageNumber.HasValue && pageSize.HasValue)
                {
                    // 验证分页参数
                    if (pageNumber.Value < 1)
                    {
                        return BadRequest("pageNumber必须大于0");
                    }
                    
                    if (pageSize.Value < 1)
                    {
                        return BadRequest("pageSize必须大于0");
                    }
                    
                    var paginatedResult = await _repository.GetPaginatedReservationsAsync(
                        timeRange, projectEngineer, createdBy, pageNumber.Value, pageSize.Value);
                    return Ok(paginatedResult);
                }
                
                // 否则返回所有数据
                var result = await _repository.GetAllReservationsAsync(timeRange, projectEngineer, createdBy);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // 记录异常
                return StatusCode(500, $"获取预约列表时发生错误: {ex.Message}");
            }
        }

        [Authorize(Roles = "Admin,Engineer")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Reservation reservation)
        {
            var result = await _repository.CreateReservationsAsync(reservation);
            return Ok(result);
        }

        [Authorize(Roles = "Admin,Engineer")]
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] Reservation reservation)
        {
            var result = await _repository.UpdateReservationAsync(reservation);
            return Ok(result);
        }
        [Authorize(Roles = "Admin,Engineer")]
        [HttpGet("station_status")]
        public async Task<IActionResult> GetStationStatus([FromQuery] int id, [FromQuery] string date){
            var result = await _repository.GetStationStatusPerDateAsync(id, date);
            return Ok(result);
        }
        [Authorize(Roles = "Admin,Engineer")]
        [HttpGet("station/{id}")]
        public async Task<IActionResult> GetStationById(int id)
        {
            var result = await _repository.GetStationByIdAsync(id);
            return Ok(result);
        }

        [Authorize(Roles = "Admin,Engineer")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _repository.DeleteReservationAsync(id);
            return Ok(result);
        }
    }
}
