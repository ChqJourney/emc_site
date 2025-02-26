using Microsoft.Data.Sqlite;
using Xunit;
using emc_api.Services;
using emc_api.Models;
using emc_api.Repositories;

namespace emc_api.Tests.Repositories;

public class UserRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // 创建测试表
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserName TEXT NOT NULL,
                MachineName TEXT NOT NULL,
                FullName TEXT,
                Team TEXT,
                Role TEXT,
                LoginAt TEXT
            );
            CREATE TABLE UserActivities (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                ApiUsed TEXT NOT NULL,
                Timestamp TEXT NOT NULL
            );";
        command.ExecuteNonQuery();

        _repository = new UserRepository(_connection);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task GetUserAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var username = "testuser";
        var machineName = "testmachine";
        using var command = _connection.CreateCommand();
        command.CommandText = "INSERT INTO Users (UserName, MachineName, FullName, Team, Role, LoginAt) VALUES (@username, @machineName, 'Test User', 'Test Team', 'User', @loginAt)";
        command.Parameters.AddWithValue("@username", username);
        command.Parameters.AddWithValue("@machineName", machineName);
        command.Parameters.AddWithValue("@loginAt", DateTime.UtcNow.ToString("O"));
        command.ExecuteNonQuery();

        // Act
        var user = await _repository.GetUserAsync(username, machineName);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(username, user.UserName);
        Assert.Equal(machineName, user.MachineName);
        Assert.Equal("Test User", user.FullName);
    }

    [Fact]
    public async Task GetUserAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Act
        var user = await _repository.GetUserAsync("nonexistent", "nonexistent");

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public async Task LogUserActivityAsync_ShouldCreateActivity()
    {
        // Arrange
        var activity = new UserActivity
        {
            UserId = 1,
            ApiUsed = "TestApi",
            Timestamp = DateTime.UtcNow.ToString("O")
        };

        // Act
        var result = await _repository.LogUserActivityAsync(activity);

        // Assert
        Assert.NotEqual(0, result.Id);
    }

    [Fact]
    public async Task SetUserAsync_ShouldCreateAndReturnUser()
    {
        // Arrange
        var loginAt = DateTime.UtcNow;
        var userDto = new UserDto
        {
            UserName = "newuser",
            MachineName = "newmachine",
            FullName = "New User",
            Team = "New Team",
            Role = "User",
            LoginAt = loginAt
        };

        // Act
        var result = await _repository.SetUserAsync(userDto);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(0, result.Id);
        Assert.Equal(userDto.UserName, result.UserName);
        Assert.Equal(userDto.MachineName, result.MachineName);
        Assert.Equal(userDto.FullName, result.FullName);
        // SQLite stores DateTime as string, so we need to parse it back
        var resultLoginAt = DateTime.Parse(result.LoginAt);
        Assert.Equal(loginAt.Year, resultLoginAt.Year);
        Assert.Equal(loginAt.Month, resultLoginAt.Month);
        Assert.Equal(loginAt.Day, resultLoginAt.Day);
        Assert.Equal(loginAt.Hour, resultLoginAt.Hour);
        Assert.Equal(loginAt.Minute, resultLoginAt.Minute);
    }

    [Fact]
    public async Task GetAllUsersAsync_ShouldReturnAllUsers()
    {
        // Arrange
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Users (UserName, MachineName, FullName, Team, Role, LoginAt) 
            VALUES 
                ('user1', 'machine1', 'User 1', 'Team1', 'User', @loginAt),
                ('user2', 'machine2', 'User 2', 'Team2', 'User', @loginAt)";
        command.Parameters.AddWithValue("@loginAt", DateTime.UtcNow.ToString("O"));
        command.ExecuteNonQuery();

        // Act
        var users = await _repository.GetAllUsersAsync();

        // Assert
        Assert.Equal(2, users.Count());
    }
}
