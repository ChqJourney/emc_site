using emc_api.Models;

namespace emc_api.Repositories
{
    public interface IBizRepository
    {
        Task<IEnumerable<Reservation>> GetReservationsByDateAsync(string date);
        Task<IEnumerable<Reservation>> GetReservationsByMonthAsync(string month, string projectEngineer = null);
        Task<IEnumerable<Reservation>> GetReservationsByYearAsync(string year, string projectEngineer = null);
        Task<IEnumerable<Reservation>> GetAllReservationsAsync(string timeRange, string projectEngineer = null);
        Task<bool> CreateReservationAsync(Reservation reservation);
        Task<int> CreateReservationsAsync(Reservation reservation);
        Task<bool> UpdateReservationAsync(Reservation reservation);
        Task<bool> DeleteReservationAsync(int id);
        
        Task<IEnumerable<Station>> GetAllStationsAsync();
        Task<Station> GetStationByIdAsync(int id);
        Task<string> GetStationShortNameByIdAsync(int id);
        Task<bool> CreateStationAsync(Station station);
        Task<bool> UpdateStationAsync(Station station);
        Task<bool> DeleteStationAsync(int id);
        Task<bool> CheckDatabaseConnectionAsync();
    }
}