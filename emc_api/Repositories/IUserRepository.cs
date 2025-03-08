public interface IUserRepository
{
    Task<User?> GetByUserNameAsync(string userName);
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<int> CreateUserAsync(User user);
    Task UpdateRefreshTokenAsync(int userId, string? refreshToken, DateTime? expiryTime);

    Task UpdatePasswordAsync(int userId, string passwordHash);
    Task UpdateUserAsync(User user);
    Task DisableUserAsync(int userId);
    Task EnableUserAsync(int userId);
    
}