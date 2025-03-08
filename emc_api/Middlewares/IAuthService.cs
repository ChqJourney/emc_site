using System.Threading.Tasks;

namespace emc_api.Middleware
{
    public interface IAuthService
    {
        Task<List<ControlledUser>> GetUsersAsync();
    }
}