using LocationTrackerFinal.Data;
using LocationTrackerFinal.Models;
using LocationTrackerFinal.Utilities;
using Microsoft.Extensions.Logging;

namespace LocationTrackerFinal.Services
{
    public class LocationTrackingService : ILocationTrackingService
    {
        private readonly IGoogleDirectionsService _directionsService;
        private readonly ILocationRepository _repository;
        private readonly ErrorHandler _errorHandler;
        private readonly int _updateIntervalMs;
        private CancellationTokenSource? _trackingCts;
        private List<RoutePoint> _currentRoute = new List<RoutePoint>();
        private int _currentRouteIndex;
        private int _currentSessionId;

        public bool IsTracking { get; private set; }
        public int CurrentSessionId => _currentSessionId;

        public event EventHandler<LocationPoint>? LocationUpdated;
        public event EventHandler<string>? ErrorOccurred;

        public LocationTrackingService(
            IGoogleDirectionsService directionsService,
            ILocationRepository repository,
            ILogger<LocationTrackingService> logger,
            int updateIntervalMs = 2000)
        {
            _directionsService = directionsService ?? throw new ArgumentNullException(nameof(directionsService));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _errorHandler = new ErrorHandler(logger);
            _updateIntervalMs = updateIntervalMs;
        }

        public async Task<bool> StartTrackingAsync(
            double originLat,
            double originLng,
            double destLat,
            double destLng)
        {
            if (IsTracking)
            {
                ErrorOccurred?.Invoke(this, "Tracking is already active.");
                return false;
            }

            var result = await _errorHandler.ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    // Fetch route from Google Directions API
                    var directionsResponse = await _directionsService.GetDirectionsAsync(
                        originLat, originLng, destLat, destLng);

                    // Check if API request was successful
                    if (directionsResponse.Status != "OK" || directionsResponse.RoutePoints.Count == 0)
                    {
                        ErrorOccurred?.Invoke(this, $"Failed to get route: {directionsResponse.Status}");
                        return false;
                    }

                    // Store the route
                    _currentRoute = directionsResponse.RoutePoints;
                    _currentRouteIndex = 0;

                    // Create new tracking session
                    var session = new TrackingSession
                    {
                        StartTime = DateTime.UtcNow,
                        OriginLatitude = originLat,
                        OriginLongitude = originLng,
                        DestinationLatitude = destLat,
                        DestinationLongitude = destLng,
                        IsActive = true
                    };

                    _currentSessionId = await _repository.CreateSessionAsync(session);

                    // Start tracking
                    IsTracking = true;
                    _trackingCts = new CancellationTokenSource();

                    // Start simulation loop in background
                    _ = Task.Run(() => SimulateMovementAsync(_trackingCts.Token));

                    return true;
                },
                "StartTracking",
                errorMessage => ErrorOccurred?.Invoke(this, errorMessage));

            return result.IsSuccess && result.Value;
        }

        public async Task StopTrackingAsync()
        {
            if (!IsTracking)
            {
                return;
            }

            await _errorHandler.ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    // Cancel the tracking loop
                    _trackingCts?.Cancel();
                    IsTracking = false;

                    // Update session end time
                    var sessions = await _repository.GetLocationsBySessionAsync(_currentSessionId);
                    if (sessions.Count > 0)
                    {
                        // Get the actual session to preserve all fields
                        var activeSession = await _repository.GetActiveSessionAsync();
                        if (activeSession != null)
                        {
                            activeSession.EndTime = DateTime.UtcNow;
                            activeSession.IsActive = false;
                            await _repository.UpdateSessionAsync(activeSession);
                        }
                    }

                    // Clean up
                    _trackingCts?.Dispose();
                    _trackingCts = null;
                },
                "StopTracking",
                errorMessage => ErrorOccurred?.Invoke(this, errorMessage));
        }

        private async Task SimulateMovementAsync(CancellationToken cancellationToken)
        {
            await _errorHandler.ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        // Check if we have more route points to process
                        if (_currentRouteIndex < _currentRoute.Count)
                        {
                            var routePoint = _currentRoute[_currentRouteIndex];

                            // Create location point
                            var locationPoint = new LocationPoint
                            {
                                Latitude = routePoint.Latitude,
                                Longitude = routePoint.Longitude,
                                Timestamp = DateTime.UtcNow,
                                SessionId = _currentSessionId
                            };

                            // Save to database
                            await _repository.SaveLocationAsync(locationPoint);

                            // Emit LocationUpdated event
                            LocationUpdated?.Invoke(this, locationPoint);

                            // Move to next point
                            _currentRouteIndex++;
                        }
                        else
                        {
                            // Destination reached, but continue tracking
                            // Stay at the last position and keep emitting updates
                            if (_currentRoute.Count > 0)
                            {
                                var lastPoint = _currentRoute[_currentRoute.Count - 1];
                                var locationPoint = new LocationPoint
                                {
                                    Latitude = lastPoint.Latitude,
                                    Longitude = lastPoint.Longitude,
                                    Timestamp = DateTime.UtcNow,
                                    SessionId = _currentSessionId
                                };

                                await _repository.SaveLocationAsync(locationPoint);
                                LocationUpdated?.Invoke(this, locationPoint);
                            }
                        }

                        // Wait for the next update interval
                        await Task.Delay(_updateIntervalMs, cancellationToken);
                    }
                },
                "SimulateMovement",
                errorMessage =>
                {
                    ErrorOccurred?.Invoke(this, errorMessage);
                    IsTracking = false;
                });
        }
    }
}
