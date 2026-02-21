using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Maps;
using LocationTrackerFinal.Data;
using LocationTrackerFinal.Services;
using LocationTrackerFinal.ViewModels;
using LocationTrackerFinal.Controls;
using LocationTrackerFinal.Models;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Handlers;
using Microsoft.Extensions.Configuration;
using System.Reflection;

#if ANDROID
using LocationTrackerFinal.Platforms.Android.Handlers;
#elif IOS
using LocationTrackerFinal.Platforms.iOS.Handlers;
#endif

namespace LocationTrackerFinal;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiMaps()
			.ConfigureMauiHandlers(handlers =>
			{
#if ANDROID || IOS
				// Register the custom HeatMapOverlay handler
				handlers.AddHandler<HeatMapOverlay, HeatMapOverlayHandler>();
#endif
			})
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Load configuration from embedded appsettings.json
		var assembly = Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream("LocationTrackerFinal.appsettings.json");
		
		var config = new ConfigurationBuilder()
			.AddJsonStream(stream!)
			.Build();

		// Bind configuration to AppConfiguration model
		var appConfig = new AppConfiguration();
		config.Bind(appConfig);
		
		// Set database path to use app data directory
		appConfig.DatabasePath = Path.Combine(FileSystem.AppDataDirectory, appConfig.DatabasePath);
		
		// Register configuration as singleton
		builder.Services.AddSingleton(appConfig);

		// Configure HttpClient with appropriate timeout
		builder.Services.AddHttpClient("GoogleDirections", client =>
		{
			client.Timeout = TimeSpan.FromSeconds(30);
		});

		// Register ILocationRepository with singleton lifetime
		builder.Services.AddSingleton<ILocationRepository>(sp => 
		{
			var logger = sp.GetRequiredService<ILogger<LocationRepository>>();
			var configuration = sp.GetRequiredService<AppConfiguration>();
			return new LocationRepository(configuration.DatabasePath, logger);
		});
		
		// Register IGoogleDirectionsService with transient lifetime
		builder.Services.AddTransient<IGoogleDirectionsService>(sp =>
		{
			var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
			var httpClient = httpClientFactory.CreateClient("GoogleDirections");
			var logger = sp.GetRequiredService<ILogger<GoogleDirectionsService>>();
			var configuration = sp.GetRequiredService<AppConfiguration>();
			return new GoogleDirectionsService(httpClient, configuration.GoogleMapsApiKey, logger);
		});
		
		// Register ILocationTrackingService with singleton lifetime
		builder.Services.AddSingleton<ILocationTrackingService>(sp =>
		{
			var directionsService = sp.GetRequiredService<IGoogleDirectionsService>();
			var repository = sp.GetRequiredService<ILocationRepository>();
			var logger = sp.GetRequiredService<ILogger<LocationTrackingService>>();
			var configuration = sp.GetRequiredService<AppConfiguration>();
			return new LocationTrackingService(directionsService, repository, logger, configuration.LocationUpdateIntervalMs);
		});
		
		// Register IHeatMapService with transient lifetime
		builder.Services.AddTransient<IHeatMapService>(sp =>
		{
			var repository = sp.GetRequiredService<ILocationRepository>();
			var crowdSimulator = sp.GetRequiredService<ICrowdSimulator>();
			var logger = sp.GetRequiredService<ILogger<HeatMapService>>();
			return new HeatMapService(repository, crowdSimulator, logger);
		});
		
		// Register ICrowdSimulator with singleton lifetime
		builder.Services.AddSingleton<ICrowdSimulator, CrowdSimulator>();
		
		// Register MainViewModel
		builder.Services.AddTransient<MainViewModel>();
		
		// Register MainPage
		builder.Services.AddTransient<MainPage>();
		
		// Register App
		builder.Services.AddSingleton<App>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
