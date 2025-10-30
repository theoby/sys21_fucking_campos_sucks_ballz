using System.Net.Http.Headers;
using sys21_campos_zukarmex.Models;

namespace sys21_campos_zukarmex.Services.Api;

/// <summary>
/// Factory para crear HttpClient con BaseAddress din�mico basado en la configuraci�n de BD
/// </summary>
public interface IDynamicHttpClientFactory
{
    Task<HttpClient> CreateHttpClientAsync();
    Task<HttpClient> CreateHttpClientWithConfigAsync();
    void InvalidateCache();
}

public class DynamicHttpClientFactory : IDynamicHttpClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DatabaseService _databaseService;
    private HttpClient? _cachedClient;
    private string? _cachedBaseUrl;
    private readonly object _lock = new object();

    public DynamicHttpClientFactory(IHttpClientFactory httpClientFactory, DatabaseService databaseService)
    {
        _httpClientFactory = httpClientFactory;
        _databaseService = databaseService;
    }

    /// <summary>
    /// Crea un HttpClient b�sico sin configurar BaseAddress (para casos especiales)
    /// </summary>
    public async Task<HttpClient> CreateHttpClientAsync()
    {
        var client = _httpClientFactory.CreateClient();
        ConfigureDefaultHeaders(client);
        return client;
    }

    /// <summary>
    /// Crea un HttpClient con BaseAddress configurado desde la BD
    /// Usa cach� para evitar consultas innecesarias a la BD
    /// </summary>
    public async Task<HttpClient> CreateHttpClientWithConfigAsync()
    {
        var currentBaseUrl = await GetCurrentBaseUrlAsync();
        
        lock (_lock)
        {
            // Verificar si ya tenemos un cliente cachado con la URL correcta
            if (_cachedClient != null && _cachedBaseUrl == currentBaseUrl)
            {
                System.Diagnostics.Debug.WriteLine($"[DynamicHttpClientFactory] Usando cliente cacheado con URL: {currentBaseUrl}");
                return _cachedClient;
            }

            // Crear nuevo cliente con la URL actualizada
            System.Diagnostics.Debug.WriteLine($"[DynamicHttpClientFactory] Creando nuevo cliente con URL: {currentBaseUrl}");
            
            // Disponer del cliente anterior si existe
            _cachedClient?.Dispose();
            
            // Crear nuevo cliente
            _cachedClient = _httpClientFactory.CreateClient();
            _cachedClient.BaseAddress = new Uri(currentBaseUrl);
            _cachedBaseUrl = currentBaseUrl;
            
            ConfigureDefaultHeaders(_cachedClient);
            
            System.Diagnostics.Debug.WriteLine($"[DynamicHttpClientFactory] Cliente creado exitosamente con BaseAddress: {_cachedClient.BaseAddress}");
            
            return _cachedClient;
        }
    }

    /// <summary>
    /// Invalida el cach� para forzar la recreaci�n del cliente con nueva configuraci�n
    /// </summary>
    public void InvalidateCache()
    {
        lock (_lock)
        {
            System.Diagnostics.Debug.WriteLine("[DynamicHttpClientFactory] Invalidando cach� de cliente HTTP");
            _cachedClient?.Dispose();
            _cachedClient = null;
            _cachedBaseUrl = null;
        }
    }

    /// <summary>
    /// Obtiene la URL base actual desde la BD o fallback
    /// </summary>
    private async Task<string> GetCurrentBaseUrlAsync()
    {
        try
        {
            // Intentar obtener desde la BD primero
            var configuraciones = await _databaseService.GetAllAsync<Configuracion>();
            var configuracionActiva = configuraciones?.OrderByDescending(c => c.Fecha).FirstOrDefault();

            if (configuracionActiva != null && !string.IsNullOrWhiteSpace(configuracionActiva.Ruta))
            {
                var rutaFormateada = configuracionActiva.Ruta.TrimEnd('/') + "/";
                System.Diagnostics.Debug.WriteLine($"[DynamicHttpClientFactory] URL desde BD: {rutaFormateada}");
                return rutaFormateada;
            }

            // Fallback a AppConfigService
            var fallbackUrl = AppConfigService.ApiBaseUrl;
            System.Diagnostics.Debug.WriteLine($"[DynamicHttpClientFactory] URL fallback: {fallbackUrl}");
            return fallbackUrl;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DynamicHttpClientFactory] Error obteniendo URL: {ex.Message}");
            return AppConfigService.FallbackUrl;
        }
    }

    /// <summary>
    /// Configura headers por defecto para el HttpClient
    /// </summary>
    private static void ConfigureDefaultHeaders(HttpClient client)
    {
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.Timeout = TimeSpan.FromSeconds(AppConfigService.SyncTimeoutSeconds);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _cachedClient?.Dispose();
            _cachedClient = null;
        }
    }
}