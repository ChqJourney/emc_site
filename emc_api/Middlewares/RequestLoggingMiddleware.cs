using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using emc_api.Models;
using emc_api.Services;

namespace emc_api.Middleware
{
    /// <summary>
    /// 请求日志中间件，负责记录所有HTTP请求的信息
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILoggerService _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILoggerService logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 获取请求信息
            var request = context.Request;
            var userAgent = request.Headers.TryGetValue("User-Agent", out StringValues userAgentValues) ? userAgentValues.ToString() : "未知";
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "未知";
            var traceId = context.TraceIdentifier;
            
            // 创建RequestInfo对象
            var requestInfo = new RequestInfo
            {
                TraceId = traceId,
                Method = request.Method,
                Path = request.Path.ToString(),
                QueryString = request.QueryString.ToString(),
                ClientIp = ipAddress,
                UserAgent = userAgent
            };
            
            // 请求开始前记录
            var requestInfoMessage = BuildRequestLogMessage(context);
            await _logger.LogInformationWithRequestInfoAsync($"请求开始: {requestInfoMessage}", requestInfo);

            // 测量请求处理时间
            var startTime = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // 继续处理请求管道
                await _next(context);
            }
            finally
            {
                startTime.Stop();
                
                // 请求结束后记录
                var statusCode = context.Response.StatusCode;
                var logLevel = statusCode >= 500 ? Models.LogLevel.Error : 
                               statusCode >= 400 ? Models.LogLevel.Warning : 
                               Models.LogLevel.Information;
                
                var message = $"请求完成: {requestInfoMessage} | 状态码: {statusCode} | 耗时: {startTime.ElapsedMilliseconds}ms";
                
                switch (logLevel)
                {
                    case Models.LogLevel.Error:
                        await _logger.LogErrorWithRequestInfoAsync(message, null, requestInfo);
                        break;
                    case Models.LogLevel.Warning:
                        await _logger.LogWarningWithRequestInfoAsync(message, requestInfo);
                        break;
                    default:
                        await _logger.LogInformationWithRequestInfoAsync(message, requestInfo);
                        break;
                }
            }
        }

        private string BuildRequestLogMessage(HttpContext context)
        {
            var request = context.Request;
            var userAgent = request.Headers.TryGetValue("User-Agent", out StringValues values) ? values.ToString() : "未知";
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "未知";
            var traceId = context.TraceIdentifier;
            
            return $"TraceId:{traceId} | {request.Method} {request.Path}{request.QueryString} | IP:{ipAddress} | User-Agent:{userAgent}";
        }
    }

    // 中间件扩展类
    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
} 