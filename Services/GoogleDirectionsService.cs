using LocationTrackerFinal.Models;
using LocationTrackerFinal.Utilities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LocationTrackerFinal.Services
{
    public class GoogleDirectionsService : IGoogleDirectionsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ErrorHandler _errorHandler;
        private readonly ILogger<GoogleDirectionsService> _logger;

        public GoogleDirectionsService(HttpClient httpClient, string apiKey, ILogger<GoogleDirectionsService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _apiKey = apiKey ?? string.Empty;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHandler = new ErrorHandler(logger);
        }

        public async Task<DirectionsResponse> GetDirectionsAsync(
            double originLat,
            double originLng,
            double destLat,
            double destLng)
        {
            var result = await _errorHandler.ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    // Build the Google Directions API URL
                    var url = BuildApiUrl(originLat, originLng, destLat, destLng);
                    _logger.LogInformation($"Requesting directions from: {url}");

                    // Make HTTP GET request
                    var response = await _httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    // Read and parse JSON response
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"API Response: {jsonContent.Substring(0, Math.Min(500, jsonContent.Length))}...");
                    
                    var apiResponse = JsonSerializer.Deserialize<GoogleDirectionsApiResponse>(jsonContent, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });

                    // Check API status
                    if (apiResponse?.Status != "OK")
                    {
                        var errorMessage = apiResponse?.Status ?? "UNKNOWN_ERROR";
                        
                        // Provide more detailed error messages
                        var detailedError = errorMessage switch
                        {
                            "REQUEST_DENIED" => "API key is invalid or Directions API is not enabled",
                            "OVER_QUERY_LIMIT" => "API quota exceeded",
                            "ZERO_RESULTS" => "No route found between the specified locations",
                            "NOT_FOUND" => "One or more locations could not be geocoded",
                            "INVALID_REQUEST" => "Invalid request parameters",
                            _ => errorMessage
                        };
                        
                        // Include API error message if available
                        if (!string.IsNullOrEmpty(apiResponse?.ErrorMessage))
                        {
                            detailedError += $": {apiResponse.ErrorMessage}";
                        }
                        
                        _logger.LogError($"Directions API error: {detailedError}");
                        
                        return new DirectionsResponse
                        {
                            Status = detailedError,
                            RoutePoints = new List<RoutePoint>()
                        };
                    }

                    // Extract route points from polyline
                    var routePoints = new List<RoutePoint>();
                    if (apiResponse.Routes != null && apiResponse.Routes.Count > 0)
                    {
                        var route = apiResponse.Routes[0];
                        
                        // Extract distance and duration
                        double totalDistance = 0;
                        int totalDuration = 0;
                        
                        if (route.Legs != null && route.Legs.Count > 0)
                        {
                            foreach (var leg in route.Legs)
                            {
                                totalDistance += leg.Distance?.Value ?? 0;
                                totalDuration += leg.Duration?.Value ?? 0;
                            }
                        }

                        // Decode polyline
                        if (route.OverviewPolyline?.Points != null)
                        {
                            routePoints = DecodePolyline(route.OverviewPolyline.Points);
                        }

                        return new DirectionsResponse
                        {
                            Status = "OK",
                            RoutePoints = routePoints,
                            TotalDistance = totalDistance,
                            TotalDuration = totalDuration
                        };
                    }

                    return new DirectionsResponse
                    {
                        Status = "NO_ROUTES",
                        RoutePoints = new List<RoutePoint>()
                    };
                },
                "GetDirections");

            // If error occurred, return error response
            if (!result.IsSuccess)
            {
                return new DirectionsResponse
                {
                    Status = $"ERROR: {result.ErrorMessage}",
                    RoutePoints = new List<RoutePoint>()
                };
            }

            return result.Value ?? new DirectionsResponse
            {
                Status = "UNKNOWN_ERROR",
                RoutePoints = new List<RoutePoint>()
            };
        }

        private string BuildApiUrl(double originLat, double originLng, double destLat, double destLng)
        {
            return $"https://maps.googleapis.com/maps/api/directions/json?origin={originLat},{originLng}&destination={destLat},{destLng}&key={_apiKey}";
        }

        private List<RoutePoint> DecodePolyline(string encodedPolyline)
        {
            if (string.IsNullOrEmpty(encodedPolyline))
                return new List<RoutePoint>();

            var polylineChars = encodedPolyline.ToCharArray();
            var index = 0;
            var currentLat = 0;
            var currentLng = 0;
            var points = new List<RoutePoint>();

            while (index < polylineChars.Length)
            {
                // Decode latitude
                int sum = 0;
                int shifter = 0;
                int nextFiveBits;
                do
                {
                    nextFiveBits = polylineChars[index++] - 63;
                    sum |= (nextFiveBits & 31) << shifter;
                    shifter += 5;
                } while (nextFiveBits >= 32 && index < polylineChars.Length);

                if (index >= polylineChars.Length && nextFiveBits >= 32)
                    break;

                currentLat += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

                // Decode longitude
                sum = 0;
                shifter = 0;
                do
                {
                    nextFiveBits = polylineChars[index++] - 63;
                    sum |= (nextFiveBits & 31) << shifter;
                    shifter += 5;
                } while (nextFiveBits >= 32 && index < polylineChars.Length);

                if (index > polylineChars.Length && nextFiveBits >= 32)
                    break;

                currentLng += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

                points.Add(new RoutePoint
                {
                    Latitude = currentLat / 1E5,
                    Longitude = currentLng / 1E5
                });
            }

            return points;
        }

        // Internal classes for JSON deserialization
        private class GoogleDirectionsApiResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("status")]
            public string Status { get; set; } = string.Empty;
            
            [System.Text.Json.Serialization.JsonPropertyName("routes")]
            public List<Route>? Routes { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("error_message")]
            public string? ErrorMessage { get; set; }
        }

        private class Route
        {
            [System.Text.Json.Serialization.JsonPropertyName("legs")]
            public List<Leg>? Legs { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("overview_polyline")]
            public Polyline? OverviewPolyline { get; set; }
        }

        private class Leg
        {
            [System.Text.Json.Serialization.JsonPropertyName("distance")]
            public Distance? Distance { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("duration")]
            public Duration? Duration { get; set; }
        }

        private class Distance
        {
            [System.Text.Json.Serialization.JsonPropertyName("value")]
            public double Value { get; set; }
        }

        private class Duration
        {
            [System.Text.Json.Serialization.JsonPropertyName("value")]
            public int Value { get; set; }
        }

        private class Polyline
        {
            [System.Text.Json.Serialization.JsonPropertyName("points")]
            public string? Points { get; set; }
        }
    }
}
