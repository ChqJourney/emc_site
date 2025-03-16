using Microsoft.AspNetCore.Mvc;
using emc_api.Models;
using emc_api.Repositories;
using emc_api.Services;
using Microsoft.AspNetCore.Authorization;
using System;

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
            try
            {
                var username = User.Identity?.Name ?? "匿名用户";
                await _logger.LogInformationAsync($"用户 {username} 获取所有事件列表");
                
                var result = await _repository.GetAllSeventsAsync();
                
                if (result == null)
                {
                    await _logger.LogWarningAsync($"未找到任何事件");
                    return NotFound("No events found");
                }
                
                await _logger.LogInformationAsync($"返回所有事件列表成功，共 {result.Count()} 条");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                var username = User.Identity?.Name ?? "匿名用户";
                await _logger.LogErrorAsync($"获取所有事件列表时发生错误: 用户 {username}, 错误: {ex.Message}", ex);
                return StatusCode(500, "获取事件列表时发生错误");
            }
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var username = User.Identity?.Name ?? "匿名用户";
                await _logger.LogInformationAsync($"用户 {username} 获取事件详情，ID: {id}");
                
                if (id <= 0)
                {
                    await _logger.LogWarningAsync($"获取事件详情失败: ID {id} 无效");
                    return BadRequest("事件ID无效");
                }
                
                var result = await _repository.GetSeventByIdAsync(id);
                
                if (result == null)
                {
                    await _logger.LogInformationAsync($"事件ID {id} 不存在，返回空列表");
                    return Ok(new List<SeventDto>()); 
                }
                
                await _logger.LogInformationAsync($"返回事件 {id} 详情成功");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                var username = User.Identity?.Name ?? "匿名用户";
                await _logger.LogErrorAsync($"获取事件详情时发生错误: 用户 {username}, ID: {id}, 错误: {ex.Message}", ex);
                return StatusCode(500, $"获取事件详情时发生错误");
            }
        }
        [HttpGet("station/{id}")]
        public async Task<IActionResult> GetByStation(int id)
        {
            try
            {
                var username = User.Identity?.Name ?? "匿名用户";
                await _logger.LogInformationAsync($"用户 {username} 获取工作站 {id} 的事件列表");
                
                if (id <= 0)
                {
                    await _logger.LogWarningAsync($"获取工作站事件列表失败: 工作站ID {id} 无效");
                    return BadRequest("工作站ID无效");
                }
                
                var result = await _repository.GetSeventsByStationIdAsync(id);
                
                await _logger.LogInformationAsync($"返回工作站 {id} 的事件列表成功，共 {result.Count()} 条");
                
                return Ok(result); // 直接返回Ok，因为Repository已保证即使为空也返回空集合
            }
            catch (Exception ex)
            {
                var username = User.Identity?.Name ?? "匿名用户";
                await _logger.LogErrorAsync($"获取工作站事件列表时发生错误: 用户 {username}, 工作站ID: {id}, 错误: {ex.Message}", ex);
                return StatusCode(500, $"获取工作站事件列表时发生错误");
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SeventDto sevent)
        {
            try
            {
                var username = User.Identity?.Name;
                await _logger.LogInformationAsync($"管理员 {username} 创建事件: {sevent.Name}, 工作站: {sevent.StationId}, 时间范围: {sevent.FromDate} - {sevent.ToDate}");
                
                if (sevent == null || string.IsNullOrEmpty(sevent.Name) || string.IsNullOrEmpty(sevent.FromDate) || string.IsNullOrEmpty(sevent.ToDate))
                {
                    await _logger.LogWarningAsync($"创建事件失败: 请求数据无效");
                    return BadRequest("事件数据无效");
                }
                
                var result = await _repository.CreateSeventAsync(sevent);
                
                if (!result)
                {
                    await _logger.LogWarningAsync($"创建事件失败: {sevent.Name}");
                    return StatusCode(500, "创建事件失败");
                }
                
                await _logger.LogInformationAsync($"管理员 {username} 成功创建事件: {sevent.Name}");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                var username = User.Identity?.Name ?? "未知管理员";
                await _logger.LogErrorAsync($"创建事件时发生错误: 管理员 {username}, 事件名: {sevent?.Name}, 错误: {ex.Message}", ex);
                return StatusCode(500, "创建事件时发生错误");
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] SeventDto sevent)
        {
            try
            {
                var username = User.Identity?.Name;
                await _logger.LogInformationAsync($"管理员 {username} 更新事件: {sevent.Name}, 工作站: {sevent.StationId}, 时间范围: {sevent.FromDate} - {sevent.ToDate}");
                
                if (sevent == null || string.IsNullOrEmpty(sevent.Name) || string.IsNullOrEmpty(sevent.FromDate) || string.IsNullOrEmpty(sevent.ToDate))
                {
                    await _logger.LogWarningAsync($"更新事件失败: 请求数据无效");
                    return BadRequest("事件数据无效");
                }
                
                var result = await _repository.UpdateSeventAsync(sevent);
                
                if (!result)
                {
                    await _logger.LogWarningAsync($"更新事件失败: {sevent.Name}");
                    return StatusCode(500, "更新事件失败");
                }
                
                await _logger.LogInformationAsync($"管理员 {username} 成功更新事件: {sevent.Name}");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                var username = User.Identity?.Name ?? "未知管理员";
                await _logger.LogErrorAsync($"更新事件时发生错误: 管理员 {username}, 事件名: {sevent?.Name}, 错误: {ex.Message}", ex);
                return StatusCode(500, "更新事件时发生错误");
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var username = User.Identity?.Name;
                await _logger.LogInformationAsync($"管理员 {username} 删除事件，ID: {id}");
                
                if (id <= 0)
                {
                    await _logger.LogWarningAsync($"删除事件失败: ID {id} 无效");
                    return BadRequest("事件ID无效");
                }
                
                var result = await _repository.DeleteSeventAsync(id);
                
                if (!result)
                {
                    await _logger.LogWarningAsync($"删除事件失败: ID {id} 不存在或删除操作失败");
                    return NotFound($"未找到ID为 {id} 的事件或删除操作失败");
                }
                
                await _logger.LogInformationAsync($"管理员 {username} 成功删除事件，ID: {id}");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                var username = User.Identity?.Name ?? "未知管理员";
                await _logger.LogErrorAsync($"删除事件时发生错误: 管理员 {username}, 事件ID: {id}, 错误: {ex.Message}", ex);
                return StatusCode(500, "删除事件时发生错误");
            }
        }
    }
}