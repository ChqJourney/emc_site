using Microsoft.AspNetCore.Mvc;
using emc_api.Repositories;
using emc_api.Services;
using System.Threading.Tasks;

namespace emc_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IBizRepository _repository;
        private readonly ILoggerService _logger;

        public HealthController(IBizRepository repository, ILoggerService logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Check()
        {
            await _logger.LogInformationAsync($"健康检查API被调用 - IP:{HttpContext.Connection.RemoteIpAddress}, User-Agent:{Request.Headers["User-Agent"]}");
            
            try
            {
                var result = await _repository.CheckDatabaseConnectionAsync();
                
                if (result)
                {
                    await _logger.LogInformationAsync("数据库连接检查成功");
                    return Ok("数据库连接成功");
                }
                else
                {
                    await _logger.LogWarningAsync("数据库连接检查失败");
                    return NotFound("数据库连接失败");
                }
            }
            catch (System.Exception ex)
            {
                await _logger.LogErrorAsync($"健康检查过程中发生异常", ex);
                return StatusCode(500, "健康检查过程中发生错误");
            }
        }
        
        [HttpGet("detailed")]
        public async Task<IActionResult> DetailedCheck()
        {
            await _logger.LogInformationAsync($"详细健康检查API被调用 - IP:{HttpContext.Connection.RemoteIpAddress}");
            
            try
            {
                // 检查数据库连接
                var dbResult = await _repository.CheckDatabaseConnectionAsync();
                
                // 检查系统资源
                var memoryInfo = new
                {
                    TotalMemory = System.GC.GetTotalMemory(false),
                    WorkingSet = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64,
                    ProcessorCount = System.Environment.ProcessorCount
                };
                
                var healthStatus = new
                {
                    DatabaseStatus = dbResult ? "连接正常" : "连接失败",
                    ServerTime = System.DateTime.Now,
                    MemoryUsage = memoryInfo,
                    ApplicationStatus = "运行中"
                };
                
                await _logger.LogInformationAsync($"详细健康检查完成，数据库状态: {(dbResult ? "正常" : "异常")}");
                return Ok(healthStatus);
            }
            catch (System.Exception ex)
            {
                await _logger.LogErrorAsync($"详细健康检查过程中发生异常", ex);
                return StatusCode(500, "详细健康检查过程中发生错误");
            }
        }
    }
}
