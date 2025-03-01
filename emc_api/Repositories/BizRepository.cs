using System.Text.Json;
using Dapper;
using emc_api.Models;
using emc_api.Services;
using Microsoft.Data.Sqlite;

namespace emc_api.Repositories
{
    public class BizRepository : IBizRepository, IAsyncDisposable, IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ILoggerService _logger;
        private readonly IConfiguration _configuration;

        public BizRepository(SqliteConnection conn, ILoggerService logger, IConfiguration configuration)
        {
            _connection = conn;
            _logger = logger;
            _configuration = configuration;
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        public async Task<IEnumerable<Reservation>> GetReservationsByDateAsync(string date)
        {
            try
            {

                var sql = "SELECT * FROM reservations WHERE reservation_date = @Date";
                return await _connection.QueryAsync<Reservation>(sql, new { Date = date });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error getting reservations by date {Date}", ex);
                throw;
            }
        }

        public async Task<IEnumerable<Reservation>> GetReservationsByMonthAsync(string month, string projectEngineer = null)
        {
            try
            {
                var startDate = $"{month}-01";
                var endDate = $"{month}-{DateTime.DaysInMonth(int.Parse(month.Split('-')[0]), int.Parse(month.Split('-')[1]))}";

                var sql = projectEngineer == null
                    ? "SELECT * FROM reservations WHERE reservation_date >= @StartDate AND reservation_date <= @EndDate ORDER BY reservation_date DESC"
                    : "SELECT * FROM reservations WHERE reservation_date >= @StartDate AND reservation_date <= @EndDate AND project_engineer = @ProjectEngineer ORDER BY reservation_date DESC";

                return await _connection.QueryAsync<Reservation>(sql, new { StartDate = startDate, EndDate = endDate, ProjectEngineer = projectEngineer });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error getting reservations by month {Month}", ex);
                throw;
            }
        }

        public async Task<IEnumerable<Reservation>> GetReservationsByYearAsync(string year, string projectEngineer = null)
        {
            try
            {
                var startDate = $"{year}-01-01";
                var endDate = $"{year}-12-31";

                var sql = projectEngineer == null
                    ? "SELECT * FROM reservations WHERE reservation_date >= @StartDate AND reservation_date <= @EndDate ORDER BY reservation_date DESC"
                    : "SELECT * FROM reservations WHERE reservation_date >= @StartDate AND reservation_date <= @EndDate AND project_engineer = @ProjectEngineer ORDER BY reservation_date DESC";

                return await _connection.QueryAsync<Reservation>(sql, new { StartDate = startDate, EndDate = endDate, ProjectEngineer = projectEngineer });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error getting reservations by year {Year}", ex);
                throw;
            }
        }

        public async Task<IEnumerable<Reservation>> GetAllReservationsAsync(string timeRange, string projectEngineer = null)
        {
            try
            {
                switch (timeRange.ToLower())
                {
                    case "month":
                        var currentMonth = DateTime.Now.ToString("yyyy-MM");
                        return await GetReservationsByMonthAsync(currentMonth, projectEngineer);
                    case "year":
                        var currentYear = DateTime.Now.Year.ToString();
                        return await GetReservationsByYearAsync(currentYear, projectEngineer);
                    case "all":
                        var sql = projectEngineer == null
                            ? "SELECT * FROM reservations ORDER BY reservation_date DESC"
                            : "SELECT * FROM reservations WHERE project_engineer = @ProjectEngineer ORDER BY reservation_date DESC";
                        return await _connection.QueryAsync<Reservation>(sql, new { ProjectEngineer = projectEngineer });

                    default:
                        return Enumerable.Empty<Reservation>();
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error getting all reservations", ex);
                throw;
            }
        }

        public async Task<bool> CreateReservationAsync(Reservation reservation)
        {
            try
            {
                var sql = @"INSERT INTO reservations (
                    reservation_date, time_slot, station_id, client_name, product_name,
                    tests, job_no, project_engineer, testing_engineer, purpose_description,
                    contact_name, contact_phone, sales, reservate_by, reservation_status
                ) VALUES (
                    @Reservation_Date, @Time_Slot, @Station_Id, @Client_Name, @Product_Name,
                    @Tests, @Job_No, @Project_Engineer, @Testing_Engineer, @Purpose_Description,
                    @Contact_Name, @Contact_Phone, @Sales, @Reservate_By, @Reservation_Status
                )";

                var result = await _connection.ExecuteAsync(sql, reservation);
                return result > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error creating reservation", ex);
                throw;
            }
        }
        public async Task<int> CreateReservationsAsync(Reservation reservation)
        {
            var sql = @"
INSERT INTO reservations (
    reservation_date, time_slot, station_id, client_name, product_name,
    tests, job_no, project_engineer, testing_engineer, purpose_description,
    contact_name, contact_phone, sales, reservate_by, reservation_status
)
SELECT @Reservation_Date, @Time_Slot, @Station_Id, @Client_Name, @Product_Name,
       @Tests, @Job_No, @Project_Engineer, @Testing_Engineer, @Purpose_Description,
       @Contact_Name, @Contact_Phone, @Sales, @Reservate_By, @Reservation_Status
WHERE NOT EXISTS (
    SELECT 1 FROM reservations 
    WHERE reservation_date = @Reservation_Date
      AND time_slot = @Time_Slot
      AND station_id = @Station_Id
);"; ;
            // 确保连接已打开
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }
            using var transaction = _connection.BeginTransaction();
            try
            {
                int rowsAffected = 0;
                // 如果 Time_Slot 中含有逗号，则拆分出多条记录
                var timeSlots = reservation.Time_Slot.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (timeSlots.Length > 1)
                {
                    foreach (var timeSlot in timeSlots)
                    {
                        var newReservation = new Reservation
                        {
                            Reservation_Date = reservation.Reservation_Date,
                            Time_Slot = timeSlot.Trim(),
                            Station_Id = reservation.Station_Id,
                            Client_Name = reservation.Client_Name,
                            Product_Name = reservation.Product_Name,
                            Tests = reservation.Tests,
                            Job_No = reservation.Job_No,
                            Project_Engineer = reservation.Project_Engineer,
                            Testing_Engineer = reservation.Testing_Engineer,
                            Purpose_Description = reservation.Purpose_Description,
                            Contact_Name = reservation.Contact_Name,
                            Contact_Phone = reservation.Contact_Phone,
                            Sales = reservation.Sales,
                            Reservate_By = reservation.Reservate_By,
                            Reservation_Status = reservation.Reservation_Status
                        };

                        rowsAffected += await _connection.ExecuteAsync(sql, newReservation, transaction);
                    }
                }
                else
                {
                    rowsAffected = await _connection.ExecuteAsync(sql, reservation, transaction);
                }

                await transaction.CommitAsync();
                return rowsAffected;

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await _logger.LogErrorAsync("Error creating multiple reservations", ex);
                throw;
            }

        }

        public async Task<bool> UpdateReservationAsync(Reservation reservation)
        {
            try
            {
                var sql = @"UPDATE reservations SET
                    reservation_date = @Reservation_Date,
                    time_slot = @Time_Slot,
                    station_id = @Station_Id,
                    client_name = @Client_Name,
                    product_name = @Product_Name,
                    tests = @Tests,
                    job_no = @Job_No,
                    project_engineer = @Project_Engineer,
                    testing_engineer = @Testing_Engineer,
                    purpose_description = @Purpose_Description,
                    contact_name = @Contact_Name,
                    contact_phone = @Contact_Phone,
                    sales = @Sales,
                    reservate_by = @Reservate_By,
                    reservation_status = @Reservation_Status
                WHERE id = @Id and updated_On=@Updated_On";

                var result = await _connection.ExecuteAsync(sql, reservation);
                return result > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error updating reservation {reservation.Id}", ex);
                throw;
            }
        }

        public async Task<bool> DeleteReservationAsync(int id)
        {
            try
            {
                var sql = "DELETE FROM reservations WHERE id = @Id";
                var result = await _connection.ExecuteAsync(sql, new { Id = id });
                return result > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error deleting reservation {id}", ex);
                throw;
            }
        }

        public async Task<IEnumerable<Station>> GetAllStationsAsync()
        {
            try
            {
                return await _connection.QueryAsync<Station>("SELECT * FROM stations ORDER BY created_on DESC");
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error getting all stations", ex);
                throw;
            }
        }

        public async Task<Station> GetStationByIdAsync(int id)
        {
            try
            {
                return await _connection.QueryFirstOrDefaultAsync<Station>("SELECT * FROM stations WHERE id = @Id", new { Id = id });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error getting station {id}", ex);
                throw;
            }
        }

        public async Task<string> GetStationShortNameByIdAsync(int id)
        {
            try
            {
                return await _connection.QueryFirstOrDefaultAsync<string>("SELECT short_name FROM stations WHERE id = @Id", new { Id = id });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error getting station short name {id}", ex);
                throw;
            }
        }

        public async Task<bool> CreateStationAsync(Station station)
        {
            try
            {
                var sql = @"INSERT INTO stations (name, short_name, description, photo_path, status)
                           VALUES (@Name, @Short_Name, @Description, @Photo_Path, @Status)";
                var result = await _connection.ExecuteAsync(sql, station);
                return result > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error creating station", ex);
                throw;
            }
        }

        public async Task<bool> UpdateStationAsync(Station station)
        {
            try
            {
                var sql = @"UPDATE stations SET
                           name = @Name,
                           short_name = @Shor_Name,
                           description = @Description,
                           photo_path = @Photo_Path,
                           status = @Status
                           WHERE id = @Id";
                var result = await _connection.ExecuteAsync(sql, station);
                return result > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error updating station {station.Id}", ex);
                throw;
            }
        }

        public async Task<bool> DeleteStationAsync(int id)
        {
            try
            {
                var sql = "DELETE FROM stations WHERE id = @Id";
                var result = await _connection.ExecuteAsync(sql, new { Id = id });
                return result > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error deleting station {id}", ex);
                throw;
            }
        }
        public async Task<bool> CheckDatabaseConnectionAsync()
        {
            try
            {
                await _connection.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error checking database connection", ex);
                return false;
            }
        }

        public async Task<Settings> GetSettingsAsync()
        {
            var dataDir = _configuration["data:dir"];
            if (string.IsNullOrEmpty(dataDir))
                throw new InvalidOperationException("Configuration 'data:dir' is not set in appsettings.json");

            var filePath = Path.Combine(dataDir, "settings.json");

            using var reader = new StreamReader(filePath, System.Text.Encoding.UTF8);
            var jsonText = reader.ReadToEnd();
            var settings = JsonSerializer.Deserialize<Settings>(jsonText);
            await _logger.LogInformationAsync(settings.ToString());
            return settings;
        }

        public async Task<IEnumerable<Sevent>> GetAllSeventsAsync()
        {
            var sevents = await _connection.QueryAsync<Sevent>("SELECT * FROM s_events");
            Console.WriteLine("allaa");
            Console.WriteLine(sevents.Count());
            return sevents;
        }
        public Task<Sevent> GetSeventByIdAsync(int id)
        {
            return _connection.QueryFirstOrDefaultAsync<Sevent>("SELECT * FROM s_events WHERE id = @Id", new { Id = id });
        }

        public async Task<bool> CreateSeventAsync(SeventDto sevent)
        {
            var sql = @"INSERT INTO s_events (name, from_date, to_date, station_id, updated_By)
                   VALUES (@Name, @FromDate, @ToDate, @StationId,@UpdatedBy)";
            var result = await _connection.ExecuteAsync(sql, sevent);
            return result > 0;
        }

        public async Task<bool> UpdateSeventAsync(SeventDto sevent)
        {
            var sql = @"UPDATE s_events SET
                           name = @Name,
                           from_date = @FromDate,
                           to_date = @ToDate,
                           station_id = @StationId,
                           updated_by = @UpdatedBy
                           WHERE id = @Id";
            var result = await _connection.ExecuteAsync(sql, sevent);
            return result > 0;
        }

        public async Task<bool> DeleteSeventAsync(int id)
        {
            var sql = "DELETE FROM s_events WHERE id = @Id";
            var result = await _connection.ExecuteAsync(sql, new { Id = id });
            return result > 0;
        }

        public async Task<IEnumerable<Reservation>> GetReservationsByStationAndMonthAsync(int stationId, string month)
        {
            var sql = "SELECT * FROM reservations WHERE station_id = @StationId AND MONTH(reservation_date) = @Month";
            var result = await _connection.QueryAsync<Reservation>(sql, new { StationId = stationId, Month = month });
            return result?? Enumerable.Empty<Reservation>();
        }

        public async Task<IEnumerable<Sevent>> GetSeventsByStationIdAsync(int id)
        {
            var sql = "SELECT * FROM s_events WHERE station_id = @Id";
            var result = await _connection.QueryAsync<Sevent>(sql, new { Id = id });
            return result?? Enumerable.Empty<Sevent>();
        }

    }
}