namespace LocationTrackerFinal.Utilities
{
    /// <summary>
    /// Result of coordinate validation
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Indicates whether the validation passed
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Error message if validation failed
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Creates a successful validation result
        /// </summary>
        public static ValidationResult Success() => new ValidationResult { IsValid = true };

        /// <summary>
        /// Creates a failed validation result with an error message
        /// </summary>
        public static ValidationResult Failure(string errorMessage) => 
            new ValidationResult { IsValid = false, ErrorMessage = errorMessage };
    }

    /// <summary>
    /// Provides validation methods for geographic coordinates
    /// </summary>
    public static class CoordinateValidator
    {
        private const double MinLatitude = -90.0;
        private const double MaxLatitude = 90.0;
        private const double MinLongitude = -180.0;
        private const double MaxLongitude = 180.0;

        /// <summary>
        /// Validates a latitude value
        /// </summary>
        /// <param name="latitude">The latitude value to validate</param>
        /// <returns>Validation result indicating success or failure with error message</returns>
        public static ValidationResult ValidateLatitude(double latitude)
        {
            if (latitude < MinLatitude || latitude > MaxLatitude)
            {
                return ValidationResult.Failure(
                    $"Latitude must be between {MinLatitude} and {MaxLatitude} degrees. Provided value: {latitude}");
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates a longitude value
        /// </summary>
        /// <param name="longitude">The longitude value to validate</param>
        /// <returns>Validation result indicating success or failure with error message</returns>
        public static ValidationResult ValidateLongitude(double longitude)
        {
            if (longitude < MinLongitude || longitude > MaxLongitude)
            {
                return ValidationResult.Failure(
                    $"Longitude must be between {MinLongitude} and {MaxLongitude} degrees. Provided value: {longitude}");
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates a coordinate pair (latitude and longitude)
        /// </summary>
        /// <param name="latitude">The latitude value to validate</param>
        /// <param name="longitude">The longitude value to validate</param>
        /// <returns>Validation result indicating success or failure with error message</returns>
        public static ValidationResult ValidateCoordinates(double latitude, double longitude)
        {
            var latResult = ValidateLatitude(latitude);
            if (!latResult.IsValid)
            {
                return latResult;
            }

            var lngResult = ValidateLongitude(longitude);
            if (!lngResult.IsValid)
            {
                return lngResult;
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates origin and destination coordinate pairs
        /// </summary>
        /// <param name="originLat">Origin latitude</param>
        /// <param name="originLng">Origin longitude</param>
        /// <param name="destLat">Destination latitude</param>
        /// <param name="destLng">Destination longitude</param>
        /// <returns>Validation result indicating success or failure with error message</returns>
        public static ValidationResult ValidateOriginAndDestination(
            double originLat, 
            double originLng, 
            double destLat, 
            double destLng)
        {
            var originResult = ValidateCoordinates(originLat, originLng);
            if (!originResult.IsValid)
            {
                return ValidationResult.Failure($"Origin coordinates invalid: {originResult.ErrorMessage}");
            }

            var destResult = ValidateCoordinates(destLat, destLng);
            if (!destResult.IsValid)
            {
                return ValidationResult.Failure($"Destination coordinates invalid: {destResult.ErrorMessage}");
            }

            return ValidationResult.Success();
        }
    }
}
