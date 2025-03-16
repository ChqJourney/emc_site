using Microsoft.AspNetCore.Mvc;
using emc_api.Models;
using emc_api.Repositories;
using System.Threading.Tasks;
using emc_api.Services;
using System;

namespace emc_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GeneralController : ControllerBase
    {
        private readonly IBizRepository _repository;
        private readonly ILoggerService _logger;

        public GeneralController(IBizRepository repository, ILoggerService logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            try
            {
                var username = User.Identity?.Name ?? "匿名用户";
                await _logger.LogInformationAsync($"用户 {username} 获取系统设置");
                
                var result = await _repository.GetSettingsAsync();
                
                if (result == null)
                {
                    await _logger.LogWarningAsync($"系统设置获取失败: 未找到系统设置");
                    return NotFound("未找到系统设置");
                }
                
                await _logger.LogInformationAsync($"返回系统设置成功");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                var username = User.Identity?.Name ?? "匿名用户";
                await _logger.LogErrorAsync($"获取系统设置时发生错误: 用户 {username}, 错误: {ex.Message}", ex);
                return StatusCode(500, "获取系统设置时发生错误");
            }
        }
    }
}