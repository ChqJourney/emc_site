using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using emc_api.Services;
using emc_api.Models;
using Microsoft.AspNetCore.Authorization;

namespace emc_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly ILoggerService _loggerService;
        private readonly string _logDirectory;
        private readonly JsonSerializerOptions _jsonOptions;

        public LogsController(ILoggerService loggerService)
        {
            _loggerService = loggerService;
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            
            // 配置JSON序列化选项，禁用Unicode转义
            _jsonOptions = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 这将禁用Unicode转义
            };
        }

        /// <summary>
        /// 获取所有可用的日志文件列表
        /// </summary>
        [HttpGet]
        // [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> GetLogFiles()
        {
            await _loggerService.LogInformationAsync($"用户请求查看日志文件列表");
            
            try
            {
                if (!Directory.Exists(_logDirectory))
                {
                    return NotFound("日志目录不存在");
                }

                var logFiles = Directory.GetFiles(_logDirectory, "*.json")
                    .Select(file => new
                    {
                        FileName = Path.GetFileName(file),
                        Date = Path.GetFileNameWithoutExtension(file),
                        Size = new FileInfo(file).Length,
                        LastModified = System.IO.File.GetLastWriteTime(file)
                    })
                    .OrderByDescending(f => f.Date)
                    .ToList();

                return Ok(logFiles);
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"获取日志文件列表时出错", ex);
                return StatusCode(500, "获取日志文件列表时发生错误");
            }
        }

        /// <summary>
        /// 获取指定日期的日志内容
        /// </summary>
        [HttpGet("{date}")]
        // [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> GetLogByDate(string date)
        {
            await _loggerService.LogInformationAsync($"用户请求查看日期为 {date} 的日志");
            
            try
            {
                string filePath = Path.Combine(_logDirectory, $"{date}.json");
                
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound($"指定日期 {date} 的日志文件不存在");
                }

                string jsonContent = await System.IO.File.ReadAllTextAsync(filePath);
                var logs = JsonSerializer.Deserialize<List<LogEntry>>(jsonContent, _jsonOptions);

                return new JsonResult(logs, _jsonOptions);
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"获取日期为 {date} 的日志时出错", ex);
                return StatusCode(500, $"获取日志时发生错误");
            }
        }

        /// <summary>
        /// 根据日期和级别获取过滤后的日志
        /// </summary>
        [HttpGet("filter")]
        // [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> GetFilteredLogs(
            [FromQuery] string date, 
            [FromQuery] string level = null, 
            [FromQuery] string keyword = null)
        {
            await _loggerService.LogInformationAsync($"用户请求过滤日志: 日期={date}, 级别={level}, 关键词={keyword}");
            
            try
            {
                string filePath = Path.Combine(_logDirectory, $"{date}.json");
                
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound($"指定日期 {date} 的日志文件不存在");
                }

                string jsonContent = await System.IO.File.ReadAllTextAsync(filePath);
                var logs = JsonSerializer.Deserialize<List<LogEntry>>(jsonContent, _jsonOptions);

                // 应用过滤条件
                if (!string.IsNullOrEmpty(level))
                {
                    logs = logs.Where(log => log.Level.Equals(level, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrEmpty(keyword))
                {
                    logs = logs.Where(log => 
                        log.Message.Contains(keyword, StringComparison.OrdinalIgnoreCase) || 
                        (log.Exception != null && log.Exception.Message != null && 
                         log.Exception.Message.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                return new JsonResult(logs, _jsonOptions);
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"过滤日志时出错", ex);
                return StatusCode(500, "过滤日志时发生错误");
            }
        }

        /// <summary>
        /// 清除指定日期的日志
        /// </summary>
        [HttpDelete("{date}")]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> DeleteLogByDate(string date)
        {
            await _loggerService.LogWarningAsync($"用户请求删除日期为 {date} 的日志");
            
            try
            {
                string filePath = Path.Combine(_logDirectory, $"{date}.json");
                
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound($"指定日期 {date} 的日志文件不存在");
                }

                System.IO.File.Delete(filePath);
                await _loggerService.LogInformationAsync($"成功删除日期为 {date} 的日志");
                
                return Ok($"已删除日期为 {date} 的日志");
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"删除日期为 {date} 的日志时出错", ex);
                return StatusCode(500, $"删除日志时发生错误");
            }
        }
    }
    
    // 如果LogEntry类不在同一命名空间，则需要在此处重新定义
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public ExceptionInfo? Exception { get; set; }
        public int ThreadId { get; set; }
        public string MachineName { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public RequestInfo? RequestInfo { get; set; }
    }

    public class RequestInfo
    {
        public string? TraceId { get; set; }
        public string? Method { get; set; }
        public string? Path { get; set; }
        public string? QueryString { get; set; }
        public string? ClientIp { get; set; }
        public string? UserAgent { get; set; }
    }

    public class ExceptionInfo
    {
        public string? Type { get; set; }
        public string? Message { get; set; }
        public string? StackTrace { get; set; }
    }
} 