using System.Threading.Tasks;

namespace emc_api.Services
{
    public interface ILoggerService
    {
        Task LogInformationAsync(string message);
        Task LogWarningAsync(string message);
        Task LogErrorAsync(string message, Exception ex=null);
    }
}