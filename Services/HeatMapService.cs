using LocationTrackerFinal.Data;
using LocationTrackerFinal.Models;
using LocationTrackerFinal.Utilities;
using Microsoft.Extensions.Logging;

namespace LocationTrackerFinal.Services
{
    public class HeatMapService : IHeatMapService
    {
        private readonly ILocationRepository _repository;
        private readonly ICrowdSimulator _crowdSimulator;
        private readonly ErrorHandler _errorHandler;
        private bool _isCrowdSimulationEnabled;

        public bool IsCrowdSimulationEnabled => _isCrowdSimulationEnabled;

        public HeatMapService(
            ILocationRepository repository, 
            ICrowdSimulator crowdSimulator,
            ILogger<HeatMapService> logger)
        {
            _repository = repository;
            _crowdSimulator = crowdSimulator;
            _errorHandler = new ErrorHandler(logger);
            _isCrowdSimulationEnabled = false;
        }

        public void SetCrowdSimulationEnabled(bool enabled)
        {
            _isCrowdSimulationEnabled = enabled;
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

        public async Task<List<HeatMapPoint>> GenerateHeatMapDataWithCrowdAsync()
        {
            var result = await _errorHandler.ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    var locations = await _repository.GetAllLocationsAsync();
                    return GenerateHeatMapFromLocationsWithCrowd(locations);
                },
                "GenerateHeatMapDataWithCrowd");

            return result.IsSuccess ? result.Value ?? new List<HeatMapPoint>() : new List<HeatMapPoint>();
        }

        public async Task<List<HeatMapPoint>> GenerateHeatMapDataForSessionWithCrowdAsync(int sessionId)
        {
            var result = await _errorHandler.ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    var locations = await _repository.GetLocationsBySessionAsync(sessionId);
                    return GenerateHeatMapFromLocationsWithCrowd(locations);
                },
                "GenerateHeatMapDataForSessionWithCrowd");

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

        private List<HeatMapPoint> GenerateHeatMapFromLocationsWithCrowd(List<LocationPoint> locations)
        {
            if (locations == null || locations.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("GenerateHeatMapFromLocationsWithCrowd: No locations provided");
                return new List<HeatMapPoint>();
            }

            // If crowd simulation is disabled, return normal heatmap
            if (!_isCrowdSimulationEnabled)
            {
                System.Diagnostics.Debug.WriteLine("GenerateHeatMapFromLocationsWithCrowd: Crowd simulation is DISABLED");
                return GenerateHeatMapFromLocations(locations);
            }

            System.Diagnostics.Debug.WriteLine($"GenerateHeatMapFromLocationsWithCrowd: Crowd simulation is ENABLED, processing {locations.Count} locations");

            // CRITICAL: Limit input locations to prevent rendering too many circles
            // For MVP: Keep it very light - only process most recent 30 locations
            // Each location generates 2-4 crowd points, and each point creates 2 circles (blue + red)
            // So 30 locations = ~90 points = ~180 circles (very light for MVP)
            const int maxInputLocations = 30;
            var locationsToProcess = locations.Count > maxInputLocations 
                ? locations.OrderByDescending(l => l.Timestamp).Take(maxInputLocations).ToList()
                : locations;

            if (locations.Count > maxInputLocations)
            {
                System.Diagnostics.Debug.WriteLine($"Limited input locations from {locations.Count} to {maxInputLocations} (most recent)");
            }

            // Generate synthetic points for each actual location
            var allHeatMapPoints = new List<HeatMapPoint>();
            const int maxHeatMapPoints = 150; // Very low limit for MVP performance

            foreach (var location in locationsToProcess)
            {
                // Check if we've reached the maximum point limit
                if (allHeatMapPoints.Count >= maxHeatMapPoints)
                {
                    break;
                }

                try
                {
                    // Generate crowd points around this location
                    var crowdPoints = _crowdSimulator.GenerateCrowdPoints(location);
                    
                    // Add points up to the limit
                    var remainingCapacity = maxHeatMapPoints - allHeatMapPoints.Count;
                    var pointsToAdd = crowdPoints.Take(remainingCapacity);
                    allHeatMapPoints.AddRange(pointsToAdd);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error generating crowd points: {ex.Message}");
                    // If crowd generation fails, add the actual location as a fallback
                    if (allHeatMapPoints.Count < maxHeatMapPoints)
                    {
                        allHeatMapPoints.Add(new HeatMapPoint
                        {
                            Latitude = location.Latitude,
                            Longitude = location.Longitude,
                            Intensity = 0.7
                        });
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"Total synthetic points before grouping: {allHeatMapPoints.Count}");

            // Group overlapping points with less aggressive rounding to preserve crowd effect
            // Using 5 decimal places (~1.1 meters precision) instead of 4 (~11 meters)
            var groupedPoints = allHeatMapPoints
                .GroupBy(p => new { Lat = Math.Round(p.Latitude, 5), Lng = Math.Round(p.Longitude, 5) })
                .ToList();

            var finalHeatMapPoints = new List<HeatMapPoint>();
            foreach (var group in groupedPoints)
            {
                // For crowd visualization, we want to show individual points more
                // So we use average intensity rather than summing
                var avgIntensity = group.Average(p => p.Intensity);
                finalHeatMapPoints.Add(new HeatMapPoint
                {
                    Latitude = group.Key.Lat,
                    Longitude = group.Key.Lng,
                    Intensity = avgIntensity
                });
            }

            System.Diagnostics.Debug.WriteLine($"Final heatmap points after grouping: {finalHeatMapPoints.Count}");
            return finalHeatMapPoints;
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
