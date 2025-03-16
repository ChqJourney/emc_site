using Microsoft.AspNetCore.Mvc;
using emc_api.Models;
using emc_api.Repositories;
using Microsoft.AspNetCore.Authorization;
using emc_api.Services;

namespace emc_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationsController : ControllerBase
    {
        private readonly IBizRepository _repository;
        private readonly ILoggerService _logger;

        public ReservationsController(IBizRepository repository, ILoggerService logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpGet("{date}")]
        public async Task<IActionResult> GetByDate(string date)
        {
            try
            {
                var username = User.Identity?.Name ?? "匿名用户";
                await _logger.LogInformationAsync($"用户 {username} 获取日期 {date} 的预约");
                
                if (string.IsNullOrEmpty(date))
                {
                    await _logger.LogWarningAsync($"获取预约失败: 日期参数为空");
                    return BadRequest("日期参数不能为空");
                }
                
                var result = await _repository.GetReservationsByDateAsync(date);
                
                await _logger.LogInformationAsync($"返回日期 {date} 的预约数据，共 {result.Count()} 条");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                var username = User.Identity?.Name ?? "匿名用户";
                await _logger.LogErrorAsync($"获取日期预约时发生错误: 用户 {username}, 日期: {date}, 错误: {ex.Message}", ex);
                return StatusCode(500, "获取预约数据时发生错误");
            }
        }

        [HttpGet("month/{month}")]
        public async Task<IActionResult> GetByMonth(string month, [FromQuery] string? projectEngineer)
        {
            try
            {
                var username = User.Identity?.Name ?? "匿名用户";
                await _logger.LogInformationAsync($"用户 {username} 获取月份 {month} 的预约，工程师筛选: {projectEngineer ?? "无"}");
                
                if (string.IsNullOrEmpty(month))
                {
                    await _logger.LogWarningAsync($"获取预约失败: 月份参数为空");
                    return BadRequest("月份参数不能为空");
                }
                
                var result = await _repository.GetReservationsByMonthAsync(month, projectEngineer);
                
                await _logger.LogInformationAsync($"返回月份 {month} 的预约数据，共 {result.Count()} 条");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                var username = User.Identity?.Name ?? "匿名用户";
                await _logger.LogErrorAsync($"获取月份预约时发生错误: 用户 {username}, 月份: {month}, 工程师: {projectEngineer}, 错误: {ex.Message}", ex);
                return StatusCode(500, "获取预约数据时发生错误");
            }
        }

        [HttpGet("year/{year}")]
        public async Task<IActionResult> GetByYear(string year, [FromQuery] string? projectEngineer)
        {
            try
            {
                var username = User.Identity?.Name ?? "匿名用户";
                await _logger.LogInformationAsync($"用户 {username} 获取年份 {year} 的预约，工程师筛选: {projectEngineer ?? "无"}");
                
                if (string.IsNullOrEmpty(year))
                {
                    await _logger.LogWarningAsync($"获取预约失败: 年份参数为空");
                    return BadRequest("年份参数不能为空");
                }
                
                var result = await _repository.GetReservationsByYearAsync(year, projectEngineer);
                
                await _logger.LogInformationAsync($"返回年份 {year} 的预约数据，共 {result.Count()} 条");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                var username = User.Identity?.Name ?? "匿名用户";
                await _logger.LogErrorAsync($"获取年份预约时发生错误: 用户 {username}, 年份: {year}, 工程师: {projectEngineer}, 错误: {ex.Message}", ex);
                return StatusCode(500, "获取预约数据时发生错误");
            }
        }
        [HttpGet("station")]
        public async Task<IActionResult> GetByStationAndMonth([FromQuery] int id, [FromQuery] string month)
        {
            try
            {
                var username = User.Identity?.Name ?? "匿名用户";
                await _logger.LogInformationAsync($"用户 {username} 获取工作站 {id} 在月份 {month} 的预约");
                
                if (id <= 0)
                {
                    await _logger.LogWarningAsync($"获取工作站预约失败: 工作站ID {id} 无效");
                    return BadRequest("工作站ID无效");
                }
                
                if (string.IsNullOrEmpty(month))
                {
                    await _logger.LogWarningAsync($"获取工作站预约失败: 月份参数为空");
                    return BadRequest("月份参数不能为空");
                }
                
                var result = await _repository.GetReservationsByStationAndMonthAsync(id, month);
                
                await _logger.LogInformationAsync($"返回工作站 {id} 在月份 {month} 的预约数据，共 {result.Count()} 条");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                var username = User.Identity?.Name ?? "匿名用户";
                await _logger.LogErrorAsync($"获取工作站月份预约时发生错误: 用户 {username}, 工作站ID: {id}, 月份: {month}, 错误: {ex.Message}", ex);
                return StatusCode(500, "获取预约数据时发生错误");
            }
        }

        /*
        设置页面获取所有预约列表
        timeRange: 时间范围，格式为"YYYY-MM-DD"
        projectEngineer: 项目工程师，格式为"YYYY-MM-DD"
        reservatBy: 预约人，格式为"YYYY-MM-DD"
        pageNumber: 页码，格式为"YYYY-MM-DD"
        pageSize: 每页大小，格式为"YYYY-MM-DD"
        */
        [Authorize(Roles = "Admin,Engineer")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string timeRange, [FromQuery] string? projectEngineer, [FromQuery] string? reservatBy,
            [FromQuery] int? pageNumber = null, [FromQuery] int? pageSize = null)
        {
            try
            {
                var username = User.Identity?.Name;
                await _logger.LogInformationAsync($"用户 {username} 获取预约列表，时间范围: {timeRange}，工程师: {projectEngineer ?? "无"}，预约人: {reservatBy ?? "无"}，分页: {(pageNumber.HasValue ? $"{pageNumber}/{pageSize}" : "无")}");
                
                // 验证时间范围参数是否有效
                if (string.IsNullOrWhiteSpace(timeRange))
                {
                    await _logger.LogWarningAsync($"获取预约列表失败: 用户 {username} 未提供必要的时间范围参数");
                    return BadRequest("timeRange参数是必需的");
                }
                
                // 如果提供了分页参数，使用分页方法
                if (pageNumber.HasValue && pageSize.HasValue)
                {
                    // 验证分页参数
                    if (pageNumber.Value < 1)
                    {
                        await _logger.LogWarningAsync($"获取预约列表失败: 用户 {username} 提供的页码 {pageNumber.Value} 无效");
                        return BadRequest("pageNumber必须大于0");
                    }
                    
                    if (pageSize.Value < 1)
                    {
                        await _logger.LogWarningAsync($"获取预约列表失败: 用户 {username} 提供的页大小 {pageSize.Value} 无效");
                        return BadRequest("pageSize必须大于0");
                    }
                    
                    var paginatedResult = await _repository.GetPaginatedReservationsAsync(
                        timeRange, projectEngineer, reservatBy, pageNumber.Value, pageSize.Value);
                    
                    await _logger.LogInformationAsync($"返回分页预约列表成功，页码: {pageNumber.Value}，页大小: {pageSize.Value}，共 {paginatedResult.TotalCount} 条");
                    
                    return Ok(paginatedResult);
                }
                
                // 否则返回所有数据
                var result = await _repository.GetAllReservationsAsync(timeRange, projectEngineer, reservatBy);
                
                await _logger.LogInformationAsync($"返回所有预约列表成功，共 {result.Count()} 条");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                var username = User.Identity?.Name ?? "匿名用户";
                await _logger.LogErrorAsync($"获取预约列表时发生错误: 用户 {username}, 错误: {ex.Message}", ex);
                // 记录异常
                return StatusCode(500, $"获取预约列表时发生错误: {ex.Message}");
            }
        }

        [Authorize(Roles = "Admin,Engineer")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ReservationDTO reservation)
        {
            try
            {
                var username = User.Identity?.Name;
                await _logger.LogInformationAsync($"用户 {username} 创建预约: 站点 {reservation.Station_Id}, 日期 {reservation.Reservation_Date}, 时间 {reservation.Time_Slot}, 客户 {reservation.Client_Name}");
                
                if (reservation == null)
                {
                    await _logger.LogWarningAsync($"创建预约失败: 请求数据无效");
                    return BadRequest("预约数据无效");
                }
                
                if (string.IsNullOrEmpty(reservation.Reservation_Date) || string.IsNullOrEmpty(reservation.Time_Slot))
                {
                    await _logger.LogWarningAsync($"创建预约失败: 日期或时间段为空");
                    return BadRequest("预约日期和时间段不能为空");
                }
                
                var result = await _repository.CreateReservationsAsync(reservation);
                
                await _logger.LogInformationAsync($"用户 {username} 成功创建预约: 站点 {reservation.Station_Id}, 日期 {reservation.Reservation_Date}");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                var username = User.Identity?.Name ?? "未知用户";
                await _logger.LogErrorAsync($"创建预约时发生错误: 用户 {username}, 错误: {ex.Message}", ex);
                return StatusCode(500, "创建预约时发生错误");
            }
        }

        [Authorize(Roles = "Admin,Engineer")]
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] Reservation reservation)
        {
            try
            {
                var username = User.Identity?.Name;
                await _logger.LogInformationAsync($"用户 {username} 更新预约: ID {reservation.Id}, 站点 {reservation.Station_Id}, 日期 {reservation.Reservation_Date}");
                
                if (reservation == null || reservation.Id <= 0)
                {
                    await _logger.LogWarningAsync($"更新预约失败: 请求数据无效或ID无效");
                    return BadRequest("预约数据无效或ID无效");
                }
                
                var result = await _repository.UpdateReservationAsync(reservation);
                
                if (!result)
                {
                    await _logger.LogWarningAsync($"更新预约失败: ID {reservation.Id}");
                    return StatusCode(500, "更新预约失败");
                }
                
                await _logger.LogInformationAsync($"用户 {username} 成功更新预约: ID {reservation.Id}, 状态 {reservation.Reservation_Status}");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                var username = User.Identity?.Name ?? "未知用户";
                await _logger.LogErrorAsync($"更新预约时发生错误: 用户 {username}, 预约ID: {reservation?.Id}, 错误: {ex.Message}", ex);
                return StatusCode(500, "更新预约时发生错误");
            }
        }
        [Authorize(Roles = "Admin,Engineer")]
        [HttpGet("station_status")]
        public async Task<IActionResult> GetStationStatus([FromQuery] int id, [FromQuery] string date)
        {
            try
            {
                var username = User.Identity?.Name;
                await _logger.LogInformationAsync($"用户 {username} 获取工作站状态: ID {id}, 日期 {date}");
                
                if (id <= 0)
                {
                    await _logger.LogWarningAsync($"获取工作站状态失败: 工作站ID {id} 无效");
                    return BadRequest("工作站ID无效");
                }
                
                if (string.IsNullOrEmpty(date))
                {
                    await _logger.LogWarningAsync($"获取工作站状态失败: 日期参数为空");
                    return BadRequest("日期参数不能为空");
                }
                
                var result = await _repository.GetStationStatusPerDateAsync(id, date);
                
                await _logger.LogInformationAsync($"返回工作站 {id} 在日期 {date} 的状态数据成功");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                var username = User.Identity?.Name ?? "未知用户";
                await _logger.LogErrorAsync($"获取工作站状态时发生错误: 用户 {username}, 工作站ID: {id}, 日期: {date}, 错误: {ex.Message}", ex);
                return StatusCode(500, "获取工作站状态时发生错误");
            }
        }
        [Authorize(Roles = "Admin,Engineer")]
        [HttpGet("station/{id}")]
        public async Task<IActionResult> GetStationById(int id)
        {
            try
            {
                var username = User.Identity?.Name;
                await _logger.LogInformationAsync($"用户 {username} 获取工作站详情: ID {id}");
                
                if (id <= 0)
                {
                    await _logger.LogWarningAsync($"获取工作站详情失败: 工作站ID {id} 无效");
                    return BadRequest("工作站ID无效");
                }
                
                var result = await _repository.GetStationByIdAsync(id);
                
                if (result == null)
                {
                    await _logger.LogWarningAsync($"获取工作站详情失败: 工作站ID {id} 不存在");
                    return NotFound($"未找到ID为 {id} 的工作站");
                }
                
                await _logger.LogInformationAsync($"返回工作站 {id} 的详情数据成功");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                var username = User.Identity?.Name ?? "未知用户";
                await _logger.LogErrorAsync($"获取工作站详情时发生错误: 用户 {username}, 工作站ID: {id}, 错误: {ex.Message}", ex);
                return StatusCode(500, "获取工作站详情时发生错误");
            }
        }

        [Authorize(Roles = "Admin,Engineer")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var username = User.Identity?.Name;
                await _logger.LogInformationAsync($"用户 {username} 删除预约: ID {id}");
                
                if (id <= 0)
                {
                    await _logger.LogWarningAsync($"删除预约失败: 预约ID {id} 无效");
                    return BadRequest("预约ID无效");
                }
                
                var result = await _repository.DeleteReservationAsync(id);
                
                if (!result)
                {
                    await _logger.LogWarningAsync($"删除预约失败: ID {id} 不存在或删除操作失败");
                    return NotFound($"未找到ID为 {id} 的预约或删除操作失败");
                }
                
                await _logger.LogInformationAsync($"用户 {username} 成功删除预约: ID {id}");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                var username = User.Identity?.Name ?? "未知用户";
                await _logger.LogErrorAsync($"删除预约时发生错误: 用户 {username}, 预约ID: {id}, 错误: {ex.Message}", ex);
                return StatusCode(500, "删除预约时发生错误");
            }
        }
    }
}
