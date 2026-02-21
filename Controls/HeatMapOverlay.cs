using Microsoft.Maui.Controls.Maps;
using LocationTrackerFinal.Models;
using System.Collections.Generic;
using MauiMap = Microsoft.Maui.Controls.Maps.Map;

namespace LocationTrackerFinal.Controls
{
    /// <summary>
    /// Custom map control that extends the standard MAUI Map to display heat map overlays
    /// based on location tracking data.
    /// </summary>
    public class HeatMapOverlay : MauiMap
    {
        /// <summary>
        /// Bindable property for the collection of heat map points to be rendered.
        /// </summary>
        public static readonly BindableProperty HeatMapPointsProperty =
            BindableProperty.Create(
                nameof(HeatMapPoints),
                typeof(IEnumerable<HeatMapPoint>),
                typeof(HeatMapOverlay),
                defaultValue: null,
                propertyChanged: OnHeatMapPointsChanged);

        /// <summary>
        /// Bindable property for the current position marker.
        /// </summary>
        public static readonly BindableProperty CurrentPositionProperty =
            BindableProperty.Create(
                nameof(CurrentPosition),
                typeof(LocationPoint),
                typeof(HeatMapOverlay),
                defaultValue: null,
                propertyChanged: OnCurrentPositionChanged);

        /// <summary>
        /// Bindable property for showing/hiding the current position marker.
        /// </summary>
        public static readonly BindableProperty ShowCurrentPositionProperty =
            BindableProperty.Create(
                nameof(ShowCurrentPosition),
                typeof(bool),
                typeof(HeatMapOverlay),
                defaultValue: false,
                propertyChanged: OnShowCurrentPositionChanged);

        /// <summary>
        /// Bindable property for the collection of path points (tracking history).
        /// </summary>
        public static readonly BindableProperty PathPointsProperty =
            BindableProperty.Create(
                nameof(PathPoints),
                typeof(IEnumerable<LocationPoint>),
                typeof(HeatMapOverlay),
                defaultValue: null,
                propertyChanged: OnPathPointsChanged);

        /// <summary>
        /// Gets or sets the collection of heat map points to be displayed on the map.
        /// </summary>
        public IEnumerable<HeatMapPoint> HeatMapPoints
        {
            get => (IEnumerable<HeatMapPoint>)GetValue(HeatMapPointsProperty);
            set => SetValue(HeatMapPointsProperty, value);
        }

        /// <summary>
        /// Gets or sets the current position to be displayed on the map.
        /// </summary>
        public LocationPoint CurrentPosition
        {
            get => (LocationPoint)GetValue(CurrentPositionProperty);
            set => SetValue(CurrentPositionProperty, value);
        }

        /// <summary>
        /// Gets or sets whether to show the current position marker.
        /// </summary>
        public bool ShowCurrentPosition
        {
            get => (bool)GetValue(ShowCurrentPositionProperty);
            set => SetValue(ShowCurrentPositionProperty, value);
        }

        /// <summary>
        /// Gets or sets the collection of path points (tracking history) to be displayed on the map.
        /// </summary>
        public IEnumerable<LocationPoint> PathPoints
        {
            get => (IEnumerable<LocationPoint>)GetValue(PathPointsProperty);
            set => SetValue(PathPointsProperty, value);
        }

        /// <summary>
        /// Property changed callback for HeatMapPoints.
        /// Triggers the platform-specific rendering when the heat map data changes.
        /// </summary>
        private static void OnHeatMapPointsChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is HeatMapOverlay overlay)
            {
                // Notify the handler that the heat map points have changed
                overlay.Handler?.UpdateValue(nameof(HeatMapPoints));
            }
        }

        /// <summary>
        /// Property changed callback for CurrentPosition.
        /// Triggers the platform-specific rendering when the current position changes.
        /// </summary>
        private static void OnCurrentPositionChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is HeatMapOverlay overlay)
            {
                // Notify the handler that the current position has changed
                overlay.Handler?.UpdateValue(nameof(CurrentPosition));
            }
        }

        /// <summary>
        /// Property changed callback for ShowCurrentPosition.
        /// Triggers the platform-specific rendering when the visibility changes.
        /// </summary>
        private static void OnShowCurrentPositionChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is HeatMapOverlay overlay)
            {
                // Notify the handler that the visibility has changed
                overlay.Handler?.UpdateValue(nameof(ShowCurrentPosition));
            }
        }

        /// <summary>
        /// Property changed callback for PathPoints.
        /// Triggers the platform-specific rendering when the path points change.
        /// </summary>
        private static void OnPathPointsChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is HeatMapOverlay overlay)
            {
                // Notify the handler that the path points have changed
                overlay.Handler?.UpdateValue(nameof(PathPoints));
            }
        }
    }
}
