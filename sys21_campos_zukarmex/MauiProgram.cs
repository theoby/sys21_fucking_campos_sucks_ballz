using Microsoft.Extensions.Logging;
using sys21_campos_zukarmex.Services;
using sys21_campos_zukarmex.ViewModels;
using sys21_campos_zukarmex.Views;
using CommunityToolkit.Maui;

namespace sys21_campos_zukarmex;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register all sys21_campos_zukarmex services and repositories
        builder.Services.Addsys21_campos_zukarmexServices();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // Initialize services on app startup
        Task.Run(async () =>
        {
            try
            {
                await app.Services.InitializeServicesAsync();
            }
            catch (Exception ex)
            {
                // Log initialization error
                var logger = app.Services.GetService<ILogger<App>>();
                logger?.LogError(ex, "Error initializing services");
            }
        });

        return app;
    }
}
