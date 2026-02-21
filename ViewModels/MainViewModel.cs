using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LocationTrackerFinal.Models;
using LocationTrackerFinal.Services;

namespace LocationTrackerFinal.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ILocationTrackingService _trackingService;
        private readonly IHeatMapService _heatMapService;

        private bool _isTracking;
        private string _errorMessage = string.Empty;
        private LocationPoint? _currentPosition;
        private IEnumerable<LocationPoint>? _pathPointsForBinding;
        private bool _isHeatmapEnabled;
        private int _locationUpdatesSinceLastHeatmapRefresh = 0;
        private const int HeatmapRefreshInterval = 5; // Refresh heatmap every 5 location updates

        // Hardcoded coordinates for MVP
        //Van Ness Ave & Fell St to 2229-2201 Geary Blvd
        private readonly double _originLatitude = 37.776345;      // Van Ness Ave & Fell St
        private readonly double _originLongitude = -122.419663;

        private readonly double _destinationLatitude = 37.783227;  // 2229-2201 Geary Blvd
        private readonly double _destinationLongitude = -122.439540;

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainViewModel(ILocationTrackingService trackingService, IHeatMapService heatMapService)
        {
            _trackingService = trackingService ?? throw new ArgumentNullException(nameof(trackingService));
            _heatMapService = heatMapService ?? throw new ArgumentNullException(nameof(heatMapService));

            PathPoints = new ObservableCollection<LocationPoint>();

            StartTrackingCommand = new Command(async () => await StartTracking());
            StopTrackingCommand = new Command(async () => await StopTracking());
            ToggleHeatmapCommand = new Command(() => IsHeatmapEnabled = !IsHeatmapEnabled);

            // Subscribe to tracking service events
            _trackingService.LocationUpdated += OnLocationUpdated;
            _trackingService.ErrorOccurred += OnErrorOccurred;
        }

        public bool IsTracking
        {
            get => _isTracking;
            set
            {
                if (_isTracking != value)
                {
                    _isTracking = value;
                    
                    // Ensure UI updates happen on main thread
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        OnPropertyChanged();
                    });
                }
            }
        }

        private IEnumerable<HeatMapPoint>? _heatMapData;

        public IEnumerable<HeatMapPoint>? HeatMapData
        {
            get => _heatMapData;
            set
            {
                if (_heatMapData != value)
                {
                    _heatMapData = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public ObservableCollection<LocationPoint> PathPoints { get; }
        
        public IEnumerable<LocationPoint>? PathPointsForBinding
        {
            get => _pathPointsForBinding;
            set
            {
                if (_pathPointsForBinding != value)
                {
                    _pathPointsForBinding = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public LocationPoint? CurrentPosition
        {
            get => _currentPosition;
            set
            {
                if (_currentPosition != value)
                {
                    _currentPosition = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand StartTrackingCommand { get; }
        public ICommand StopTrackingCommand { get; }
        public ICommand ToggleHeatmapCommand { get; }

        public bool IsHeatmapEnabled
        {
            get => _isHeatmapEnabled;
            set
            {
                if (_isHeatmapEnabled != value)
                {
                    System.Diagnostics.Debug.WriteLine($"IsHeatmapEnabled changing from {_isHeatmapEnabled} to {value}");
                    _isHeatmapEnabled = value;
                    _heatMapService.SetCrowdSimulationEnabled(value);
                    System.Diagnostics.Debug.WriteLine($"Called SetCrowdSimulationEnabled({value})");
                    OnPropertyChanged();
                    
                    // Refresh heatmap visualization
                    // When disabling, this will clear synthetic points from memory
                    System.Diagnostics.Debug.WriteLine("About to call RefreshHeatMapAsync");
                    _ = RefreshHeatMapAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"IsHeatmapEnabled not changing, already {value}");
                }
            }
        }

        private async Task StartTracking()
        {
            // Clear any previous error messages
            ErrorMessage = string.Empty;

            // Call tracking service with hardcoded coordinates
            var success = await _trackingService.StartTrackingAsync(
                _originLatitude,
                _originLongitude,
                _destinationLatitude,
                _destinationLongitude);

            if (success)
            {
                // Clear path points when starting new tracking
                PathPoints.Clear();
                
                // Reset heatmap refresh counter
                _locationUpdatesSinceLastHeatmapRefresh = 0;
                
                // Update UI state
                IsTracking = true;
            }
        }

        private async Task StopTracking()
        {
            // Stop tracking service
            await _trackingService.StopTrackingAsync();

            // Update UI state
            IsTracking = false;

            // Clear current position when tracking stops
            CurrentPosition = null;

            // Refresh heat map data
            await RefreshHeatMapAsync();
        }

        public async Task RefreshHeatMapAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"RefreshHeatMapAsync called. IsHeatmapEnabled: {IsHeatmapEnabled}");
                System.Diagnostics.Debug.WriteLine($"HeatMapService.IsCrowdSimulationEnabled: {_heatMapService.IsCrowdSimulationEnabled}");
                
                // Only generate heatmap if crowd simulation is enabled
                if (!IsHeatmapEnabled)
                {
                    System.Diagnostics.Debug.WriteLine("RefreshHeatMapAsync: Heatmap is disabled, clearing data");
                    HeatMapData = null;
                    return;
                }
                
                // Use session-based heatmap when tracking is active to avoid rendering too many points
                List<HeatMapPoint> heatMapPoints;
                if (IsTracking && _trackingService.CurrentSessionId > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"RefreshHeatMapAsync: Using session-based heatmap for session {_trackingService.CurrentSessionId}");
                    heatMapPoints = await _heatMapService.GenerateHeatMapDataForSessionWithCrowdAsync(_trackingService.CurrentSessionId);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("RefreshHeatMapAsync: Using all-data heatmap (tracking not active)");
                    heatMapPoints = await _heatMapService.GenerateHeatMapDataWithCrowdAsync();
                }

                System.Diagnostics.Debug.WriteLine($"RefreshHeatMapAsync: Received {heatMapPoints.Count} heatmap points");

                // Replace the entire collection to trigger binding update
                HeatMapData = heatMapPoints;
                
                System.Diagnostics.Debug.WriteLine($"RefreshHeatMapAsync: HeatMapData property set with {heatMapPoints.Count} points");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to refresh heat map: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"RefreshHeatMapAsync ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private async void OnLocationUpdated(object? sender, LocationPoint e)
        {
            // Ensure UI updates happen on main thread
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // If there's a current position, add it to the path (it's now a previous position)
                if (CurrentPosition != null)
                {
                    PathPoints.Add(CurrentPosition);
                    // Update the binding property to trigger map update
                    PathPointsForBinding = PathPoints.ToList();
                }
                
                // Update current position to the new location
                CurrentPosition = e;
                
                // Refresh heat map periodically (not on every update) to reduce load
                if (IsHeatmapEnabled)
                {
                    _locationUpdatesSinceLastHeatmapRefresh++;
                    if (_locationUpdatesSinceLastHeatmapRefresh >= HeatmapRefreshInterval)
                    {
                        _locationUpdatesSinceLastHeatmapRefresh = 0;
                        await RefreshHeatMapAsync();
                    }
                }
            });
        }

        private void OnErrorOccurred(object? sender, string e)
        {
            // Ensure UI updates happen on main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Display error message
                ErrorMessage = e;
            });
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
