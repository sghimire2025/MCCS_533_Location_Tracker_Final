using LocationTrackerFinal.Models;

namespace LocationTrackerFinal.Services
{
    /// <summary>
    /// Interface for generating synthetic location points to simulate crowd density
    /// </summary>
    public interface ICrowdSimulator
    {
        /// <summary>
        /// Generates synthetic location points around an actual location to simulate crowd density
        /// </summary>
        /// <param name="actualLocation">The real tracked location</param>
        /// <param name="crowdDensity">Number of synthetic points to generate (5-10)</param>
        /// <param name="radiusMeters">Radius in meters for point distribution (10-20)</param>
        /// <returns>Collection of synthetic heatmap points</returns>
        List<HeatMapPoint> GenerateCrowdPoints(
            LocationPoint actualLocation,
            int crowdDensity = 7,
            double radiusMeters = 15.0);
    }
}
