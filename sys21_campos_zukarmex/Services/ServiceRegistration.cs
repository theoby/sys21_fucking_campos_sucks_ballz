using sys21_campos_zukarmex.Services.Repositories;
using sys21_campos_zukarmex.Services.Api;
using sys21_campos_zukarmex.ViewModels;
using sys21_campos_zukarmex.Views;

namespace sys21_campos_zukarmex.Services;

public static class ServiceRegistration
{
    public static IServiceCollection Addsys21_campos_zukarmexServices(this IServiceCollection services)
    {
        // Core services
        services.AddSingleton<DatabaseService>();
        services.AddSingleton<SyncService>();
        services.AddSingleton<CatalogService>();
        services.AddSingleton<SessionService>();
        services.AddSingleton<ValeNavigationService>();
        services.AddSingleton<ConnectivityService>();
        services.AddSingleton<NavigationService>(); // Agregar NavigationService
        services.AddSingleton<IConfiguracionService, ConfiguracionService>();

        // HTTP Client Infrastructure - configurar primero
        services.AddHttpClient();
        services.AddSingleton<IDynamicHttpClientFactory, DynamicHttpClientFactory>();

        // Main API service - usar factory para resolver URLs dinámicas
        services.AddSingleton<ApiService>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IDynamicHttpClientFactory>();
            var sessionService = provider.GetRequiredService<SessionService>();
            var databaseService = provider.GetRequiredService<DatabaseService>();
            var connectivityService = provider.GetRequiredService<ConnectivityService>();
            return new ApiService(httpClientFactory, sessionService, databaseService, connectivityService);
        });

        // Repositories
        services.AddSingleton<IAlmacenRepository, AlmacenRepository>();
        services.AddSingleton<IArticuloRepository, ArticuloRepository>();
        services.AddSingleton<ICampoRepository, CampoRepository>();
        services.AddSingleton<IConfiguracionRepository, ConfiguracionRepository>();
        services.AddSingleton<IEmpresaRepository, EmpresaRepository>();
        services.AddSingleton<IFamiliaRepository, FamiliaRepository>();
        services.AddSingleton<IInspectorRepository, InspectorRepository>();
        services.AddSingleton<ILoteRepository, LoteRepository>();
        services.AddSingleton<IMaquinariaRepository, MaquinariaRepository>();
        services.AddSingleton<IRecetaRepository, RecetaRepository>();
        services.AddSingleton<ISubFamiliaRepository, SubFamiliaRepository>();
        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<ISessionRepository, SessionRepository>();
        services.AddSingleton<ISalidaRepository, SalidaRepository>();
        services.AddSingleton<ISalidaDetalleRepository, SalidaDetalleRepository>();
        services.AddSingleton<IZafraRepository, ZafraRepository>();
        services.AddSingleton<IPluviometroRepository, PluviometroRepository>();
        services.AddSingleton<ICicloRepository, CicloRepository>();


        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<AdminConfigViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<ValeViewModel>();
        services.AddTransient<AgregarArticuloViewModel>();
        services.AddTransient<StatusViewModel>();
        services.AddTransient<AuthorizationViewModel>();
        services.AddTransient<SyncViewModel>();
        services.AddTransient<LoadingViewModel>();
        services.AddTransient<CatalogExampleViewModel>();
        services.AddTransient<HistorialViewModel>();
        services.AddTransient<RatTrappingViewModel>();
        services.AddTransient<DamageAssessmentViewModel>();
        services.AddTransient<RodenticideConsumptionViewModel>();


        // Pages
        services.AddTransient<LoginPage>();
        services.AddTransient<AdminConfigPage>();
        services.AddTransient<HomePage>();
        services.AddTransient<ValePage>();
        services.AddTransient<AgregarArticuloPage>();
        services.AddTransient<StatusPage>();
        services.AddTransient<AuthorizationPage>();
        services.AddTransient<SyncPage>();
        services.AddTransient<LoadingPage>();
        services.AddTransient<HistorialPage>();
        services.AddTransient<RatTrappingPage>();
        services.AddTransient<DamageAssessmentPage>();
        services.AddTransient<RodenticideConsumptionPage>();

        return services;
    }
}

/// <summary>
/// Extension methods for easier service usage
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Initialize all necessary services including AppConfigService
    /// </summary>
    public static async Task InitializeServicesAsync(this IServiceProvider serviceProvider)
    {
        try
        {
            // Initialize database first
            var databaseService = serviceProvider.GetRequiredService<DatabaseService>();
            await databaseService.InitializeAsync();
            
            // Initialize AppConfigService with DatabaseService
            AppConfigService.Initialize(databaseService);
            
            // Load URL from database configuration
            await AppConfigService.LoadUrlFromDatabaseAsync();
            
            System.Diagnostics.Debug.WriteLine($"Servicios inicializados. API URL: {AppConfigService.ApiBaseUrl}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error inicializando servicios: {ex.Message}");
            
            // Ensure AppConfigService is at least initialized with default values
            if (!AppConfigService.IsInitialized)
            {
                var databaseService = serviceProvider.GetService<DatabaseService>();
                if (databaseService != null)
                {
                    AppConfigService.Initialize(databaseService);
                }
                AppConfigService.ResetToDefaultUrl();
            }
        }
    }

    /// <summary>
    /// Initialize services and ensure empresas are available in database
    /// </summary>
    public static async Task InitializeServicesWithEmpresaValidationAsync(this IServiceProvider serviceProvider)
    {
        try
        {
            // Perform standard initialization first
            await serviceProvider.InitializeServicesAsync();
            
            // Get API service for empresa validation
            var apiService = serviceProvider.GetRequiredService<ApiService>();
            
            // Validate and ensure empresas are in database
            var empresaValidation = await AppConfigService.ValidateAndEnsureEmpresasAsync(apiService);
            
            if (empresaValidation.IsValid)
            {
                if (empresaValidation.WasSyncForced)
                {
                    System.Diagnostics.Debug.WriteLine($"? Empresas sincronizadas automaticamente: {empresaValidation.EmpresasCount} empresas");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"? Empresas ya disponibles en BD: {empresaValidation.EmpresasCount} empresas");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"?? Problema con empresas: {empresaValidation.Message}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en inicializacion con validacion de empresas: {ex.Message}");
            // Continue with standard initialization even if empresa validation fails
            await serviceProvider.InitializeServicesAsync();
        }
    }

    /// <summary>
    /// Validate empresas in database and force sync if needed
    /// </summary>
    public static async Task<EmpresaValidationResult> ValidateEmpresasAsync(this IServiceProvider serviceProvider)
    {
        try
        {
            var apiService = serviceProvider.GetRequiredService<ApiService>();
            return await AppConfigService.ValidateAndEnsureEmpresasAsync(apiService);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error validando empresas: {ex.Message}");
            return new EmpresaValidationResult
            {
                IsValid = false,
                Message = $"Error en validacion: {ex.Message}",
                RequiresSync = false,
                EmpresasCount = 0
            };
        }
    }

    /// <summary>
    /// Force sync empresas from API
    /// </summary>
    public static async Task<ForceSyncResult> ForceSyncEmpresasAsync(this IServiceProvider serviceProvider)
    {
        try
        {
            var apiService = serviceProvider.GetRequiredService<ApiService>();
            return await AppConfigService.ForceEmpresasSyncAsync(apiService);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en sincronizacion forzada: {ex.Message}");
            return new ForceSyncResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                EmpresasCount = 0
            };
        }
    }

    /// <summary>
    /// Get empresa statistics from database
    /// </summary>
    public static async Task<EmpresaStats> GetEmpresaStatsAsync(this IServiceProvider serviceProvider)
    {
        return await AppConfigService.GetEmpresaStatsAsync();
    }

    /// <summary>
    /// Get repository for specific entity type
    /// </summary>
    public static T GetRepository<T>(this IServiceProvider serviceProvider) where T : class
    {
        return serviceProvider.GetRequiredService<T>();
    }
}