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

            HeatMapData = new ObservableCollection<HeatMapPoint>();
            PathPoints = new ObservableCollection<LocationPoint>();

            StartTrackingCommand = new Command(async () => await StartTracking());
            StopTrackingCommand = new Command(async () => await StopTracking());

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

        public ObservableCollection<HeatMapPoint> HeatMapData { get; }
        
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
                // Call heat map service to get latest data
                var heatMapPoints = await _heatMapService.GenerateHeatMapDataAsync();

                // Update HeatMapData observable collection
                HeatMapData.Clear();
                foreach (var point in heatMapPoints)
                {
                    HeatMapData.Add(point);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to refresh heat map: {ex.Message}";
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
                
                // Refresh heat map in real-time when location is updated
                await RefreshHeatMapAsync();
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
