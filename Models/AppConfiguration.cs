namespace LocationTrackerFinal.Models
{
    /// <summary>
    /// Application configuration settings
    /// </summary>
    public class AppConfiguration
    {
        /// <summary>
        /// Google Maps API key for accessing Google Directions API
        /// </summary>
        public string GoogleMapsApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Interval in milliseconds between location updates during tracking
        /// </summary>
        public int LocationUpdateIntervalMs { get; set; } = 2000;

        /// <summary>
        /// Path to the SQLite database file
        /// </summary>
        public string DatabasePath { get; set; } = string.Empty;

        /// <summary>
        /// Heat map visualization configuration
        /// </summary>
        public HeatMapConfiguration HeatMap { get; set; } = new HeatMapConfiguration();
    }
}
