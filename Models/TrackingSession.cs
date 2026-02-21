using SQLite;

namespace LocationTrackerFinal.Models
{
    public class TrackingSession
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        [NotNull]
        public double OriginLatitude { get; set; }

        [NotNull]
        public double OriginLongitude { get; set; }

        [NotNull]
        public double DestinationLatitude { get; set; }

        [NotNull]
        public double DestinationLongitude { get; set; }

        [NotNull]
        public bool IsActive { get; set; }
    }
}
