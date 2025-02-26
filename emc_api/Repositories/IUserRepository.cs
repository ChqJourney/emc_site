public interface IUserRepository
{
    Task<User?> GetUserAsync(string username, string machineName);
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<User> SetUserAsync(UserDto user);
    Task<UserActivity> LogUserActivityAsync(UserActivity activity);
    
}