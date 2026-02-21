using LocationTrackerFinal.Models;

namespace LocationTrackerFinal.Services
{
    public interface IGoogleDirectionsService
    {
        Task<DirectionsResponse> GetDirectionsAsync(
            double originLat,
            double originLng,
            double destLat,
            double destLng);
    }
}
