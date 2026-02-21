using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using LocationTrackerFinal.Controls;
using LocationTrackerFinal.Models;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Maps.Handlers;
using System.Collections.Generic;
using System.Linq;

namespace LocationTrackerFinal.Platforms.Android.Handlers
{
    /// <summary>
    /// Android-specific handler for the HeatMapOverlay control.
    /// Uses Google Maps Android API to render markers for heat map visualization.
    /// </summary>
    public class HeatMapOverlayHandler : MapHandler
    {
        private List<Circle>? _heatMapCircles;
        private List<Circle>? _pathCircles;
        private Marker? _currentPositionMarker;
        private GoogleMap? _googleMap;

        public HeatMapOverlayHandler() : base(PropertyMapper)
        {
        }

        public static IPropertyMapper<HeatMapOverlay, HeatMapOverlayHandler> PropertyMapper = new PropertyMapper<HeatMapOverlay, HeatMapOverlayHandler>(Mapper)
        {
            [nameof(HeatMapOverlay.HeatMapPoints)] = MapHeatMapPoints,
            [nameof(HeatMapOverlay.CurrentPosition)] = MapCurrentPosition,
            [nameof(HeatMapOverlay.ShowCurrentPosition)] = MapShowCurrentPosition,
            [nameof(HeatMapOverlay.PathPoints)] = MapPathPoints,
        };

        public static void MapHeatMapPoints(HeatMapOverlayHandler handler, HeatMapOverlay overlay)
        {
            handler.UpdateHeatMap(overlay.HeatMapPoints);
        }

        public static void MapCurrentPosition(HeatMapOverlayHandler handler, HeatMapOverlay overlay)
        {
            handler.UpdateCurrentPositionMarker(overlay.CurrentPosition, overlay.ShowCurrentPosition);
        }

        public static void MapShowCurrentPosition(HeatMapOverlayHandler handler, HeatMapOverlay overlay)
        {
            handler.UpdateCurrentPositionMarker(overlay.CurrentPosition, overlay.ShowCurrentPosition);
        }

        public static void MapPathPoints(HeatMapOverlayHandler handler, HeatMapOverlay overlay)
        {
            handler.UpdatePathPoints(overlay.PathPoints);
        }

        protected override void ConnectHandler(MapView platformView)
        {
            base.ConnectHandler(platformView);

            try
            {
                platformView?.GetMapAsync(new MapReadyCallback(googleMap =>
                {
                    _googleMap = googleMap;
                    
                    if (VirtualView is HeatMapOverlay overlay)
                    {
                        if (overlay.HeatMapPoints != null)
                        {
                            UpdateHeatMap(overlay.HeatMapPoints);
                        }
                        
                        if (overlay.PathPoints != null)
                        {
                            UpdatePathPoints(overlay.PathPoints);
                        }
                        
                        UpdateCurrentPositionMarker(overlay.CurrentPosition, overlay.ShowCurrentPosition);
                    }
                }));
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error connecting map handler: {ex.Message}");
            }
        }

        private void UpdateHeatMap(IEnumerable<HeatMapPoint>? points)
        {
            try
            {
                if (_googleMap == null || points == null)
                    return;

                // Remove existing circles
                if (_heatMapCircles != null)
                {
                    foreach (var circle in _heatMapCircles)
                    {
                        circle?.Remove();
                    }
                    _heatMapCircles = null;
                }

                var pointsList = points.ToList();
                if (pointsList.Count == 0)
                    return;

                _heatMapCircles = new List<Circle>();

                // Create circles for each heat map point
                foreach (var point in pointsList)
                {
                    if (point == null)
                        continue;

                    var intensity = (float)point.Intensity;
                    
                    // Calculate color based on intensity (green -> yellow -> red)
                    int color;
                    if (intensity < 0.5)
                    {
                        // Green to Yellow
                        var ratio = intensity * 2;
                        color = global::Android.Graphics.Color.Argb(
                            (int)(100 + intensity * 100),
                            (int)(255 * ratio),
                            255,
                            0
                        );
                    }
                    else
                    {
                        // Yellow to Red
                        var ratio = (intensity - 0.5f) * 2;
                        color = global::Android.Graphics.Color.Argb(
                            (int)(100 + intensity * 100),
                            255,
                            (int)(255 * (1 - ratio)),
                            0
                        );
                    }

                    var circleOptions = new CircleOptions()
                        .InvokeCenter(new LatLng(point.Latitude, point.Longitude))
                        .InvokeRadius(50) // 50 meters radius
                        .InvokeFillColor(color)
                        .InvokeStrokeWidth(0);

                    var circle = _googleMap.AddCircle(circleOptions);
                    if (circle != null)
                    {
                        _heatMapCircles.Add(circle);
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating heat map: {ex.Message}");
            }
        }

        private void UpdateCurrentPositionMarker(LocationPoint? position, bool showMarker)
        {
            try
            {
                if (_googleMap == null)
                    return;

                if (_currentPositionMarker != null)
                {
                    _currentPositionMarker.Remove();
                    _currentPositionMarker = null;
                }

                if (position != null && showMarker)
                {
                    var markerOptions = new MarkerOptions()
                        .SetPosition(new LatLng(position.Latitude, position.Longitude))
                        .SetTitle("Current Position")
                        .SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueRed)); // Red marker (default)

                    _currentPositionMarker = _googleMap.AddMarker(markerOptions);
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating current position marker: {ex.Message}");
            }
        }

        private void UpdatePathPoints(IEnumerable<LocationPoint>? points)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"UpdatePathPoints called with {points?.Count() ?? 0} points");
                
                if (_googleMap == null || points == null)
                {
                    System.Diagnostics.Debug.WriteLine("GoogleMap is null or points is null");
                    return;
                }

                // Remove existing path circles
                if (_pathCircles != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Removing {_pathCircles.Count} existing path circles");
                    foreach (var circle in _pathCircles)
                    {
                        circle?.Remove();
                    }
                    _pathCircles = null;
                }

                var pointsList = points.ToList();
                if (pointsList.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Points list is empty");
                    return;
                }

                _pathCircles = new List<Circle>();
                System.Diagnostics.Debug.WriteLine($"Creating {pointsList.Count} blue circles");

                // Create blue circles for each path point
                foreach (var point in pointsList)
                {
                    if (point == null)
                        continue;

                    // Solid blue color for path tracking (matching reference image)
                    int blueColor = global::Android.Graphics.Color.Argb(255, 66, 133, 244); // Google Maps blue

                    var circleOptions = new CircleOptions()
                        .InvokeCenter(new LatLng(point.Latitude, point.Longitude))
                        .InvokeRadius(50) // Increased radius for better visibility
                        .InvokeFillColor(blueColor)
                        .InvokeStrokeWidth(0); // No border for cleaner look

                    var circle = _googleMap.AddCircle(circleOptions);
                    if (circle != null)
                    {
                        _pathCircles.Add(circle);
                        System.Diagnostics.Debug.WriteLine($"Added circle at {point.Latitude}, {point.Longitude}");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"Total path circles added: {_pathCircles.Count}");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating path points: {ex.Message}");
            }
        }

        protected override void DisconnectHandler(MapView platformView)
        {
            try
            {
                if (_heatMapCircles != null)
                {
                    foreach (var circle in _heatMapCircles)
                    {
                        circle?.Remove();
                    }
                    _heatMapCircles = null;
                }

                if (_pathCircles != null)
                {
                    foreach (var circle in _pathCircles)
                    {
                        circle?.Remove();
                    }
                    _pathCircles = null;
                }

                if (_currentPositionMarker != null)
                {
                    _currentPositionMarker.Remove();
                    _currentPositionMarker = null;
                }

                _googleMap = null;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disconnecting map handler: {ex.Message}");
            }
            finally
            {
                base.DisconnectHandler(platformView);
            }
        }

        private class MapReadyCallback : Java.Lang.Object, IOnMapReadyCallback
        {
            private readonly Action<GoogleMap> _onMapReady;

            public MapReadyCallback(Action<GoogleMap> onMapReady)
            {
                _onMapReady = onMapReady;
            }

            public void OnMapReady(GoogleMap googleMap)
            {
                _onMapReady?.Invoke(googleMap);
            }
        }
    }
}
