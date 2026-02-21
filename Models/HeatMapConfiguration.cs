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

        /// <summary>
        /// Core color for crowd heatmap visualization (blue)
        /// </summary>
        public string CoreColor { get; set; } = "#4285F4";

        /// <summary>
        /// Outer layer color for crowd heatmap visualization (light red)
        /// </summary>
        public string OuterLayerColor { get; set; } = "#FF6B6B";

        /// <summary>
        /// Opacity of the outer layer (0.0 to 1.0)
        /// </summary>
        public double OuterLayerOpacity { get; set; } = 0.3;

        /// <summary>
        /// Radius of the core circle in pixels
        /// </summary>
        public double CoreRadius { get; set; } = 8.0;

        /// <summary>
        /// Radius of the outer layer in pixels
        /// </summary>
        public double OuterRadius { get; set; } = 16.0;
    }
}
