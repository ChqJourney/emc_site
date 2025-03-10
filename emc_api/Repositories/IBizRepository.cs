using System.Text.Json.Nodes;
using emc_api.Models;

namespace emc_api.Repositories
{
    public record StationStatus(bool isOccupied,string timeSlot);
    public interface IBizRepository
    {
        Task<IEnumerable<Reservation>> GetReservationsByDateAsync(string date);
        Task<IEnumerable<Reservation>> GetReservationsByMonthAsync(string month, string projectEngineer = null);
        Task<IEnumerable<Reservation>> GetReservationsByYearAsync(string year, string projectEngineer = null);
        Task<IEnumerable<Reservation>> GetAllReservationsAsync(string timeRange, string projectEngineer = null,string createdBy=null);
        Task<IEnumerable<Reservation>> GetReservationsByStationAndMonthAsync(int stationId, string month);
        Task<bool> GetStationStatusPerDateAndTimeslotAsync(int id, string date, string timeSlot);
        Task<IEnumerable<StationStatus>> GetStationStatusPerDateAsync(int id, string date);
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
        Task<IEnumerable<Sevent>> GetAllSeventsAsync();
        Task<bool> CreateSeventAsync(SeventDto sevent);
        Task<bool> UpdateSeventAsync(SeventDto sevent);
        Task<Sevent> GetSeventByIdAsync(int id);
        Task<IEnumerable<Sevent>> GetSeventsByStationIdAsync(int id);
        Task<bool> DeleteSeventAsync(int id);
        Task<bool> CheckDatabaseConnectionAsync();
        Task<Settings> GetSettingsAsync();
    }
}