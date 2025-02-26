
using Dapper;
using emc_api.Services;
using Microsoft.Data.Sqlite;

public class UserRepository : IUserRepository, IAsyncDisposable, IDisposable
{
    private readonly SqliteConnection _connection;

    public UserRepository(SqliteConnection connection)
    {
        _connection = connection;
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }


    public async Task<User?> GetUserAsync(string username, string machineName)
    {
        const string sql = "SELECT * FROM Users WHERE Username = @Username AND MachineName = @MachineName";
        return await _connection.QueryFirstOrDefaultAsync<User>(sql, new { Username = username, MachineName = machineName });
    }


    public async Task<UserActivity> LogUserActivityAsync(UserActivity activity)
    {
        const string sql = @"
                INSERT INTO UserActivities (UserId, ApiUsed, Timestamp)
                VALUES (@UserId, @ApiUsed, @Timestamp);
                SELECT last_insert_rowid()";

        activity.Id = await _connection.ExecuteScalarAsync<int>(sql, activity);
        return activity;
    }

    public async Task<User> SetUserAsync(UserDto user)
    {
        const string insertSql = @"
        INSERT INTO Users (UserName, MachineName, FullName, Team, Role, LoginAt)
        VALUES (@UserName, @MachineName, @FullName, @Team, @Role, @LoginAt);
        SELECT last_insert_rowid();";

    // 先插入，返回新行的 id
    var newId = await _connection.ExecuteScalarAsync<long>(insertSql, user);
    
    // 再查询整个用户数据
    const string selectSql = "SELECT * FROM Users WHERE Id = @Id";
    var insertedUser = await _connection.QuerySingleAsync<User>(selectSql, new { Id = newId });
    return insertedUser;
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        const string sql="SELECT * FROM Users";
        return await _connection.QueryAsync<User>(sql);
    }

}