namespace LocationTrackerFinal.Models
{
    public class DirectionsResponse
    {
        public List<RoutePoint> RoutePoints { get; set; } = new List<RoutePoint>();

        public string Status { get; set; } = string.Empty;

        public double TotalDistance { get; set; }

        public int TotalDuration { get; set; }
    }
}
