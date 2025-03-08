
using Dapper;
using emc_api.Services;
using Microsoft.Data.Sqlite;

public class UserRepository : BaseRepository,IUserRepository
{
    public UserRepository(IConfiguration config) : base(config) { }
    public async Task<User?> GetByUserNameAsync(string userName)
    {
        using var conn = await CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE UserName = @UserName",
            new { UserName = userName });
    }
    public async Task<int> CreateUserAsync(User user)
    {
        using var conn = await CreateConnection();
        var sql = @"
            INSERT INTO Users 
                (UserName, FullName, Team, Role, PasswordHash, CreatedAt, IsActive)
            VALUES 
                (@UserName, @FullName, @Team, @Role, @PasswordHash, @CreatedAt, @IsActive);
            SELECT last_insert_rowid();";
        
        return await conn.ExecuteScalarAsync<int>(sql, user);
    }
    public async Task UpdateRefreshTokenAsync(int userId, string? refreshToken, DateTime? expiryTime)
    {
        using var conn = await CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Users SET RefreshToken = @refreshToken, RefreshTokenExpiryTime = @expiryTime WHERE Id = @userId",
            new { userId, refreshToken, expiryTime });
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        using var conn = await CreateConnection();
        return await conn.QueryAsync<User>("SELECT * FROM Users");
    }

    public async Task UpdatePasswordAsync(int userId, string passwordHash)
    {
        using var conn = await CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Users SET PasswordHash = @passwordHash WHERE Id = @userId",
            new { userId, passwordHash });
    }

    public async Task UpdateUserAsync(User user)
    {
        using var conn = await CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Users SET FullName = @FullName, Team = @Team, Role = @Role, IsActive = @IsActive WHERE Id = @Id",
            user);
    }

    public async Task DisableUserAsync(int userId)
    {
        using var conn = await CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Users SET IsActive = 0 WHERE Id = @userId",
            new { userId });
    }

    public async Task EnableUserAsync(int userId)
    {
        using var conn = await CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Users SET IsActive = 1 WHERE Id = @userId",
            new { userId });
    }

}