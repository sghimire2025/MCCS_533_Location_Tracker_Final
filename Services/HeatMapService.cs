using LocationTrackerFinal.Data;
using LocationTrackerFinal.Models;
using LocationTrackerFinal.Utilities;
using Microsoft.Extensions.Logging;

namespace LocationTrackerFinal.Services
{
    public class HeatMapService : IHeatMapService
    {
        private readonly ILocationRepository _repository;
        private readonly ErrorHandler _errorHandler;

        public HeatMapService(ILocationRepository repository, ILogger<HeatMapService> logger)
        {
            _repository = repository;
            _errorHandler = new ErrorHandler(logger);
        }

        public async Task<List<HeatMapPoint>> GenerateHeatMapDataAsync()
        {
            var result = await _errorHandler.ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    var locations = await _repository.GetAllLocationsAsync();
                    return GenerateHeatMapFromLocations(locations);
                },
                "GenerateHeatMapData");

            return result.IsSuccess ? result.Value ?? new List<HeatMapPoint>() : new List<HeatMapPoint>();
        }

        public async Task<List<HeatMapPoint>> GenerateHeatMapDataForSessionAsync(int sessionId)
        {
            var result = await _errorHandler.ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    var locations = await _repository.GetLocationsBySessionAsync(sessionId);
                    return GenerateHeatMapFromLocations(locations);
                },
                "GenerateHeatMapDataForSession");

            return result.IsSuccess ? result.Value ?? new List<HeatMapPoint>() : new List<HeatMapPoint>();
        }

        private List<HeatMapPoint> GenerateHeatMapFromLocations(List<LocationPoint> locations)
        {
            if (locations == null || locations.Count == 0)
            {
                return new List<HeatMapPoint>();
            }

            // Calculate intensity based on location density
            var heatMapPoints = new List<HeatMapPoint>();
            var groupedLocations = locations
                .GroupBy(l => new { Lat = Math.Round(l.Latitude, 4), Lng = Math.Round(l.Longitude, 4) })
                .ToList();

            foreach (var group in groupedLocations)
            {
                var intensity = CalculateIntensity(group.Count(), locations.Count);
                heatMapPoints.Add(new HeatMapPoint
                {
                    Latitude = group.Key.Lat,
                    Longitude = group.Key.Lng,
                    Intensity = intensity
                });
            }

            return heatMapPoints;
        }

        private double CalculateIntensity(int pointCount, int totalCount)
        {
            // Normalize intensity between 0 and 1
            // Higher count at a location = higher intensity
            if (totalCount == 0) return 0;
            
            double normalizedIntensity = (double)pointCount / totalCount;
            // Scale to make differences more visible (apply power function)
            return Math.Min(1.0, normalizedIntensity * 10);
        }
    }
}
