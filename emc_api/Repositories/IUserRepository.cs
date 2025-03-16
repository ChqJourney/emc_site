public interface IUserRepository
{
    Task<User?> GetByUserNameAsync(string userName);
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<int> CreateUserAsync(UserDto user, string hash);
    Task UpdateRefreshTokenAsync(int userId, string? refreshToken, long expiryTime);

    Task UpdatePasswordAsync(int userId, string passwordHash);
    Task UpdateUserAsync(User user);
    Task DisableUserAsync(int userId);
    Task EnableUserAsync(int userId);
    Task RemoveUserAsync(int userId);
    Task LockUserAsync(int userId,bool isLocked);
    
}