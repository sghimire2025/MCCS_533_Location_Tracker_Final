using Microsoft.Extensions.DependencyInjection;
using LocationTrackerFinal.Data;
using Microsoft.Extensions.Logging;

namespace LocationTrackerFinal;

public partial class App : Application
{
	public App(IServiceProvider serviceProvider)
	{
		InitializeComponent();
		
		// Initialize database on app startup
		InitializeDatabaseAsync(serviceProvider).ConfigureAwait(false);
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}

	private async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
	{
		try
		{
			var repository = serviceProvider.GetRequiredService<ILocationRepository>();
			await repository.InitializeDatabaseAsync();
		}
		catch (Exception ex)
		{
			// Log the error but don't crash the app
			var logger = serviceProvider.GetService<ILogger<App>>();
			logger?.LogError(ex, "Failed to initialize database on app startup");
			
			// Optionally show a user-friendly message
			// This could be done through a messaging service or alert
			System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
		}
	}
}