using CoreGraphics;
using CoreLocation;
using LocationTrackerFinal.Controls;
using LocationTrackerFinal.Models;
using MapKit;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Maps.Handlers;
using System.Collections.Generic;
using System.Linq;
using UIKit;

namespace LocationTrackerFinal.Platforms.iOS.Handlers
{
    /// <summary>
    /// iOS-specific handler for the HeatMapOverlay control.
    /// Uses MapKit with custom MKOverlay and MKOverlayRenderer to render heat map overlays.
    /// </summary>
    public class HeatMapOverlayHandler : MapHandler
    {
        private HeatMapMKOverlay? _heatMapOverlay;
        private HeatMapOverlayRenderer? _heatMapRenderer;
        private MKPointAnnotation? _currentPositionAnnotation;

        /// <summary>
        /// Maps the cross-platform HeatMapOverlay control to the iOS MapHandler.
        /// </summary>
        public static void MapHeatMapPoints(IMapHandler handler, IMap map)
        {
            if (handler is HeatMapOverlayHandler heatMapHandler && map is HeatMapOverlay overlay)
            {
                heatMapHandler.UpdateHeatMap(overlay.HeatMapPoints);
            }
        }

        /// <summary>
        /// Maps the current position property to update the position marker.
        /// </summary>
        public static void MapCurrentPosition(IMapHandler handler, IMap map)
        {
            if (handler is HeatMapOverlayHandler heatMapHandler && map is HeatMapOverlay overlay)
            {
                heatMapHandler.UpdateCurrentPositionMarker(overlay.CurrentPosition, overlay.ShowCurrentPosition);
            }
        }

        /// <summary>
        /// Maps the show current position property to update marker visibility.
        /// </summary>
        public static void MapShowCurrentPosition(IMapHandler handler, IMap map)
        {
            if (handler is HeatMapOverlayHandler heatMapHandler && map is HeatMapOverlay overlay)
            {
                heatMapHandler.UpdateCurrentPositionMarker(overlay.CurrentPosition, overlay.ShowCurrentPosition);
            }
        }

        /// <summary>
        /// Called when the handler is connected to the platform view.
        /// Sets up the MKMapView and configures the heat map rendering.
        /// </summary>
        protected override void ConnectHandler(MKMapView platformView)
        {
            base.ConnectHandler(platformView);

            // Set up the overlay renderer delegate
            platformView.OverlayRenderer = GetOverlayRenderer;

            // Initial heat map rendering will happen when HeatMapPoints is set
            if (VirtualView is HeatMapOverlay overlay && overlay.HeatMapPoints != null)
            {
                UpdateHeatMap(overlay.HeatMapPoints);
            }

            // Initial position marker rendering
            if (VirtualView is HeatMapOverlay overlayPos)
            {
                UpdateCurrentPositionMarker(overlayPos.CurrentPosition, overlayPos.ShowCurrentPosition);
            }
        }

        /// <summary>
        /// Updates the heat map overlay with new data points.
        /// </summary>
        private void UpdateHeatMap(IEnumerable<HeatMapPoint>? points)
        {
            if (PlatformView == null || points == null)
                return;

            // Remove existing heat map overlay if present
            if (_heatMapOverlay != null)
            {
                PlatformView.RemoveOverlay(_heatMapOverlay);
                _heatMapOverlay = null;
                _heatMapRenderer = null;
            }

            var pointsList = points.ToList();
            if (pointsList.Count == 0)
                return;

            // Create new heat map overlay with the data points
            _heatMapOverlay = new HeatMapMKOverlay(pointsList);
            PlatformView.AddOverlay(_heatMapOverlay);
        }

        /// <summary>
        /// Provides the appropriate renderer for map overlays.
        /// </summary>
        private MKOverlayRenderer GetOverlayRenderer(MKMapView mapView, IMKOverlay overlay)
        {
            if (overlay is HeatMapMKOverlay heatMapOverlay)
            {
                _heatMapRenderer = new HeatMapOverlayRenderer(heatMapOverlay);
                return _heatMapRenderer;
            }

            return new MKOverlayRenderer(overlay);
        }

        /// <summary>
        /// Updates the current position marker on the map.
        /// </summary>
        /// <param name="position">The current location point.</param>
        /// <param name="showMarker">Whether to show the marker.</param>
        private void UpdateCurrentPositionMarker(LocationPoint? position, bool showMarker)
        {
            if (PlatformView == null)
                return;

            // Remove existing annotation if present
            if (_currentPositionAnnotation != null)
            {
                PlatformView.RemoveAnnotation(_currentPositionAnnotation);
                _currentPositionAnnotation = null;
            }

            // Add new annotation if position is valid and should be shown
            if (position != null && showMarker)
            {
                _currentPositionAnnotation = new MKPointAnnotation
                {
                    Coordinate = new CLLocationCoordinate2D(position.Latitude, position.Longitude),
                    Title = "Current Position"
                };

                PlatformView.AddAnnotation(_currentPositionAnnotation);
            }
        }

        /// <summary>
        /// Called when the handler is disconnected from the platform view.
        /// Cleans up the heat map overlay resources.
        /// </summary>
        protected override void DisconnectHandler(MKMapView platformView)
        {
            if (_heatMapOverlay != null)
            {
                platformView.RemoveOverlay(_heatMapOverlay);
                _heatMapOverlay = null;
                _heatMapRenderer = null;
            }

            if (_currentPositionAnnotation != null)
            {
                platformView.RemoveAnnotation(_currentPositionAnnotation);
                _currentPositionAnnotation = null;
            }

            platformView.OverlayRenderer = null;
            base.DisconnectHandler(platformView);
        }
    }

    /// <summary>
    /// Custom MKOverlay implementation for heat map data.
    /// </summary>
    internal class HeatMapMKOverlay : MKOverlay
    {
        private readonly List<HeatMapPoint> _points;
        private readonly MKMapRect _boundingMapRect;
        private readonly CLLocationCoordinate2D _coordinate;

        public HeatMapMKOverlay(List<HeatMapPoint> points)
        {
            _points = points;

            // Calculate bounding box for all points
            if (points.Count > 0)
            {
                double minLat = points.Min(p => p.Latitude);
                double maxLat = points.Max(p => p.Latitude);
                double minLon = points.Min(p => p.Longitude);
                double maxLon = points.Max(p => p.Longitude);

                var topLeft = MKMapPoint.FromCoordinate(new CLLocationCoordinate2D(maxLat, minLon));
                var bottomRight = MKMapPoint.FromCoordinate(new CLLocationCoordinate2D(minLat, maxLon));

                _boundingMapRect = new MKMapRect(
                    topLeft.X,
                    topLeft.Y,
                    bottomRight.X - topLeft.X,
                    bottomRight.Y - topLeft.Y);

                _coordinate = new CLLocationCoordinate2D(
                    (minLat + maxLat) / 2,
                    (minLon + maxLon) / 2);
            }
            else
            {
                _boundingMapRect = MKMapRect.World;
                _coordinate = new CLLocationCoordinate2D(0, 0);
            }
        }

        public List<HeatMapPoint> Points => _points;

        public override MKMapRect BoundingMapRect => _boundingMapRect;

        public override CLLocationCoordinate2D Coordinate => _coordinate;
    }

    /// <summary>
    /// Custom MKOverlayRenderer for rendering heat map visualization.
    /// </summary>
    internal class HeatMapOverlayRenderer : MKOverlayRenderer
    {
        private readonly HeatMapMKOverlay _heatMapOverlay;

        public HeatMapOverlayRenderer(HeatMapMKOverlay overlay) : base(overlay)
        {
            _heatMapOverlay = overlay;
        }

        public override void DrawMapRect(MKMapRect mapRect, nfloat zoomScale, CGContext context)
        {
            var points = _heatMapOverlay.Points;
            if (points.Count == 0)
                return;

            // Set up drawing context
            context.SetAlpha(0.6f);

            // Define gradient colors (green -> yellow -> red)
            var colorSpace = CGColorSpace.CreateDeviceRGB();
            var colors = new CGColor[]
            {
                new CGColor(0, 1, 0, 1),    // Green
                new CGColor(1, 1, 0, 1),    // Yellow
                new CGColor(1, 0, 0, 1)     // Red
            };
            var locations = new nfloat[] { 0.0f, 0.5f, 1.0f };
            var gradient = new CGGradient(colorSpace, colors, locations);

            // Draw heat map points
            foreach (var point in points)
            {
                var coordinate = new CLLocationCoordinate2D(point.Latitude, point.Longitude);
                var mapPoint = MKMapPoint.FromCoordinate(coordinate);
                var pointInView = PointForMapPoint(mapPoint);

                // Calculate radius based on intensity and zoom level
                var radius = (nfloat)(20.0 * point.Intensity / zoomScale);
                if (radius < 5) radius = 5;

                // Draw radial gradient for each point
                var center = new CGPoint(pointInView.X, pointInView.Y);
                context.DrawRadialGradient(
                    gradient,
                    center, 0,
                    center, radius,
                    CGGradientDrawingOptions.DrawsAfterEndLocation);
            }
        }

        public override bool CanDrawMapRect(MKMapRect mapRect, nfloat zoomScale)
        {
            return true;
        }
    }
}
