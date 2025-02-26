using Microsoft.Data.Sqlite;
using Xunit;
using Moq;
using emc_api.Services;
using emc_api.Models;
using emc_api.Repositories;

namespace emc_api.Tests.Repositories;

public class BizRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly BizRepository _repository;
    private readonly Mock<ILoggerService> _loggerMock;

    public BizRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // 创建测试表
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE reservations (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                reservation_date TEXT NOT NULL,
                project_engineer TEXT,
                client_name TEXT,
                product_name TEXT,
                tests TEXT,
                job_no TEXT,
                testing_engineer TEXT,
                purpose_description TEXT,
                contact_name TEXT,
                time_slot TEXT,
                station_id INTEGER
            )";
        command.ExecuteNonQuery();

        _loggerMock = new Mock<ILoggerService>();
        _repository = new BizRepository(_connection, _loggerMock.Object);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task GetReservationsByDateAsync_ShouldReturnReservations()
    {
        // Arrange
        var date = "2025-02-26";
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO reservations (reservation_date, project_engineer, client_name, product_name) 
            VALUES (@date, 'Engineer1', 'Customer1', 'Project1')";
        command.Parameters.AddWithValue("@date", date);
        command.ExecuteNonQuery();

        // Act
        var result = await _repository.GetReservationsByDateAsync(date);

        // Assert
        var reservation = Assert.Single(result);
        Assert.Equal(date, reservation.Reservation_Date);
        Assert.Equal("Engineer1", reservation.Project_Engineer);
    }

    [Fact]
    public async Task GetReservationsByMonthAsync_ShouldReturnReservationsForMonth()
    {
        // Arrange
        var month = "2025-02";
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO reservations (reservation_date, project_engineer) 
            VALUES 
                ('2025-02-01', 'Engineer1'),
                ('2025-02-15', 'Engineer1'),
                ('2025-03-01', 'Engineer1')";
        command.ExecuteNonQuery();

        // Act
        var result = await _repository.GetReservationsByMonthAsync(month);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetReservationsByMonthAsync_WithProjectEngineer_ShouldFilterByEngineer()
    {
        // Arrange
        var month = "2025-02";
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO reservations (reservation_date, project_engineer) 
            VALUES 
                ('2025-02-01', 'Engineer1'),
                ('2025-02-15', 'Engineer2')";
        command.ExecuteNonQuery();

        // Act
        var result = await _repository.GetReservationsByMonthAsync(month, "Engineer1");

        // Assert
        var reservation = Assert.Single(result);
        Assert.Equal("Engineer1", reservation.Project_Engineer);
    }

    [Fact]
    public async Task GetReservationsByYearAsync_ShouldReturnReservationsForYear()
    {
        // Arrange
        var year = "2025";
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO reservations (reservation_date, project_engineer) 
            VALUES 
                ('2025-01-01', 'Engineer1'),
                ('2025-12-31', 'Engineer1'),
                ('2024-12-31', 'Engineer1')";
        command.ExecuteNonQuery();

        // Act
        var result = await _repository.GetReservationsByYearAsync(year);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Theory]
    [InlineData("month")]
    [InlineData("year")]
    [InlineData("all")]
    public async Task GetAllReservationsAsync_ShouldReturnReservationsBasedOnTimeRange(string timeRange)
    {
        // Arrange
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO reservations (reservation_date, project_engineer) 
            VALUES 
                ('2025-02-26', 'Engineer1'),
                ('2025-01-01', 'Engineer1'),
                ('2024-12-31', 'Engineer1')";
        command.ExecuteNonQuery();

        // Act
        var result = await _repository.GetAllReservationsAsync(timeRange);

        // Assert
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetAllReservationsAsync_WithProjectEngineer_ShouldFilterByEngineer()
    {
        // Arrange
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO reservations (reservation_date, project_engineer) 
            VALUES 
                ('2025-02-26', 'Engineer1'),
                ('2025-02-26', 'Engineer2')";
        command.ExecuteNonQuery();

        // Act
        var result = await _repository.GetAllReservationsAsync("all", "Engineer1");

        // Assert
        var reservation = Assert.Single(result);
        Assert.Equal("Engineer1", reservation.Project_Engineer);
    }
}
