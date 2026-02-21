using SQLite;

namespace LocationTrackerFinal.Models
{
    public class LocationPoint
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public double Latitude { get; set; }

        [NotNull]
        public double Longitude { get; set; }

        [NotNull]
        public DateTime Timestamp { get; set; }

        [NotNull, Indexed]
        public int SessionId { get; set; }
    }
}
