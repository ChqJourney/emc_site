using System.Text.Json;
using Dapper;
using emc_api.Models;
using emc_api.Services;
using Microsoft.Data.Sqlite;

namespace emc_api.Repositories
{
    public class BizRepository : BaseRepository, IBizRepository
    {
        private readonly ILoggerService _logger;

        public BizRepository(IConfiguration config, ILoggerService logger) : base(config,"Biz")
        {
            _logger = logger;
        }



        public async Task<IEnumerable<Reservation>> GetReservationsByDateAsync(string date)
        {
            try
            {
                using var _connection = await CreateConnection();
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

                using var _connection = await CreateConnection();
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
                using var _connection = await CreateConnection();
                return await _connection.QueryAsync<Reservation>(sql, new { StartDate = startDate, EndDate = endDate, ProjectEngineer = projectEngineer });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error getting reservations by year {Year}", ex);
                throw;
            }
        }
        public async Task<bool> GetStationStatusPerDateAndTimeslotAsync(int id, string date, string timeSlot)
        {
            try
            {
                using var _connection = await CreateConnection();
                var sql = "SELECT * FROM reservations WHERE station_id = @Id AND reservation_date = @Date AND time_slot = @TimeSlot";
                var result = await _connection.QueryAsync<Reservation>(sql, new { Id = id, Date = date, TimeSlot = timeSlot });
                return result.Count() == 0;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error getting station status {Id} {Date} {TimeSlot}", ex);
                throw;
            }
        }
        public async Task<IEnumerable<StationStatus>> GetStationStatusPerDateAsync(int id, string date)
        {
            try
            {
                using var _connection = await CreateConnection();
                var sql = "SELECT * FROM reservations WHERE station_id = @Id AND reservation_date = @Date";
                var result = await _connection.QueryAsync<Reservation>(sql, new { Id = id, Date = date });
                return result.Select(r => new StationStatus(true, r.Time_Slot));
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error getting station status {Id} {Date}", ex);
                throw;
            }
        }
        public async Task<IEnumerable<Reservation>> GetAllReservationsAsync(string timeRange, string? projectEngineer = null, string? createdBy = null)
        {
            try
            {
                using var _connection = await CreateConnection();
                switch (timeRange.ToLower())
                {
                    case "month":
                        var currentMonth = DateTime.Now.ToString("yyyy-MM");
                        return await GetReservationsByMonthAsync(currentMonth, projectEngineer);
                    case "year":
                        var currentYear = DateTime.Now.Year.ToString();
                        return await GetReservationsByYearAsync(currentYear, projectEngineer);
                    case "all":
                        if (createdBy == null && projectEngineer == null)
                        {
                            var sql = "SELECT * FROM reservations ORDER BY reservation_date DESC";
                            return await _connection.QueryAsync<Reservation>(sql);
                        }
                        else if (createdBy == null)
                        {
                            var sql = "SELECT * FROM reservations WHERE project_engineer = @ProjectEngineer ORDER BY reservation_date DESC";
                            return await _connection.QueryAsync<Reservation>(sql, new { ProjectEngineer = projectEngineer });
                        }
                        else if (projectEngineer == null)
                        {
                            var sql = "SELECT * FROM reservations WHERE created_by = @CreatedBy ORDER BY reservation_date DESC";
                            return await _connection.QueryAsync<Reservation>(sql, new { CreatedBy = createdBy });
                        }
                        else
                        {
                            var sql = "SELECT * FROM reservations WHERE project_engineer = @ProjectEngineer AND created_by = @CreatedBy ORDER BY reservation_date DESC";
                            return await _connection.QueryAsync<Reservation>(sql, new { ProjectEngineer = projectEngineer, CreatedBy = createdBy });
                        }

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
                using var _connection = await CreateConnection();
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
);";
            using var _connection = await CreateConnection();

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

                transaction.Commit();
                return rowsAffected;

            }
            catch (Exception ex)
            {
                transaction.Rollback();
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
                using var _connection = await CreateConnection();
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
                using var _connection = await CreateConnection();
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
                using var _connection = await CreateConnection();
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
                using var _connection = await CreateConnection();
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
                using var _connection = await CreateConnection();
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
                using var _connection = await CreateConnection();
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
                using var _connection = await CreateConnection();
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
                using var _connection = await CreateConnection();
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
                using var _connection = await CreateConnection();
                await _connection.ExecuteAsync("select 1");
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
            var dataDir = _config["data:dir"];
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
            using var _connection = await CreateConnection();
            var sevents = await _connection.QueryAsync<Sevent>("SELECT * FROM s_events");
            Console.WriteLine("allaa");
            Console.WriteLine(sevents.Count());
            return sevents;
        }
        public async Task<Sevent> GetSeventByIdAsync(int id)
        {
            using var _connection = await CreateConnection();
            return await _connection.QueryFirstOrDefaultAsync<Sevent>("SELECT * FROM s_events WHERE id = @Id", new { Id = id });
        }

        public async Task<bool> CreateSeventAsync(SeventDto sevent)
        {
            var sql = @"INSERT INTO s_events (name, from_date, to_date, station_id,created_by, updated_by)
                   VALUES (@Name, @FromDate, @ToDate, @StationId,@CreatedBy,@UpdatedBy)";
            using var _connection = await CreateConnection();
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
                           created_by = @CreatedBy,
                           updated_by = @UpdatedBy
                           WHERE id = @Id";
            using var _connection = await CreateConnection();
            var result = await _connection.ExecuteAsync(sql, sevent);
            return result > 0;
        }

        public async Task<bool> DeleteSeventAsync(int id)
        {
            var sql = "DELETE FROM s_events WHERE id = @Id";
            using var _connection = await CreateConnection();
            var result = await _connection.ExecuteAsync(sql, new { Id = id });
            return result > 0;
        }

        public async Task<IEnumerable<Reservation>> GetReservationsByStationAndMonthAsync(int stationId, string month)
        {
            var sql = "SELECT * FROM reservations WHERE station_id = @StationId AND MONTH(reservation_date) = @Month";
            using var _connection = await CreateConnection();
            var result = await _connection.QueryAsync<Reservation>(sql, new { StationId = stationId, Month = month });
            return result ?? Enumerable.Empty<Reservation>();
        }

        public async Task<IEnumerable<Sevent>> GetSeventsByStationIdAsync(int id)
        {
            var sql = "SELECT name, from_date, to_date, station_id,created_by, updated_by,created_on,updated_on FROM s_events WHERE station_id = @Id";
            using var _connection = await CreateConnection();
            var result = await _connection.QueryAsync<Sevent>(sql, new { Id = id });
            return result ?? Enumerable.Empty<Sevent>();
        }

        public async Task<PaginatedResult<Reservation>> GetPaginatedReservationsAsync(string timeRange, string? projectEngineer = null, string? createdBy = null, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                // 验证分页参数
                if (pageNumber < 1)
                {
                    pageNumber = 1;
                }

                if (pageSize < 1)
                {
                    pageSize = 10;
                }

                // 获取所有记录
                var allReservations = await GetAllReservationsAsync(timeRange, projectEngineer, createdBy);
                
                // 创建分页结果
                return PaginatedResult<Reservation>.Create(allReservations, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error getting paginated reservations", ex);
                throw;
            }
        }

    }
}