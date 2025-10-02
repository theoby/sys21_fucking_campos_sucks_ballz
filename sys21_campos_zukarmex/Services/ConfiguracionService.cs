using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services.Repositories;

namespace sys21_campos_zukarmex.Services;

public interface IConfiguracionService
{
    Task<Configuracion?> GetConfiguracionActivaAsync();
    Task<Configuracion?> GetPrimeraConfiguracionAsync();
    Task<List<Configuracion>> GetAllConfiguracionesAsync();
    Task<int> SaveConfiguracionAsync(Configuracion configuracion);
    Task<bool> ExisteConfiguracionAsync();
    Task<string> GetRutaBaseAsync();
    Task UpdateApiBaseUrlAsync(string nuevaRuta);
    Task InitializeFromStoredConfigAsync();
    Task RefreshUrlFromDatabaseAsync();
}

public class ConfiguracionService : IConfiguracionService
{
    private readonly DatabaseService _databaseService;
    private readonly ApiService _apiService;

    public ConfiguracionService(DatabaseService databaseService, ApiService apiService)
    {
        _databaseService = databaseService;
        _apiService = apiService;
    }

    public async Task<Configuracion?> GetConfiguracionActivaAsync()
    {
        var configuraciones = await _databaseService.GetAllAsync<Configuracion>();
        return configuraciones.OrderByDescending(c => c.Fecha).FirstOrDefault();
    }

    public async Task<Configuracion?> GetPrimeraConfiguracionAsync()
    {
        var configuraciones = await _databaseService.GetAllAsync<Configuracion>();
        return configuraciones.OrderBy(c => c.Id).FirstOrDefault();
    }

    public async Task<List<Configuracion>> GetAllConfiguracionesAsync()
    {
        return await _databaseService.GetAllAsync<Configuracion>();
    }

    public async Task<int> SaveConfiguracionAsync(Configuracion configuracion)
    {
        // Establecer fecha actual
        configuracion.Fecha = DateTime.Now;
        
        // IMPORTANTE: No sobrescribir el Dispositivo si ya viene establecido
        // El ViewModel ya debe pasar el valor correcto del campo de entrada
        if (string.IsNullOrWhiteSpace(configuracion.Dispositivo))
        {
            // Solo usar DeviceInfo.Name como fallback si no se proporciono ningun valor
            configuracion.Dispositivo = DeviceInfo.Name ?? "Dispositivo-Desconocido";
        }
        
        var result = await _databaseService.SaveAsync(configuracion);
        
        // Actualizar la URL base del API usando el AppConfigService mejorado
        if (result > 0 && !string.IsNullOrWhiteSpace(configuracion.Ruta))
        {
            await UpdateApiBaseUrlAsync(configuracion.Ruta);
            
            // Recargar la URL desde la base de datos para asegurar consistencia
            await RefreshUrlFromDatabaseAsync();
        }
        
        return result;
    }

    public async Task<bool> ExisteConfiguracionAsync()
    {
        var count = await _databaseService.CountAsync<Configuracion>();
        return count > 0;
    }

    public async Task<string> GetRutaBaseAsync()
    {
        try
        {
            // Primero intentar obtener desde la primera configuracion
            var primeraConfig = await GetPrimeraConfiguracionAsync();
            if (primeraConfig != null && !string.IsNullOrWhiteSpace(primeraConfig.Ruta))
            {
                return primeraConfig.Ruta;
            }
            
            // Si no hay primera configuracion, usar la mas reciente
            var config = await GetConfiguracionActivaAsync();
            if (config != null && !string.IsNullOrWhiteSpace(config.Ruta))
            {
                return config.Ruta;
            }
            
            // Si no hay configuraciones, usar la URL actual del AppConfigService
            return AppConfigService.ApiBaseUrl;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo ruta base: {ex.Message}");
            return AppConfigService.FallbackUrl;
        }
    }

    public async Task UpdateApiBaseUrlAsync(string nuevaRuta)
    {
        try
        {
            // Actualizar la configuracion estatica del AppConfigService
            AppConfigService.UpdateApiBaseUrl(nuevaRuta);
            
            // Forzar el refresh del HttpClient en ApiService
            _apiService.ForceRefreshBaseUrl();
            
            System.Diagnostics.Debug.WriteLine($"API Base URL actualizada a: {nuevaRuta}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error actualizando URL base: {ex.Message}");
        }
    }

    public async Task InitializeFromStoredConfigAsync()
    {
        try
        {
            // Cargar URL desde la base de datos usando AppConfigService
            await AppConfigService.LoadUrlFromDatabaseAsync();
            
            System.Diagnostics.Debug.WriteLine($"Configuracion inicializada desde BD. URL: {AppConfigService.ApiBaseUrl}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error inicializando configuracion guardada: {ex.Message}");
            
            // En caso de error, usar configuracion por defecto
            AppConfigService.ResetToDefaultUrl();
        }
    }

    public async Task RefreshUrlFromDatabaseAsync()
    {
        try
        {
            // Recargar la URL desde la base de datos
            await AppConfigService.LoadUrlFromDatabaseAsync();
            
            System.Diagnostics.Debug.WriteLine($"URL refrescada desde BD: {AppConfigService.ApiBaseUrl}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error refrescando URL desde BD: {ex.Message}");
        }
    }
}