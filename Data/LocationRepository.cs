using SQLite;
using LocationTrackerFinal.Models;
using LocationTrackerFinal.Utilities;
using Microsoft.Extensions.Logging;

namespace LocationTrackerFinal.Data
{
    public class LocationRepository : ILocationRepository
    {
        private readonly SQLiteAsyncConnection _database;
        private readonly ErrorHandler _errorHandler;

        public LocationRepository(string dbPath, ILogger<LocationRepository> logger)
        {
            _database = new SQLiteAsyncConnection(dbPath);
            _errorHandler = new ErrorHandler(logger);
        }

        public async Task InitializeDatabaseAsync()
        {
            var result = await _errorHandler.ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    await _database.CreateTableAsync<LocationPoint>();
                    await _database.CreateTableAsync<TrackingSession>();
                },
                "InitializeDatabase");

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException("Failed to initialize database", result.Exception);
            }
        }

        public async Task<int> SaveLocationAsync(LocationPoint location)
        {
            var result = await _errorHandler.ExecuteWithErrorHandlingAsync(
                async () => await _database.InsertAsync(location),
                "SaveLocation");

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException("Failed to save location data", result.Exception);
            }

            return result.Value;
        }

        public async Task<List<LocationPoint>> GetAllLocationsAsync()
        {
            var result = await _errorHandler.ExecuteWithErrorHandlingAsync(
                async () => await _database.Table<LocationPoint>().ToListAsync(),
                "GetAllLocations");

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException("Failed to retrieve location data", result.Exception);
            }

            return result.Value ?? new List<LocationPoint>();
        }

        public async Task<List<LocationPoint>> GetLocationsBySessionAsync(int sessionId)
        {
            var result = await _errorHandler.ExecuteWithErrorHandlingAsync(
                async () => await _database.Table<LocationPoint>()
                    .Where(l => l.SessionId == sessionId)
                    .ToListAsync(),
                "GetLocationsBySession");

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to retrieve locations for session {sessionId}", result.Exception);
            }

            return result.Value ?? new List<LocationPoint>();
        }

        public async Task<int> CreateSessionAsync(TrackingSession session)
        {
            var result = await _errorHandler.ExecuteWithErrorHandlingAsync(
                async () => await _database.InsertAsync(session),
                "CreateSession");

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException("Failed to create tracking session", result.Exception);
            }

            return result.Value;
        }

        public async Task UpdateSessionAsync(TrackingSession session)
        {
            var result = await _errorHandler.ExecuteWithErrorHandlingAsync(
                async () => await _database.UpdateAsync(session),
                "UpdateSession");

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException("Failed to update tracking session", result.Exception);
            }
        }

        public async Task<TrackingSession?> GetActiveSessionAsync()
        {
            var result = await _errorHandler.ExecuteWithErrorHandlingAsync(
                async () => await _database.Table<TrackingSession>()
                    .Where(s => s.IsActive)
                    .FirstOrDefaultAsync(),
                "GetActiveSession");

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException("Failed to retrieve active session", result.Exception);
            }

            return result.Value;
        }

        public async Task<int> DeleteLocationAsync(int id)
        {
            var result = await _errorHandler.ExecuteWithErrorHandlingAsync(
                async () => await _database.DeleteAsync<LocationPoint>(id),
                "DeleteLocation");

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to delete location with id {id}", result.Exception);
            }

            return result.Value;
        }
    }
}
