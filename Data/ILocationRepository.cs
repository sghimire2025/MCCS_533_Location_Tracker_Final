using LocationTrackerFinal.Models;

namespace LocationTrackerFinal.Data
{
    public interface ILocationRepository
    {
        Task InitializeDatabaseAsync();
        Task<int> SaveLocationAsync(LocationPoint location);
        Task<List<LocationPoint>> GetAllLocationsAsync();
        Task<List<LocationPoint>> GetLocationsBySessionAsync(int sessionId);
        Task<int> CreateSessionAsync(TrackingSession session);
        Task UpdateSessionAsync(TrackingSession session);
        Task<TrackingSession?> GetActiveSessionAsync();
        Task<int> DeleteLocationAsync(int id);
    }
}
