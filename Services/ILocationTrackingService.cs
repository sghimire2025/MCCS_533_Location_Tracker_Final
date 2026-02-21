using LocationTrackerFinal.Models;

namespace LocationTrackerFinal.Services
{
    public interface ILocationTrackingService
    {
        Task<bool> StartTrackingAsync(double originLat, double originLng, double destLat, double destLng);
        Task StopTrackingAsync();
        bool IsTracking { get; }
        event EventHandler<LocationPoint>? LocationUpdated;
        event EventHandler<string>? ErrorOccurred;
    }
}
