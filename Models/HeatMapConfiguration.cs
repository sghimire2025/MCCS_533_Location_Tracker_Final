namespace LocationTrackerFinal.Models
{
    /// <summary>
    /// Configuration settings for heat map visualization
    /// </summary>
    public class HeatMapConfiguration
    {
        /// <summary>
        /// Radius of influence for each heat map point
        /// </summary>
        public double Radius { get; set; } = 20.0;

        /// <summary>
        /// Opacity of the heat map overlay (0.0 to 1.0)
        /// </summary>
        public double Opacity { get; set; } = 0.6;

        /// <summary>
        /// Gradient colors for heat map visualization (from low to high intensity)
        /// </summary>
        public string[] GradientColors { get; set; } = new[] { "#00FF00", "#FFFF00", "#FF0000" };
    }
}
