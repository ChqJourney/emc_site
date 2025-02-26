using System.Net;
using System.Text.Json;
using emc_api.Services;

namespace emc_api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILoggerService _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILoggerService logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            var response = new 
            {
                error = new
                {
                    message = "发生了一个内部服务器错误",
                    details = exception.Message
                }
            };

            // 根据异常类型设置不同的状态码
            context.Response.StatusCode = exception switch
            {
                ArgumentException => (int)HttpStatusCode.BadRequest,
                KeyNotFoundException => (int)HttpStatusCode.NotFound,
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                _ => (int)HttpStatusCode.InternalServerError
            };

            // 记录异常信息
            await _logger.LogErrorAsync($"[{context.Response.StatusCode}] {exception.GetType().Name}: {exception.Message}\nStackTrace: {exception.StackTrace}");

            // 返回错误响应
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}