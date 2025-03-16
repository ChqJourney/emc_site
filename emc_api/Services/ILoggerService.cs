using System.Threading.Tasks;
using emc_api.Models;

namespace emc_api.Services
{
    public interface ILoggerService
    {
        Task LogInformationAsync(string message);
        Task LogWarningAsync(string message);
        Task LogErrorAsync(string message, Exception ex=null);
        Task LogDebugAsync(string message);
        
        // 添加支持RequestInfo的方法
        Task LogInformationWithRequestInfoAsync(string message, RequestInfo requestInfo);
        Task LogWarningWithRequestInfoAsync(string message, RequestInfo requestInfo);
        Task LogErrorWithRequestInfoAsync(string message, Exception ex, RequestInfo requestInfo);
        Task LogDebugWithRequestInfoAsync(string message, RequestInfo requestInfo);
    }
    
    
}