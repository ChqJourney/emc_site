using System.Data;
using Microsoft.Data.Sqlite;

public abstract class BaseRepository
{
    private readonly string _connectionString;
    protected BaseRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("Default");
    }
    protected async Task<IDbConnection> CreateConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        return conn;
    }
}