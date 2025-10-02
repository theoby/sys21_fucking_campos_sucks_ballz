using System.Text;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using sys21_campos_zukarmex.Models.DTOs.Authentication;

namespace sys21_campos_zukarmex.Services.Api;

/// <summary>
/// Servicio base para operaciones de API
/// </summary>
public abstract class BaseApiService
{
    protected readonly HttpClient _httpClient;
    protected string? _authToken;
    private bool _isBaseAddressSet = false;

    protected BaseApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        InitializeHttpClient();
    }

    private void InitializeHttpClient()
    {
        if (!_isBaseAddressSet)
        {
            try
            {
                _httpClient.BaseAddress = new Uri(AppConfigService.ApiBaseUrl);
                _isBaseAddressSet = true;
                System.Diagnostics.Debug.WriteLine($"BaseApiService - BaseAddress establecida: {AppConfigService.ApiBaseUrl}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error estableciendo BaseAddress en BaseApiService: {ex.Message}");
                // Usar URL por defecto si hay error
                _httpClient.BaseAddress = new Uri("https://your-api-base-url.com/api/");
                _isBaseAddressSet = true;
            }
        }

        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.Timeout = TimeSpan.FromSeconds(AppConfigService.SyncTimeoutSeconds);
    }

    protected void UpdateBaseAddress()
    {
        // Solo actualizar si aún no se ha configurado
        if (!_isBaseAddressSet)
        {
            InitializeHttpClient();
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("BaseApiService - HttpClient ya configurado, no se puede cambiar BaseAddress");
        }
    }

    protected string GetFullUrl(string endpoint)
    {
        // Si el endpoint ya es una URL completa, usarla tal como está
        if (Uri.IsWellFormedUriString(endpoint, UriKind.Absolute))
        {
            return endpoint;
        }

        // Si no tenemos BaseAddress configurada, usar la URL base actual
        var baseUrl = _httpClient.BaseAddress?.ToString() ?? AppConfigService.ApiBaseUrl;
        
        // Asegurar que la baseUrl termine con /
        if (!baseUrl.EndsWith("/"))
        {
            baseUrl += "/";
        }

        // Asegurar que el endpoint no empiece con /
        if (endpoint.StartsWith("/"))
        {
            endpoint = endpoint.Substring(1);
        }

        return baseUrl + endpoint;
    }

    public void RefreshBaseUrl()
    {
        // Solo refrescar si aún no se ha usado el HttpClient
        if (!_isBaseAddressSet)
        {
            InitializeHttpClient();
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("BaseApiService - HttpClient ya configurado, no se puede cambiar BaseAddress");
        }
    }

    public void SetAuthToken(string token)
    {
        _authToken = token;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}