using LocationTrackerFinal.Models;

namespace LocationTrackerFinal.Services
{
    /// <summary>
    /// Generates synthetic location points to simulate crowd density around actual tracked locations
    /// </summary>
    public class CrowdSimulator : ICrowdSimulator
    {
        private readonly Random _random;

        // Earth's radius in meters for coordinate calculations
        private const double EarthRadiusMeters = 6371000.0;

        public CrowdSimulator()
        {
            _random = new Random();
        }

        /// <summary>
        /// Generates synthetic location points around an actual location to simulate crowd density
        /// </summary>
        public List<HeatMapPoint> GenerateCrowdPoints(
            LocationPoint actualLocation,
            int crowdDensity = 3,
            double radiusMeters = 50.0)
        {
            // Validate and clamp parameters - REDUCED for MVP performance
            crowdDensity = Math.Clamp(crowdDensity, 2, 4);
            radiusMeters = radiusMeters > 0 ? radiusMeters : 50.0;

            var syntheticPoints = new List<HeatMapPoint>();

            // Generate synthetic points using random distribution
            for (int i = 0; i < crowdDensity; i++)
            {
                // Generate random angle (0 to 2π)
                double angle = _random.NextDouble() * 2 * Math.PI;

                // Generate random distance (0 to radiusMeters)
                // Use square root for uniform distribution in circular area
                double distance = Math.Sqrt(_random.NextDouble()) * radiusMeters;

                // Calculate offset in meters
                double deltaX = distance * Math.Cos(angle);
                double deltaY = distance * Math.Sin(angle);

                // Convert meter offsets to lat/lng offsets
                double deltaLat = deltaY / EarthRadiusMeters * (180.0 / Math.PI);
                double deltaLng = deltaX / (EarthRadiusMeters * Math.Cos(actualLocation.Latitude * Math.PI / 180.0)) * (180.0 / Math.PI);

                // Create synthetic point
                var syntheticPoint = new HeatMapPoint
                {
                    Latitude = actualLocation.Latitude + deltaLat,
                    Longitude = actualLocation.Longitude + deltaLng,
                    Intensity = 0.5 + (_random.NextDouble() * 0.5) // Random intensity between 0.5 and 1.0
                };

                syntheticPoints.Add(syntheticPoint);
            }

            return syntheticPoints;
        }
    }
}
