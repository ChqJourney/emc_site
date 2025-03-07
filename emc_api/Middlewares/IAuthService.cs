using System.Threading.Tasks;

namespace emc_api.Middleware
{
    public interface IAuthService
    {
        Task<List<User>> GetUsersAsync();
        Task<User> ValidateUserAsync(string username, string password);
        Task<bool> ChangePasswordAsync(string username,string newPassword);
    }
}