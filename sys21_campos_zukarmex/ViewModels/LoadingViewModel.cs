using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Services;

namespace sys21_campos_zukarmex.ViewModels;

public partial class LoadingViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;
    private readonly IConfiguracionService _configuracionService;
    
    public LoadingViewModel(DatabaseService databaseService, IConfiguracionService configuracionService)
    {
        _databaseService = databaseService;
        _configuracionService = configuracionService;
        Title = "Cargando...";
    }

    [ObservableProperty]
    private string loadingMessage = "Inicializando aplicacion...";

    [ObservableProperty]
    private double progress = 0.0;

    public override async Task InitializeAsync()
    {
        await InitializeAppAsync();
    }

    [RelayCommand]
    private async Task InitializeAppAsync()
    {
        try
        {
            LoadingMessage = "Inicializando base de datos...";
            Progress = 0.2;
            await Task.Delay(500); // Visual feedback
            
            await _databaseService.InitializeAsync();
            
            LoadingMessage = "Cargando configuracion de API...";
            Progress = 0.4;
            await Task.Delay(500);
            
            // Inicializar AppConfigService y cargar URL desde configuración
            if (!AppConfigService.IsInitialized)
            {
                AppConfigService.Initialize(_databaseService);
            }
            
            // Cargar URL desde el primer registro de configuración
            await AppConfigService.LoadUrlFromDatabaseAsync();
            
            LoadingMessage = "Verificando configuracion...";
            Progress = 0.6;
            await Task.Delay(500);
            
            // Verificar si existe configuración y si la URL está funcionando
            var existeConfig = await _configuracionService.ExisteConfiguracionAsync();
            if (existeConfig)
            {
                var rutaBase = await _configuracionService.GetRutaBaseAsync();
                LoadingMessage = $"URL API: {rutaBase}";
                System.Diagnostics.Debug.WriteLine($"Configuracion cargada - URL: {rutaBase}");
            }
            else
            {
                LoadingMessage = "Usando configuracion por defecto";
                System.Diagnostics.Debug.WriteLine($"No hay configuracion guardada, usando URL por defecto: {AppConfigService.ApiBaseUrl}");
            }
            
            Progress = 0.8;
            await Task.Delay(500);
            
            LoadingMessage = "Aplicacion lista";
            Progress = 1.0;
            await Task.Delay(500);
            
            // Navigate to login
            await Shell.Current.GoToAsync("//login");
        }
        catch (Exception ex)
        {
            LoadingMessage = $"Error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Error en LoadingViewModel: {ex}");
            await Shell.Current.DisplayAlert("Error", "Error al inicializar la aplicacion", "OK");
            
            // En caso de error, asegurar que al menos se use la configuración por defecto
            if (!AppConfigService.IsInitialized)
            {
                AppConfigService.Initialize(_databaseService);
                AppConfigService.ResetToDefaultUrl();
            }
            
            // Continuar al login incluso si hay errores
            await Shell.Current.GoToAsync("//login");
        }
    }
}