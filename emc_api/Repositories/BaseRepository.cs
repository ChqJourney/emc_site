using System.Data;
using Microsoft.Data.Sqlite;

public abstract class BaseRepository
{
    protected readonly IConfiguration _config;
    private readonly string _connectionString;
    protected BaseRepository(IConfiguration config,String dbName)
    {
        _config = config;
        _connectionString = config.GetConnectionString($"{dbName}Connection");
    }
    protected async Task<IDbConnection> CreateConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        return conn;
    }
}