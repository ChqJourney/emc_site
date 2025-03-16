using Microsoft.AspNetCore.Mvc;
using emc_api.Models;
using emc_api.Repositories;
using Microsoft.AspNetCore.Authorization;
using emc_api.Services;
using System;

namespace emc_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StationsController : ControllerBase
    {
        private readonly IBizRepository _repository;
        private readonly ILoggerService _logger;

        public StationsController(IBizRepository repository, ILoggerService logger)
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
                await _logger.LogInformationAsync($"用户 {username} 获取所有工位列表");
                
                var result = await _repository.GetAllStationsAsync();
                
                await _logger.LogInformationAsync($"返回所有工位列表成功，共 {result.Count()} 个工位");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                var username = User.Identity?.Name ?? "匿名用户";
                await _logger.LogErrorAsync($"获取所有工位列表时发生错误: 用户 {username}, 错误: {ex.Message}", ex);
                return StatusCode(500, "获取工位列表时发生错误");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var username = User.Identity?.Name ?? "匿名用户";
                await _logger.LogInformationAsync($"用户 {username} 获取工位详情，ID: {id}");
                
                var result = await _repository.GetStationByIdAsync(id);
                
                if (result == null)
                {
                    await _logger.LogWarningAsync($"工位不存在，ID: {id}");
                    return NotFound($"未找到ID为 {id} 的工位");
                }
                
                await _logger.LogInformationAsync($"返回工位 {id} 详情成功");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                var username = User.Identity?.Name ?? "匿名用户";
                await _logger.LogErrorAsync($"获取工位详情时发生错误: 用户 {username}, ID: {id}, 错误: {ex.Message}", ex);
                return StatusCode(500, $"获取工位详情时发生错误");
            }
        }

        [HttpGet("{id}/shortname")]
        public async Task<IActionResult> GetShortName(int id)
        {
            try
            {
                var username = User.Identity?.Name ?? "匿名用户";
                await _logger.LogInformationAsync($"用户 {username} 获取工位简称，ID: {id}");
                
                var result = await _repository.GetStationShortNameByIdAsync(id);
                
                if (string.IsNullOrEmpty(result))
                {
                    await _logger.LogWarningAsync($"工位简称不存在，ID: {id}");
                }
                else
                {
                    await _logger.LogInformationAsync($"返回工位 {id} 简称成功: {result}");
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                var username = User.Identity?.Name ?? "匿名用户";
                await _logger.LogErrorAsync($"获取工位简称时发生错误: 用户 {username}, ID: {id}, 错误: {ex.Message}", ex);
                return StatusCode(500, $"获取工位简称时发生错误");
            }
        }
        
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Station station)
        {
            try
            {
                var username = User.Identity?.Name;
                await _logger.LogInformationAsync($"管理员 {username} 创建工位: {station.Name}, 简称: {station.Short_Name}");
                
                if (station == null)
                {
                    await _logger.LogWarningAsync($"创建工位失败: 请求数据无效");
                    return BadRequest("工位数据无效");
                }
                
                var result = await _repository.CreateStationAsync(station);
                
                if (!result)
                {
                    await _logger.LogWarningAsync($"创建工位失败: {station.Name}");
                    return StatusCode(500, "创建工位失败");
                }
                
                await _logger.LogInformationAsync($"管理员 {username} 成功创建工位: {station.Name}");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                var username = User.Identity?.Name ?? "未知管理员";
                await _logger.LogErrorAsync($"创建工位时发生错误: 管理员 {username}, 工位名: {station?.Name}, 错误: {ex.Message}", ex);
                return StatusCode(500, "创建工位时发生错误");
            }
        }
        
        [Authorize(Roles = "Admin")]
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] Station station)
        {
            try
            {
                var username = User.Identity?.Name;
                await _logger.LogInformationAsync($"管理员 {username} 更新工位，ID: {station.Id}, 名称: {station.Name}");
                
                if (station == null || station.Id <= 0)
                {
                    await _logger.LogWarningAsync($"更新工位失败: 请求数据无效或ID无效");
                    return BadRequest("工位数据无效或ID无效");
                }
                
                var result = await _repository.UpdateStationAsync(station);
                
                if (!result)
                {
                    await _logger.LogWarningAsync($"更新工位失败: ID {station.Id}");
                    return StatusCode(500, "更新工位失败");
                }
                
                await _logger.LogInformationAsync($"管理员 {username} 成功更新工位，ID: {station.Id}");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                var username = User.Identity?.Name ?? "未知管理员";
                await _logger.LogErrorAsync($"更新工位时发生错误: 管理员 {username}, 工位ID: {station?.Id}, 错误: {ex.Message}", ex);
                return StatusCode(500, "更新工位时发生错误");
            }
        }
        
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var username = User.Identity?.Name;
                await _logger.LogInformationAsync($"管理员 {username} 删除工位，ID: {id}");
                
                if (id <= 0)
                {
                    await _logger.LogWarningAsync($"删除工位失败: ID {id} 无效");
                    return BadRequest("工位ID无效");
                }
                
                var result = await _repository.DeleteStationAsync(id);
                
                if (!result)
                {
                    await _logger.LogWarningAsync($"删除工位失败: ID {id} 不存在或删除操作失败");
                    return NotFound($"未找到ID为 {id} 的工位或删除操作失败");
                }
                
                await _logger.LogInformationAsync($"管理员 {username} 成功删除工位，ID: {id}");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                var username = User.Identity?.Name ?? "未知管理员";
                await _logger.LogErrorAsync($"删除工位时发生错误: 管理员 {username}, 工位ID: {id}, 错误: {ex.Message}", ex);
                return StatusCode(500, "删除工位时发生错误");
            }
        }
    }
}
