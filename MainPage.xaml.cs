using LocationTrackerFinal.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace LocationTrackerFinal;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        
        // Set up data binding
        BindingContext = viewModel;

        // Set initial map center and zoom level (San Francisco as default)
        var initialLocation = new Location(37.7749, -122.4194);
        var mapSpan = new MapSpan(initialLocation, 0.1, 0.1); // latitudeDegrees, longitudeDegrees
        HeatMap.MoveToRegion(mapSpan);
    }
}
