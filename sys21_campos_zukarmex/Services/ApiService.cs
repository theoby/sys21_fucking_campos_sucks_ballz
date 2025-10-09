using System.Text;
using Newtonsoft.Json;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Models.DTOs.Api;
using sys21_campos_zukarmex.Models.DTOs.Authentication;
using sys21_campos_zukarmex.Models.DTOs.Catalog;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using sys21_campos_zukarmex.Services.Api;

namespace sys21_campos_zukarmex.Services;

public class ApiService : IDisposable
{
    private readonly IDynamicHttpClientFactory _httpClientFactory;
    private readonly SessionService? _sessionService;
    private readonly DatabaseService? _databaseService;
    private readonly ConnectivityService? _connectivityService;
    private string? _authToken;
    private HttpClient? _currentHttpClient;

    // Constructor principal con DynamicHttpClientFactory
    public ApiService(
        IDynamicHttpClientFactory httpClientFactory,
        SessionService sessionService,
        DatabaseService databaseService,
        ConnectivityService connectivityService)
    {
        _httpClientFactory = httpClientFactory;
        _sessionService = sessionService;
        _databaseService = databaseService;
        _connectivityService = connectivityService;
    }

    // Constructor alternativo para compatibilidad hacia atras (sin DatabaseService)
    public ApiService(
        IDynamicHttpClientFactory httpClientFactory,
        SessionService sessionService,
        ConnectivityService connectivityService)
    {
        _httpClientFactory = httpClientFactory;
        _sessionService = sessionService;
        _databaseService = null;
        _connectivityService = connectivityService;
    }

    // Constructor para casos especiales (sin dependencias adicionales)
    public ApiService(IDynamicHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _sessionService = null;
        _databaseService = null;
        _connectivityService = null;
    }

    /// <summary>
    /// Obtiene un HttpClient configurado con la URL mas reciente de la BD
    /// </summary>
    private async Task<HttpClient> GetConfiguredHttpClientAsync()
    {
        // Si ya tenemos un cliente y no ha sido invalidado, reutilizarlo
        if (_currentHttpClient != null)
        {
            return _currentHttpClient;
        }

        // Crear nuevo cliente con configuracion actual
        _currentHttpClient = await _httpClientFactory.CreateHttpClientWithConfigAsync();
        
        // Aplicar token de autorizacion si esta disponible
        if (!string.IsNullOrEmpty(_authToken))
        {
            _currentHttpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _authToken);
        }

        return _currentHttpClient;
    }

    /// <summary>
    /// Invalida el HttpClient actual para forzar la recreacion con nueva configuracion
    /// Llamar despues de guardar una nueva configuracion
    /// </summary>
    public void ForceRefreshBaseUrl()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== FORZANDO REFRESH DE URL EN APISERVICE ===");
            
            // Invalidar cache del factory
            _httpClientFactory.InvalidateCache();
            
            // Limpiar cliente actual para forzar recreacion
            _currentHttpClient?.Dispose();
            _currentHttpClient = null;
            
            System.Diagnostics.Debug.WriteLine("Cache de HttpClient invalidado, proxima llamada usara nueva URL");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error en ForceRefreshBaseUrl: {ex.Message}");
        }
    }

    #region Session Validation

    /// <summary>
    /// Valida la respuesta HTTP y maneja el caso de sesion caducada (401)
    /// Excluye la API de empresas segun requerimientos
    /// </summary>
    private async Task<bool> ValidateHttpResponseAsync(HttpResponseMessage response, bool isEmpresaEndpoint = false)
    {
        // Solo validar si hay conexion a internet y no es el endpoint de empresas
        if (_connectivityService?.IsConnected != true || isEmpresaEndpoint)
        {
            return true; // No validar sesion si no hay conexion o es endpoint de empresas
        }

        // Verificar si la respuesta es 401 (Unauthorized)
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            System.Diagnostics.Debug.WriteLine("=== SESION CADUCADA DETECTADA (401) ===");
            
            // Verificar si el token actual es diferente al almacenado
            bool shouldLogout = true;
            
            if (_sessionService != null && !string.IsNullOrEmpty(_authToken))
            {
                var isTokenDifferent = await _sessionService.IsTokenDifferentFromStoredAsync(_authToken);
                
                if (!isTokenDifferent)
                {
                    System.Diagnostics.Debug.WriteLine("🔍 Token no ha cambiado - sesión realmente caducada");
                    shouldLogout = true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("🔍 Token diferente detectado - posible uso en otro dispositivo");
                    shouldLogout = true;
                }
            }
            
            if (shouldLogout)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        // Mostrar mensaje de sesion caducada
                        await Shell.Current.DisplayAlert(
                            "Sesion Caducada",
                            "La sesion ha caducado o el mismo usuario ha sido usado en otro dispositivo. Favor de iniciar sesion de nuevo",
                            "OK"
                        );

                        // Limpiar sesion local
                        if (_sessionService != null)
                        {
                            await _sessionService.ClearSessionAsync();
                            System.Diagnostics.Debug.WriteLine("Sesion local limpiada");
                        }

                        // Navegar al login
                        await Shell.Current.GoToAsync("//login");
                        System.Diagnostics.Debug.WriteLine("Navegacion a login completada");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error manejando sesion caducada: {ex.Message}");
                    }
                });
            }

            return false; // Indica que la validacion fallo
        }

        return true; // Respuesta valida
    }

    #endregion

    public void SetAuthToken(string token)
    {
        _authToken = token;
        
        // Aplicar el token al cliente actual si existe
        if (_currentHttpClient != null)
        {
            _currentHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    /// <summary>
    /// Agrega el token Bearer automaticamente desde la sesion activa para todas las APIs excepto Empresa y Login
    /// </summary>
    private async Task EnsureAuthTokenAsync(bool isEmpresaEndpoint = false)
    {
        // No agregar token para el endpoint de Empresa
        if (isEmpresaEndpoint)
        {
            return;
        }

        // Si ya tenemos un token configurado, usarlo
        if (!string.IsNullOrEmpty(_authToken))
        {
            return;
        }

        // Si no tenemos SessionService disponible, no podemos obtener token automatico
        if (_sessionService == null)
        {
            System.Diagnostics.Debug.WriteLine("SessionService no disponible para token automatico");
            return;
        }

        // Obtener token de la sesion activa
        try
        {
            var currentSession = await _sessionService.GetCurrentSessionAsync();
            if (currentSession != null && !string.IsNullOrEmpty(currentSession.Token))
            {
                SetAuthToken(currentSession.Token);
                System.Diagnostics.Debug.WriteLine($"Token Bearer agregado automaticamente desde sesion: {currentSession.Token.Substring(0, Math.Min(10, currentSession.Token.Length))}...");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo token de sesion: {ex.Message}");
        }
    }

    private string GetFullUrl(string endpoint)
    {
        // Si el endpoint ya es una URL completa, usarla tal como esta
        if (Uri.IsWellFormedUriString(endpoint, UriKind.Absolute))
        {
            return endpoint;
        }

        // Para endpoints relativos, necesitamos obtener la URL base actual
        // Este metodo se llama desde metodos que ya tienen acceso al HttpClient configurado
        // por lo que podemos usar la BaseAddress del cliente actual
        if (_currentHttpClient?.BaseAddress != null)
        {
            var baseUrl = _currentHttpClient.BaseAddress.ToString();
            
            // Asegurar que el endpoint no empiece con /
            if (endpoint.StartsWith("/"))
            {
                endpoint = endpoint.Substring(1);
            }

            return baseUrl + endpoint;
        }

        // Fallback: usar AppConfigService si no tenemos cliente configurado
        var fallbackBaseUrl = AppConfigService.ApiBaseUrl;
        if (!fallbackBaseUrl.EndsWith("/"))
        {
            fallbackBaseUrl += "/";
        }

        if (endpoint.StartsWith("/"))
        {
            endpoint = endpoint.Substring(1);
        }

        return fallbackBaseUrl + endpoint;
    }

    #region Authentication

    public async Task<LoginResponse> LoginAsync(LoginRequest request
    )
    {
        try
        {
            // Obtener HttpClient con configuración más reciente
            var httpClient = await GetConfiguredHttpClientAsync();
            // Login no requiere token Bearer
            var fullUrl = GetFullUrl(AppConfigService.LoginEndpoint);
            
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync(fullUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new LoginResponse { Success = false, Message = $"Error {response.StatusCode}: {responseContent}" };
            }

            // Limpiar el contenido de respuesta antes de deserializar
            var cleanedResponseContent = CleanJsonResponse(responseContent);

            var apiResponse = JsonConvert.DeserializeObject<LoginApiResponse>(cleanedResponseContent);

            if (apiResponse?.Estado == 200 && apiResponse.Datos != null)
            {
                // TRADUCCION: Mapea los datos de la estructura nueva a la estructura vieja
                var session = apiResponse.Datos.Usuario?.ToSession(apiResponse.Datos.Token, apiResponse.Datos.ExpirationDate);

                return new LoginResponse
                {
                    Success = true,
                    Message = apiResponse.Mensaje ?? "Login exitoso",
                    Token = apiResponse.Datos.Token,
                    Session = session
                };
            }
            else
            {
                // Si el JSON es invalido o no tiene los datos esperados, crea una respuesta de fallo
                return new LoginResponse { Success = false, Message = apiResponse?.Mensaje ?? "La respuesta del servidor no es valida." };
            }
        }
        catch (Exception ex)
        {
            return new LoginResponse { Success = false, Message = $"Error de conexion: {ex.Message}" };
        }
    }

    #endregion

    #region Generic CRUD Operations with StandardApiResponse

    private async Task<List<T>> GetCatalogAsync<T>(string endpoint, bool isEmpresaEndpoint = false)
    {
        try
        {
            // Obtener HttpClient con configuracion mas reciente
            var httpClient = await GetConfiguredHttpClientAsync();
            
            // Agregar token Bearer automaticamente excepto para Empresa
            await EnsureAuthTokenAsync(isEmpresaEndpoint);

            var fullUrl = GetFullUrl(endpoint);
            var response = await httpClient.GetAsync(fullUrl);
            
            // Validar respuesta para detectar sesion caducada (401)
            if (!await ValidateHttpResponseAsync(response, isEmpresaEndpoint))
            {
                return new List<T>(); // Sesion caducada, devolver lista vacia
            }
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // Limpiar el contenido antes de deserializar
                var cleanedContent = CleanJsonResponse(content);
                
                // Intentar deserializar con la nueva estructura StandardApiResponse
                try
                {
                    var standardResponse = JsonConvert.DeserializeObject<StandardApiResponse<T>>(cleanedContent);
                    if (standardResponse != null && standardResponse.Success)
                    {
                        System.Diagnostics.Debug.WriteLine($"API Response - Estado: {standardResponse.Estado}, TotalDatos: {standardResponse.TotalDatos}, Mensaje: {standardResponse.Mensaje}");
                        return standardResponse.Datos ?? new List<T>();
                    }
                    else if (standardResponse != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"API Error - Estado: {standardResponse.Estado}, Mensaje: {standardResponse.Mensaje}");
                        return new List<T>();
                    }
                }
                catch (JsonException)
                {
                    // Si falla la deserializacion de StandardApiResponse, intentar con ApiResponse legacy
                    System.Diagnostics.Debug.WriteLine("Intentando deserializar con estructura legacy...");
                    var legacyResponse = JsonConvert.DeserializeObject<ApiResponse<T>>(cleanedContent);
                    return legacyResponse?.DataList ?? new List<T>();
                }
            }
            return new List<T>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en GetCatalogAsync: {ex.Message}");
            return new List<T>();
        }
    }

    private async Task<T?> GetByIdAsync<T>(string endpoint, int id, bool isEmpresaEndpoint = false) where T : class
    {
        try
        {
            // Obtener HttpClient con configuracion mas reciente
            var httpClient = await GetConfiguredHttpClientAsync();
            
            // Agregar token Bearer automaticamente excepto para Empresa
            await EnsureAuthTokenAsync(isEmpresaEndpoint);

            var fullUrl = GetFullUrl($"{endpoint}/{id}");
            var response = await httpClient.GetAsync(fullUrl);
            
            // Validar respuesta para detectar sesion caducada (401)
            if (!await ValidateHttpResponseAsync(response, isEmpresaEndpoint))
            {
                return null; // Sesion caducada
            }
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // Intentar deserializar con la nueva estructura StandardApiResponse
                try
                {
                    var standardResponse = JsonConvert.DeserializeObject<StandardApiResponse<T>>(content);
                    if (standardResponse != null && standardResponse.Success)
                    {
                        return standardResponse.FirstData;
                    }
                }
                catch (JsonException)
                {
                    // Si falla, intentar con ApiResponse legacy
                    var legacyResponse = JsonConvert.DeserializeObject<ApiResponse<T>>(content);
                    return legacyResponse?.Data;
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en GetByIdAsync: {ex.Message}");
            return null;
        }
    }

    private async Task<ApiResponse<T>> CreateAsync<T>(string endpoint, T item, bool isEmpresaEndpoint = false) where T : class
    {
        try
        {
            // Obtener HttpClient con configuracion mas reciente
            var httpClient = await GetConfiguredHttpClientAsync();
            
            // Agregar token Bearer automaticamente excepto para Empresa
            await EnsureAuthTokenAsync(isEmpresaEndpoint);

            var fullUrl = GetFullUrl(endpoint);
            var json = JsonConvert.SerializeObject(item);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync(fullUrl, content);
            
            // Validar respuesta para detectar sesion caducada (401)
            if (!await ValidateHttpResponseAsync(response, isEmpresaEndpoint))
            {
                return new ApiResponse<T> { Success = false, Message = "Sesion caducada" };
            }
            
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Intentar deserializar con StandardApiResponse primero
            try
            {
                var standardResponse = JsonConvert.DeserializeObject<StandardApiResponse<T>>(responseContent);
                if (standardResponse != null)
                {
                    return new ApiResponse<T>
                    {
                        Success = standardResponse.Success,
                        Message = standardResponse.Mensaje,
                        Data = standardResponse.FirstData
                    };
                }
            }
            catch (JsonException)
            {
                // Si falla, usar deserializacion legacy
                return JsonConvert.DeserializeObject<ApiResponse<T>>(responseContent) ?? 
                       new ApiResponse<T> { Success = false, Message = "Error al crear registro" };
            }
            
            return new ApiResponse<T> { Success = false, Message = "Error al crear registro" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<T> { Success = false, Message = ex.Message };
        }
    }

    private async Task<ApiResponse<T>> UpdateAsync<T>(string endpoint, int id, T item, bool isEmpresaEndpoint = false) where T : class
    {
        try
        {
            // Obtener HttpClient con configuracion mas reciente
            var httpClient = await GetConfiguredHttpClientAsync();
            
            // Agregar token Bearer automaticamente excepto para Empresa
            await EnsureAuthTokenAsync(isEmpresaEndpoint);

            var fullUrl = GetFullUrl($"{endpoint}/{id}");
            var json = JsonConvert.SerializeObject(item);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await httpClient.PutAsync(fullUrl, content);
            
            // Validar respuesta para detectar sesion caducada (401)
            if (!await ValidateHttpResponseAsync(response, isEmpresaEndpoint))
            {
                return new ApiResponse<T> { Success = false, Message = "Sesion caducada" };
            }
            
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Intentar deserializar con StandardApiResponse primero
            try
            {
                var standardResponse = JsonConvert.DeserializeObject<StandardApiResponse<T>>(responseContent);
                if (standardResponse != null)
                {
                    return new ApiResponse<T>
                    {
                        Success = standardResponse.Success,
                        Message = standardResponse.Mensaje,
                        Data = standardResponse.FirstData
                    };
                }
            }
            catch (JsonException)
            {
                // Si falla, usar deserializacion legacy
                return JsonConvert.DeserializeObject<ApiResponse<T>>(responseContent) ?? 
                       new ApiResponse<T> { Success = false, Message = "Error al actualizar registro" };
            }
            
            return new ApiResponse<T> { Success = false, Message = "Error al actualizar registro" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<T> { Success = false, Message = ex.Message };
        }
    }

    private async Task<ApiResponse<bool>> DeleteAsync(string endpoint, int id, bool isEmpresaEndpoint = false)
    {
        try
        {
            // Obtener HttpClient con configuracion mas reciente
            var httpClient = await GetConfiguredHttpClientAsync();
            
            // Agregar token Bearer automaticamente excepto para Empresa
            await EnsureAuthTokenAsync(isEmpresaEndpoint);

            var fullUrl = GetFullUrl($"{endpoint}/{id}");
            var response = await httpClient.DeleteAsync(fullUrl);
            
            // Validar respuesta para detectar sesion caducada (401)
            if (!await ValidateHttpResponseAsync(response, isEmpresaEndpoint))
            {
                return new ApiResponse<bool> { Success = false, Message = "Sesion caducada" };
            }
            
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Intentar deserializar con StandardApiResponse primero
            try
            {
                var standardResponse = JsonConvert.DeserializeObject<StandardApiResponse<bool>>(responseContent);
                if (standardResponse != null)
                {
                    return new ApiResponse<bool>
                    {
                        Success = standardResponse.Success,
                        Message = standardResponse.Mensaje,
                        Data = standardResponse.Success
                    };
                }
            }
            catch (JsonException)
            {
                // Si falla, usar deserializacion legacy
                return JsonConvert.DeserializeObject<ApiResponse<bool>>(responseContent) ?? 
                       new ApiResponse<bool> { Success = false, Message = "Error al eliminar registro" };
            }
            
            return new ApiResponse<bool> { Success = false, Message = "Error al eliminar registro" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool> { Success = false, Message = ex.Message };
        }
    }

    #endregion

    #region Empresa Operations (SIN TOKEN BEARER)

    /// <summary>
    /// Obtiene las empresas desde la API usando el mapeo especifico para idEmpresa, nombre y esPromotora
    /// EXCEPCION: No usa token Bearer segun requerimientos
    /// </summary>
    public async Task<List<Empresa>> GetEmpresasAsync()
    {
        try
        {
            // Obtener HttpClient con configuracion mas reciente
            var httpClient = await GetConfiguredHttpClientAsync();
            
            // NO AGREGAR TOKEN BEARER para Empresa (segun requerimientos)
            var fullUrl = GetFullUrl(AppConfigService.EmpresasEndpoint);
            var response = await httpClient.GetAsync(fullUrl);
            
            // NO validar sesion para endpoint de empresas
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Raw API Response - Empresas (SIN TOKEN): {content}");
                
                // Intentar deserializar con la nueva estructura StandardApiResponse<EmpresaApiDto>
                try
                {
                    var standardResponse = JsonConvert.DeserializeObject<StandardApiResponse<EmpresaApiDto>>(content);
                    if (standardResponse != null && standardResponse.Success)
                    {
                        System.Diagnostics.Debug.WriteLine($"Empresas API Response - Estado: {standardResponse.Estado}, TotalDatos: {standardResponse.TotalDatos}, Mensaje: {standardResponse.Mensaje}");
                        
                        // Convertir DTOs a modelos de dominio
                        var empresas = standardResponse.Datos.Select(dto => dto.ToEmpresa()).ToList();
                        
                        System.Diagnostics.Debug.WriteLine($"Empresas convertidas: {empresas.Count}");
                        foreach (var empresa in empresas)
                        {
                            System.Diagnostics.Debug.WriteLine($"  - ID: {empresa.Id}, Nombre: {empresa.Nombre}, IsPromotora: {empresa.IsPromotora}");
                        }
                        
                        return empresas;
                    }
                    else if (standardResponse != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Empresas API Error - Estado: {standardResponse.Estado}, Mensaje: {standardResponse.Mensaje}");
                        return new List<Empresa>();
                    }
                }
                catch (JsonException ex)
                {
                    // Si falla la deserializacion de StandardApiResponse, intentar con ApiResponse legacy
                    System.Diagnostics.Debug.WriteLine($"Error deserializando StandardApiResponse, intentando legacy: {ex.Message}");
                    
                    var legacyResponse = JsonConvert.DeserializeObject<ApiResponse<Empresa>>(content);
                    if (legacyResponse?.DataList != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Empresas desde estructura legacy: {legacyResponse.DataList.Count}");
                        return legacyResponse.DataList;
                    }
                    
                    // Intentar deserializacion directa como lista de empresas
                    var directList = JsonConvert.DeserializeObject<List<Empresa>>(content);
                    if (directList != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Empresas desde lista directa: {directList.Count}");
                        return directList;
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"HTTP Error - StatusCode: {response.StatusCode}, ReasonPhrase: {response.ReasonPhrase}");
            }
            
            return new List<Empresa>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception en GetEmpresasAsync: {ex}");
            return new List<Empresa>();
        }
    }

    #endregion

    #region IrrigationLine Operations (CON TOKEN BEARER)
    public async Task<ApiResponse<SalidaLineaDeRiego>> SaveIrrigationEntryAsync(SalidaLineaDeRiego entry)
    {
        try
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            await EnsureAuthTokenAsync();

            // Creamos el DTO para la petición (esto ya estaba correcto)
            var apiRequest = new IrrigationEntryApiRequest
            {
                IdCampo = entry.IdCampo,
                IdLineaRiego = entry.IdLineaRiego,
                Fecha = entry.Fecha,
                EquiposBombeoOperando = entry.EquiposBombeoOperando,
                Observaciones = entry.Observaciones,
                 Lat = entry.Lat,
                Lng = entry.Lng
            };
            var requestBody = new List<IrrigationEntryApiRequest> { apiRequest };
            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var fullUrl = GetFullUrl(AppConfigService.IrrigationEntriesEndpoint);

            System.Diagnostics.Debug.WriteLine($"Enviando JSON de Línea de Riego a: {fullUrl}");
            System.Diagnostics.Debug.WriteLine($"JSON: {json}");

            var response = await httpClient.PostAsync(fullUrl, content);

            if (!await ValidateHttpResponseAsync(response))
            {
                return new ApiResponse<SalidaLineaDeRiego> { Success = false, Message = "Sesion caducada" };
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"Respuesta de la API (Línea de Riego): {responseContent}");

            var apiResponse = JsonConvert.DeserializeObject<IrrigationEntryApiResponse>(responseContent);

            if (apiResponse != null)
            {
                var finalResponse = new ApiResponse<SalidaLineaDeRiego>
                {
                    Success = apiResponse.Success,
                    Message = apiResponse.Mensaje,
                };

                if (finalResponse.Success && _databaseService != null && entry.Id > 0)
                {
                    await _databaseService.DeleteAsync(entry);
                }

                return finalResponse;
            }

            return new ApiResponse<SalidaLineaDeRiego> { Success = false, Message = "Respuesta inválida de la API." };
        }
        catch (Exception ex)
        {
            return new ApiResponse<SalidaLineaDeRiego> { Success = false, Message = ex.Message };
        }
    }
    #endregion

    #region Rodenticide Operations (CON TOKEN BEARER)
    public async Task<ApiResponse<SalidaRodenticida>> SaveRodenticideConsumptionAsync(SalidaRodenticida consumption)
    {
        try
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            await EnsureAuthTokenAsync();

            var apiRequest = new RodenticideApiRequest
            {
                IdTemporada = consumption.IdTemporada,
                IdCampo = consumption.IdCampo,
                Fecha = consumption.Fecha,
                CantidadComederos = consumption.CantidadComederos,
                CantidadPastillas = consumption.CantidadPastillas,
                CantidadConsumo = consumption.CantidadConsumos,
                Lat = consumption.Lat,
                Lng = consumption.Lng
            };

            var requestBody = new List<RodenticideApiRequest> { apiRequest };
            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var fullUrl = GetFullUrl(AppConfigService.RodenticideConsumptionEndpoint);

            var response = await httpClient.PostAsync(fullUrl, content);

            if (!await ValidateHttpResponseAsync(response))
            {
                return new ApiResponse<SalidaRodenticida> { Success = false, Message = "Sesion caducada" };
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<RodenticideApiResponse>(responseContent);

            if (apiResponse != null)
            {
                var finalResponse = new ApiResponse<SalidaRodenticida> { Success = apiResponse.Success, Message = apiResponse.Mensaje };
                if (finalResponse.Success && _databaseService != null && consumption.Id > 0)
                {
                    await _databaseService.DeleteAsync(consumption);
                }
                return finalResponse;
            }

            return new ApiResponse<SalidaRodenticida> { Success = false, Message = "Respuesta inválida de la API." };
        }
        catch (Exception ex)
        {
            return new ApiResponse<SalidaRodenticida> { Success = false, Message = ex.Message };
        }
    }
    #endregion

    #region DAMAGE Operations (CON TOKEN BEARER)
    public async Task<ApiResponse<SalidaMuestroDaños>> SaveDamageAssessmentAsync(SalidaMuestroDaños assessment)
    {
        try
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            await EnsureAuthTokenAsync();

            var apiRequest = new DamageApiRequest
            {
                IdTemporada = assessment.IdTemporada,
                IdCampo = assessment.IdCampo,
                IdCiclo = assessment.IdCiclo,
                Fecha = assessment.Fecha,
                NumeroTallos = assessment.NumeroTallos,
                DanoViejo = assessment.DañoViejo,
                DanoNuevo = assessment.DañoNuevo,
                Lat = assessment.Lat,
                Lng = assessment.Lng
            };

            var requestBody = new List<DamageApiRequest> { apiRequest };
            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var fullUrl = GetFullUrl(AppConfigService.DamageAssessmentEndpoint);

            System.Diagnostics.Debug.WriteLine($"Enviando JSON de Muestreo de Daño a: {fullUrl}");
            System.Diagnostics.Debug.WriteLine($"JSON: {json}");

            var response = await httpClient.PostAsync(fullUrl, content);

            if (!await ValidateHttpResponseAsync(response))
            {
                return new ApiResponse<SalidaMuestroDaños> { Success = false, Message = "Sesion caducada" };
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"Respuesta de la API (Muestreo Daño): {responseContent}");

            var apiResponse = JsonConvert.DeserializeObject<DamageAssessmentApiResponse>(responseContent);

            if (apiResponse != null)
            {
                var finalResponse = new ApiResponse<SalidaMuestroDaños> { Success = apiResponse.Success, Message = apiResponse.Mensaje };
                if (finalResponse.Success && _databaseService != null && assessment.Id > 0)
                {
                    await _databaseService.DeleteAsync(assessment);
                }
                return finalResponse;
            }

            return new ApiResponse<SalidaMuestroDaños> { Success = false, Message = "Respuesta inválida de la API." };
        }
        catch (Exception ex)
        {
            return new ApiResponse<SalidaMuestroDaños> { Success = false, Message = ex.Message };
        }
    }
    #endregion

    #region Other Catalog Operations (CON TOKEN BEARER)

    public async Task<List<Almacen>> GetAlmacenesAsync()
    {
        return await GetCatalogAsync<Almacen>(AppConfigService.AlmacenesEndpoint);
    }

    public async Task<List<Articulo>> GetArticulosAsync()
    {
        return await GetCatalogAsync<Articulo>(AppConfigService.ArticulosEndpoint);
    }

    public async Task<List<Campo>> GetCamposAsync()
    {
        return await GetCatalogAsync<Campo>(AppConfigService.CamposEndpoint);
    }

    public async Task<List<Familia>> GetFamiliasAsync()
    {
        return await GetCatalogAsync<Familia>(AppConfigService.FamiliasEndpoint);
    }

    public async Task<List<Inspector>> GetInspectoresAsync()
    {
        return await GetCatalogAsync<Inspector>(AppConfigService.InspectoresEndpoint);
    }

    public async Task<List<Maquinaria>> GetMaquinariasAsync()
    {
        return await GetCatalogAsync<Maquinaria>(AppConfigService.MaquinariasEndpoint);
    }

    public async Task<List<Lote>> GetLotesAsync()
    {
        return await GetCatalogAsync<Lote>(AppConfigService.LotesEndpoint);
    }

    public async Task<List<SubFamilia>> GetSubFamiliasAsync()
    {
        return await GetCatalogAsync<SubFamilia>(AppConfigService.SubFamiliasEndpoint);
    }

    public async Task<List<Zafra>> GetZafrasAsync()
    {
        return await GetCatalogAsync<Zafra>(AppConfigService.ZafrasEndpoint);
    }
    public async Task<List<Ciclo>> GetCiclosAsync()
    {
        return await GetCatalogAsync<Ciclo>(AppConfigService.CiclosEndpoint);
    }

    public async Task<List<Pluviometro>> GetPluviometrosAsync()
    {
        return await GetCatalogAsync<Pluviometro>(AppConfigService.PluviometrosEndpoint);
    }
    public async Task<List<Receta>> GetRecetasAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] === INICIANDO SINCRONIZACIÓN DE RECETAS ===");
            
            // Obtener HttpClient con configuración más reciente
            var httpClient = await GetConfiguredHttpClientAsync();
            await EnsureAuthTokenAsync();

            var fullUrl = GetFullUrl(AppConfigService.RecetasEndpoint);
            System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] URL completa: {fullUrl}");
            System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] Endpoint: {AppConfigService.RecetasEndpoint}");
            
            // Verificar token
            var authHeader = httpClient.DefaultRequestHeaders.Authorization;
            if (authHeader != null && !string.IsNullOrEmpty(authHeader.Parameter))
            {
                var tokenLength = authHeader.Parameter.Length;
                var previewLength = Math.Min(10, tokenLength);
                System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] Token presente: {authHeader.Parameter.Substring(0, previewLength)}...");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] WARNING: No hay token de autorización");
            }
            
            var response = await httpClient.GetAsync(fullUrl);
            System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] Status Code: {response.StatusCode}");
            
            // Validar respuesta para detectar sesion caducada (401)
            if (!await ValidateHttpResponseAsync(response))
            {
                System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] Sesión caducada - devolviendo lista vacía");
                return new List<Receta>(); // Sesion caducada
            }
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] Response Length: {content?.Length ?? 0} caracteres");
                
                if (!string.IsNullOrEmpty(content))
                {
                    var previewLength = Math.Min(500, content.Length);
                    System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] Raw JSON (primeros {previewLength} chars): {content.Substring(0, previewLength)}");
                }
                
                // Limpiar el contenido antes de deserializar
                var cleanedContent = CleanJsonResponse(content);
                System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] Cleaned JSON Length: {cleanedContent?.Length ?? 0} caracteres");
                
                // Intentar deserializar con la nueva estructura StandardApiResponse
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] Intentando deserializar StandardApiResponse<RecetaApiDto>...");
                    var standardResponse = JsonConvert.DeserializeObject<StandardApiResponse<RecetaApiDto>>(cleanedContent);
                    
                    if (standardResponse != null && standardResponse.Success)
                    {
                        System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] ✅ StandardApiResponse deserializada exitosamente");
                        System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] Estado: {standardResponse.Estado}, TotalDatos: {standardResponse.TotalDatos}");
                        System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] Recetas encontradas: {standardResponse.Datos?.Count ?? 0}");
                        
                        if (standardResponse.Datos != null && standardResponse.Datos.Any())
                        {
                            // Convertir DTOs a modelos de dominio y usar SyncRecetaWithArticulosAsync
                            var recetas = new List<Receta>();
                            var totalArticulos = 0;
                            
                            foreach (var recetaDto in standardResponse.Datos)
                            {
                                System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] Procesando receta: IdReceta={recetaDto.idReceta}, Nombre='{recetaDto.nombreReceta}', Articulos={recetaDto.articulos?.Count ?? 0}");
                                
                                var receta = recetaDto.ToReceta();
                                
                                // Crear articulos para esta receta
                                var articulos = recetaDto.articulos?.Select(a => a.ToRecetaArticulo(receta.IdReceta)).ToList() ?? new List<RecetaArticulo>();
                                totalArticulos += articulos.Count;
                                
                                // Si tenemos DatabaseService disponible, usar el metodo de sincronizacion
                                if (_databaseService != null)
                                {
                                    try
                                    {
                                        await _databaseService.SyncRecetaWithArticulosAsync(receta, articulos);
                                        System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] ✅ Receta sincronizada: {receta.NombreReceta} con {articulos.Count} articulos");
                                    }
                                    catch (Exception syncEx)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] ❌ Error sincronizando receta {receta.NombreReceta}: {syncEx.Message}");
                                        throw; // Re-throw para que se propague el error
                                    }
                                }
                                
                                recetas.Add(receta);
                            }
                            
                            System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] ✅ SINCRONIZACION COMPLETADA: {recetas.Count} recetas, {totalArticulos} articulos totales");
                            return recetas;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] ⚠️ StandardApiResponse exitosa pero sin datos de recetas");
                            return new List<Receta>();
                        }
                    }
                    else if (standardResponse != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] ❌ StandardApiResponse no exitosa - Estado: {standardResponse.Estado}, Mensaje: {standardResponse.Mensaje}");
                        return new List<Receta>();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] ❌ StandardApiResponse es null despues de deserializar");
                    }
                }
                catch (JsonException jsonEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] ❌ Error deserializando StandardApiResponse: {jsonEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] Intentando deserializar con estructura legacy...");
                    
                    // Si falla la deserializacion de StandardApiResponse, intentar con estructura legacy
                    try
                    {
                        var legacyResult = await GetCatalogAsync<Receta>(AppConfigService.RecetasEndpoint);
                        System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] ✅ Legacy deserialization: {legacyResult.Count} recetas");
                        return legacyResult;
                    }
                    catch (Exception legacyEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] ❌ Error en legacy deserialization: {legacyEx.Message}");
                        return new List<Receta>();
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] ❌ HTTP Error: {response.StatusCode} - {response.ReasonPhrase}");
                
                var errorContent = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(errorContent))
                {
                    System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] Error body: {errorContent}");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] Devolviendo lista vacía por falta de respuesta exitosa");
            return new List<Receta>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] ❌ EXCEPCIÓN: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[GetRecetasAsync] Stack trace: {ex.StackTrace}");
            return new List<Receta>();
        }
    }

    #endregion

    #region CRUD Operations (CON TOKEN BEARER)

    public async Task<Almacen?> GetAlmacenByIdAsync(int id)
    {
        return await GetByIdAsync<Almacen>(AppConfigService.AlmacenesEndpoint, id);
    }

    public async Task<ApiResponse<Almacen>> CreateAlmacenAsync(Almacen almacen)
    {
        return await CreateAsync(AppConfigService.AlmacenesEndpoint, almacen);
    }

    public async Task<ApiResponse<Almacen>> UpdateAlmacenAsync(int id, Almacen almacen)
    {
        return await UpdateAsync(AppConfigService.AlmacenesEndpoint, id, almacen);
    }

    public async Task<ApiResponse<bool>> DeleteAlmacenAsync(int id)
    {
        return await DeleteAsync(AppConfigService.AlmacenesEndpoint, id);
    }

    public async Task<Articulo?> GetArticuloByIdAsync(int id)
    {
        return await GetByIdAsync<Articulo>(AppConfigService.ArticulosEndpoint, id);
    }

    public async Task<ApiResponse<Articulo>> CreateArticuloAsync(Articulo articulo)
    {
        return await CreateAsync(AppConfigService.ArticulosEndpoint, articulo);
    }

    public async Task<ApiResponse<Articulo>> UpdateArticuloAsync(int id, Articulo articulo)
    {
        return await UpdateAsync(AppConfigService.ArticulosEndpoint, id, articulo);
    }

    public async Task<ApiResponse<bool>> DeleteArticuloAsync(int id)
    {
        return await DeleteAsync(AppConfigService.ArticulosEndpoint, id);
    }

    // CRUD para Empresa (SIN TOKEN BEARER)
    public async Task<Empresa?> GetEmpresaByIdAsync(int id)
    {
        return await GetByIdAsync<Empresa>(AppConfigService.EmpresasEndpoint, id, isEmpresaEndpoint: true);
    }

    public async Task<ApiResponse<Empresa>> CreateEmpresaAsync(Empresa empresa)
    {
        return await CreateAsync(AppConfigService.EmpresasEndpoint, empresa, isEmpresaEndpoint: true);
    }

    public async Task<ApiResponse<Empresa>> UpdateEmpresaAsync(int id, Empresa empresa)
    {
        return await UpdateAsync(AppConfigService.EmpresasEndpoint, id, empresa, isEmpresaEndpoint: true);
    }

    public async Task<ApiResponse<bool>> DeleteEmpresaAsync(int id)
    {
        return await DeleteAsync(AppConfigService.EmpresasEndpoint, id, isEmpresaEndpoint: true);
    }

    public async Task<Campo?> GetCampoByIdAsync(int id)
    {
        return await GetByIdAsync<Campo>(AppConfigService.CamposEndpoint, id);
    }

    public async Task<ApiResponse<Campo>> CreateCampoAsync(Campo campo)
    {
        return await CreateAsync(AppConfigService.CamposEndpoint, campo);
    }

    public async Task<ApiResponse<Campo>> UpdateCampoAsync(int id, Campo campo)
    {
        return await UpdateAsync(AppConfigService.CamposEndpoint, id, campo);
    }

    public async Task<ApiResponse<bool>> DeleteCampoAsync(int id)
    {
        return await DeleteAsync(AppConfigService.CamposEndpoint, id);
    }

    #endregion

    #region Familia CRUD Operations

    public async Task<Familia?> GetFamiliaByIdAsync(int id)
    {
        return await GetByIdAsync<Familia>(AppConfigService.FamiliasEndpoint, id);
    }

    public async Task<ApiResponse<Familia>> CreateFamiliaAsync(Familia familia)
    {
        return await CreateAsync(AppConfigService.FamiliasEndpoint, familia);
    }

    public async Task<ApiResponse<Familia>> UpdateFamiliaAsync(int id, Familia familia)
    {
        return await UpdateAsync(AppConfigService.FamiliasEndpoint, id, familia);
    }

    public async Task<ApiResponse<bool>> DeleteFamiliaAsync(int id)
    {
        return await DeleteAsync(AppConfigService.FamiliasEndpoint, id);
    }

    #endregion

    #region Inspector CRUD Operations

    public async Task<Inspector?> GetInspectorByIdAsync(int id)
    {
        return await GetByIdAsync<Inspector>(AppConfigService.InspectoresEndpoint, id);
    }

    public async Task<ApiResponse<Inspector>> CreateInspectorAsync(Inspector inspector)
    {
        return await CreateAsync(AppConfigService.InspectoresEndpoint, inspector);
    }

    public async Task<ApiResponse<Inspector>> UpdateInspectorAsync(int id, Inspector inspector)
    {
        return await UpdateAsync(AppConfigService.InspectoresEndpoint, id, inspector);
    }

    public async Task<ApiResponse<bool>> DeleteInspectorAsync(int id)
    {
        return await DeleteAsync(AppConfigService.InspectoresEndpoint, id);
    }

    #endregion

    #region Maquinaria CRUD Operations

    public async Task<Maquinaria?> GetMaquinariaByIdAsync(int id)
    {
        return await GetByIdAsync<Maquinaria>(AppConfigService.MaquinariasEndpoint, id);
    }

    public async Task<ApiResponse<Maquinaria>> CreateMaquinariaAsync(Maquinaria maquinaria)
    {
        return await CreateAsync(AppConfigService.MaquinariasEndpoint, maquinaria);
    }

    public async Task<ApiResponse<Maquinaria>> UpdateMaquinariaAsync(int id, Maquinaria maquinaria)
    {
        return await UpdateAsync(AppConfigService.MaquinariasEndpoint, id, maquinaria);
    }

    public async Task<ApiResponse<bool>> DeleteMaquinariaAsync(int id)
    {
        return await DeleteAsync(AppConfigService.MaquinariasEndpoint, id);
    }

    #endregion

    #region Lote CRUD Operations

    public async Task<Lote?> GetLoteByIdAsync(int id)
    {
        return await GetByIdAsync<Lote>(AppConfigService.LotesEndpoint, id);
    }

    public async Task<ApiResponse<Lote>> CreateLoteAsync(Lote lote)
    {
        return await CreateAsync(AppConfigService.LotesEndpoint, lote);
    }

    public async Task<ApiResponse<Lote>> UpdateLoteAsync(int id, Lote lote)
    {
        return await UpdateAsync(AppConfigService.LotesEndpoint, id, lote);
    }

    public async Task<ApiResponse<bool>> DeleteLoteAsync(int id)
    {
        return await DeleteAsync(AppConfigService.LotesEndpoint, id);
    }

    #endregion

    #region SubFamilia CRUD Operations

    public async Task<SubFamilia?> GetSubFamiliaByIdAsync(int id)
    {
        return await GetByIdAsync<SubFamilia>(AppConfigService.SubFamiliasEndpoint, id);
    }

    public async Task<ApiResponse<SubFamilia>> CreateSubFamiliaAsync(SubFamilia subFamilia)
    {
        return await CreateAsync(AppConfigService.SubFamiliasEndpoint, subFamilia);
    }

    public async Task<ApiResponse<SubFamilia>> UpdateSubFamiliaAsync(int id, SubFamilia subFamilia)
    {
        return await UpdateAsync(AppConfigService.SubFamiliasEndpoint, id, subFamilia);
    }

    public async Task<ApiResponse<bool>> DeleteSubFamiliaAsync(int id)
    {
        return await DeleteAsync(AppConfigService.SubFamiliasEndpoint, id);
    }

    #endregion

    #region Vale (Salida) Operations (CON TOKEN BEARER)

    /// <summary>
    /// Guarda un vale usando el formato legacy - mantener para compatibilidad
    /// </summary>
    public async Task<ApiResponse<Salida>> SaveValeAsync(Salida salida)
    {
        try
        {
            // Obtener HttpClient con configuracion mas reciente
            var httpClient = await GetConfiguredHttpClientAsync();
            
            // Agregar token Bearer automaticamente
            await EnsureAuthTokenAsync();

            // Convertir el modelo Salida al formato requerido por la API
            var valeApiRequest = new ValeApiRequest
            {
                Campo = salida.IdCampo,
                Almacen = salida.IdAlmacen,
                Fecha = salida.Fecha,
                Concepto = salida.Concepto ?? string.Empty,
                Id_Receta = salida.IdReceta, // usar IdReceta de la salida  
                Articulos = (salida.SalidaDetalle ?? new List<SalidaDetalle>()).Select(detalle => new ArticuloApiRequest
                {
                    Familia = detalle.IdFamilia,
                    SubFamilia = detalle.IdSubFamilia,
                    Articulo = detalle.IdArticulo,
                    Cantidad = detalle.Cantidad,
                    Concepto = detalle.Concepto ?? string.Empty,
                    CentroCosto = detalle.IdLote, // usar el IdCampo de la salida como centro de costo
                    IdMaquinaria = detalle.IdMaquinaria,
                    IdGrupo = detalle.IdGrupoMaquinaria // usar IdGrupoMaquinaria como idGrupo
                }).ToList()
            };

            var fullUrl = GetFullUrl(AppConfigService.ValesSalidaEndpoint);
            var json = JsonConvert.SerializeObject(valeApiRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            System.Diagnostics.Debug.WriteLine($"JSON enviado a la API: {json}");
            
            var response = await httpClient.PostAsync(fullUrl, content);
            
            // Validar respuesta para detectar sesion caducada (401)
            if (!await ValidateHttpResponseAsync(response))
            {
                return new ApiResponse<Salida> { Success = false, Message = "Sesion caducada" };
            }
            
            var responseContent = await response.Content.ReadAsStringAsync();
            
            System.Diagnostics.Debug.WriteLine($"Respuesta de la API (original): {responseContent}");
            
            // Limpiar el contenido de respuesta antes de deserializar
            var cleanedResponseContent = CleanJsonResponse(responseContent);
            
            // Intentar deserializar con ApiResponseVale primero (para vales donde datos es string)
            try
            {
                var valeResponse = JsonConvert.DeserializeObject<ApiResponseVale>(cleanedResponseContent);
                if (valeResponse != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Vale API Response - Estado: {valeResponse.Estado}, Datos: {valeResponse.Datos}, TotalDatos: {valeResponse.TotalDatos}, Mensaje: {valeResponse.Mensaje}");
                    
                    var apiResponse = new ApiResponse<Salida>
                    {
                        Success = valeResponse.Success,
                        Message = valeResponse.Mensaje,
                        Data = null // Para vales, normalmente no devuelven el objeto Salida completo
                    };

                    // Si el guardado en la API fue exitoso, eliminar el registro de la base de datos local
                    if (apiResponse.Success && _databaseService != null && salida.Id > 0)
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine($"=== ELIMINANDO VALE DE BD LOCAL TRAS EXITO EN API ===");
                            System.Diagnostics.Debug.WriteLine($"Vale ID a eliminar: {salida.Id}");
                            System.Diagnostics.Debug.WriteLine($"Datos devueltos por API: {valeResponse.Datos}");
                            
                            // Primero eliminar los detalles
                            var detallesEliminados = await _databaseService.DeleteDetallesBySalidaAsync(salida.Id);
                            System.Diagnostics.Debug.WriteLine($"Detalles eliminados: {detallesEliminados}");
                            
                            // Luego eliminar el vale principal
                            var valeEliminado = await _databaseService.DeleteAsync(salida);
                            System.Diagnostics.Debug.WriteLine($"Vale eliminado de BD local. Resultado: {valeEliminado}");
                            
                            System.Diagnostics.Debug.WriteLine($"=== VALE ELIMINADO EXITOSAMENTE DE BD LOCAL ===");
                        }
                        catch (Exception deleteEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error eliminando vale de BD local: {deleteEx.Message}");
                            // No fallar la operacion completa si hay error al eliminar de BD local
                            // El vale ya se guardo exitosamente en la API
                        }
                    }
                    
                    return apiResponse;
                }
            }
            catch (JsonException jsonEx)
            {
                System.Diagnostics.Debug.WriteLine($"Error deserializando ApiResponseVale: {jsonEx.Message}");
                
                // Si falla ApiResponseVale, intentar con StandardApiResponse<Salida>
                try
                {
                    var standardResponse = JsonConvert.DeserializeObject<StandardApiResponse<Salida>>(cleanedResponseContent);
                    if (standardResponse != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"StandardApiResponse fallback exitoso - Estado: {standardResponse.Estado}, Mensaje: {standardResponse.Mensaje}");
                        
                        var apiResponse = new ApiResponse<Salida>
                        {
                            Success = standardResponse.Success,
                            Message = standardResponse.Mensaje,
                            Data = standardResponse.FirstData
                        };

                        // Si el guardado en la API fue exitoso, eliminar el registro de la base de datos local
                        if (apiResponse.Success && _databaseService != null && salida.Id > 0)
                        {
                            try
                            {
                                System.Diagnostics.Debug.WriteLine($"=== ELIMINANDO VALE DE BD LOCAL TRAS EXITO EN API (StandardApiResponse) ===");
                                System.Diagnostics.Debug.WriteLine($"Vale ID a eliminar: {salida.Id}");
                                
                                // Primero eliminar los detalles
                                var detallesEliminados = await _databaseService.DeleteDetallesBySalidaAsync(salida.Id);
                                System.Diagnostics.Debug.WriteLine($"Detalles eliminados: {detallesEliminados}");
                                
                                // Luego eliminar el vale principal
                                var valeEliminado = await _databaseService.DeleteAsync(salida);
                                System.Diagnostics.Debug.WriteLine($"Vale eliminado de BD local. Resultado: {valeEliminado}");
                                
                                System.Diagnostics.Debug.WriteLine($"=== VALE ELIMINADO EXITOSAMENTE DE BD LOCAL ===");
                            }
                            catch (Exception deleteEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error eliminando vale de BD local: {deleteEx.Message}");
                                // No fallar la operacion completa si hay error al eliminar de BD local
                                // El vale ya se guardo exitosamente en la API
                            }
                        }
                        
                        return apiResponse;
                    }
                }
                catch (JsonException standardEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error deserializando StandardApiResponse: {standardEx.Message}");
                    
                    // Si falla todo, usar deserializacion legacy
                    try
                    {
                        return JsonConvert.DeserializeObject<ApiResponse<Salida>>(cleanedResponseContent) ?? 
                               new ApiResponse<Salida> { Success = false, Message = "Error al guardar registro" };
                    }
                    catch (JsonException legacyEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error deserializando ApiResponse legacy: {legacyEx.Message}");
                        return new ApiResponse<Salida> { Success = false, Message = $"Error deserializando respuesta: {legacyEx.Message}" };
                    }
                }
            }
            
            return new ApiResponse<Salida> { Success = false, Message = "Error al guardar vale - respuesta invalida" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<Salida> { Success = false, Message = ex.Message };
        }
    }

    public async Task<ApiResponse<SalidaTrampeoRatas>> SaveRatCaptureAsync(SalidaTrampeoRatas capture)
    {
        try
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            await EnsureAuthTokenAsync();

            var apiRequest = new RatCaptureApiRequest
            {
                IdTemporada = capture.IdTemporada,
                IdCampo = capture.IdCampo,
                Fecha = capture.Fecha,
                CantidadTrampas = capture.CantidadTrampas,
                CantidadMachos = capture.CantidadMachos,
                CantidadHembras = capture.CantidadHembras,
                Lat = capture.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Lng = capture.Lng.ToString(System.Globalization.CultureInfo.InvariantCulture)
            };

            var requestBody = new List<RatCaptureApiRequest> { apiRequest };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var fullUrl = GetFullUrl(AppConfigService.RatCapturesEndpoint);

            System.Diagnostics.Debug.WriteLine($"Enviando JSON de Trampeo de Ratas a: {fullUrl}");
            System.Diagnostics.Debug.WriteLine($"JSON: {json}");

            var response = await httpClient.PostAsync(fullUrl, content);

            if (!await ValidateHttpResponseAsync(response))
            {
                return new ApiResponse<SalidaTrampeoRatas> { Success = false, Message = "Sesion caducada" };
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"Respuesta de la API: {responseContent}");

            var apiResponse = JsonConvert.DeserializeObject<RatCaptureApiResponse>(responseContent);

            if (apiResponse != null)
            {
                var finalResponse = new ApiResponse<SalidaTrampeoRatas>
                {
                    Success = apiResponse.Success,
                    Message = apiResponse.Mensaje,
                };

                if (finalResponse.Success && _databaseService != null && capture.Id > 0)
                {
                    await _databaseService.DeleteAsync(capture);
                }

                return finalResponse;
            }

            return new ApiResponse<SalidaTrampeoRatas> { Success = false, Message = "Respuesta inválida de la API." };
        }
        catch (Exception ex)
        {
            return new ApiResponse<SalidaTrampeoRatas> { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// Guarda un vale usando el nuevo formato de API
    /// </summary>
    public async Task<StandardApiResponse<object>> SaveValeOnlineAsync(ValeApiRequest valeRequest)
    {
        try
        {
            // Obtener HttpClient con configuracion mas reciente
            var httpClient = await GetConfiguredHttpClientAsync();
            
            // Agregar token Bearer automaticamente
            await EnsureAuthTokenAsync();

            var fullUrl = GetFullUrl("ValesDeSalida/vales_salida");
            var json = JsonConvert.SerializeObject(valeRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            System.Diagnostics.Debug.WriteLine($"Enviando vale a API: {json}");
            
            var response = await httpClient.PostAsync(fullUrl, content);
            
            // Validar respuesta para detectar sesion caducada (401)
            if (!await ValidateHttpResponseAsync(response))
            {
                return new StandardApiResponse<object>
                {
                    Estado = 401,
                    Mensaje = "Sesion caducada",
                    Datos = new List<object>()
                };
            }
            
            var responseContent = await response.Content.ReadAsStringAsync();
            
            System.Diagnostics.Debug.WriteLine($"Respuesta de API (original): {responseContent}");
            
            // Limpiar el contenido de respuesta antes de deserializar
            var cleanedResponseContent = CleanJsonResponse(responseContent);
            
            // Deserializar respuesta StandardApiResponse
            try
            {
                var standardResponse = JsonConvert.DeserializeObject<StandardApiResponse<object>>(cleanedResponseContent);
                if (standardResponse != null)
                {
                    return standardResponse;
                }
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deserializando respuesta: {ex.Message}");
                return new StandardApiResponse<object>
                {
                    Estado = 500,
                    Mensaje = "Error al procesar respuesta del servidor",
                    Datos = new List<object>()
                };
            }
            
            return new StandardApiResponse<object>
            {
                Estado = 500,
                Mensaje = "Respuesta invalida del servidor",
                Datos = new List<object>()
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en SaveValeOnlineAsync: {ex.Message}");
            return new StandardApiResponse<object>
            {
                Estado = 500,
                Mensaje = $"Error de conexion: {ex.Message}",
                Datos = new List<object>()
            };
        }
    }

    #endregion

    #region Vales Historial Operations (CON TOKEN BEARER)

    /// <summary>
    /// Obtiene el historial de vales de salida desde la API
    /// </summary>
    public async Task<HistorialValesResponse> GetValesHistorialAsync()
    {
        try
        {
            // Obtener HttpClient con configuracion mas reciente
            var httpClient = await GetConfiguredHttpClientAsync();
            
            // Agregar token Bearer automaticamente
            await EnsureAuthTokenAsync();

            var fullUrl = GetFullUrl(AppConfigService.ValesSalidasActualesEndpoint);
            System.Diagnostics.Debug.WriteLine($"[GetValesHistorialAsync] Consultando historial en: {fullUrl}");
            
            // Verificar token antes de hacer la llamada
            var authHeader = httpClient.DefaultRequestHeaders.Authorization;
            if (authHeader == null || string.IsNullOrEmpty(authHeader.Parameter))
            {
                System.Diagnostics.Debug.WriteLine("[GetValesHistorialAsync] ERROR: No hay token de autorizacion configurado");
                return new HistorialValesResponse
                {
                    Estado = 401,
                    Mensaje = "Token de autorizacion no disponible",
                    Datos = new List<HistorialValeItem>(),
                    TotalDatos = 0
                };
            }
            
            System.Diagnostics.Debug.WriteLine($"[GetValesHistorialAsync] Token presente: {authHeader.Parameter.Substring(0, Math.Min(10, authHeader.Parameter.Length))}...");
            
            var response = await httpClient.GetAsync(fullUrl);
            
            // Validar respuesta para detectar sesion caducada (401)
            if (!await ValidateHttpResponseAsync(response))
            {
                return new HistorialValesResponse
                {
                    Estado = 401,
                    Mensaje = "Sesion caducada",
                    Datos = new List<HistorialValeItem>(),
                    TotalDatos = 0
                };
            }
            
            var responseContent = await response.Content.ReadAsStringAsync();
            
            System.Diagnostics.Debug.WriteLine($"[GetValesHistorialAsync] Status Code: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"[GetValesHistorialAsync] Response Length: {responseContent?.Length ?? 0}");
            
            if (!string.IsNullOrEmpty(responseContent))
            {
                var previewLength = Math.Min(500, responseContent.Length);
                System.Diagnostics.Debug.WriteLine($"[GetValesHistorialAsync] Response (primeros {previewLength} chars): {responseContent.Substring(0, previewLength)}");
            }
            
            if (response.IsSuccessStatusCode)
            {
                // Limpiar el contenido de respuesta antes de deserializar
                var cleanedContent = CleanJsonResponse(responseContent);
                
                if (!string.IsNullOrEmpty(cleanedContent))
                {
                    var cleanedPreviewLength = Math.Min(300, cleanedContent.Length);
                    System.Diagnostics.Debug.WriteLine($"[GetValesHistorialAsync] JSON limpiado (primeros {cleanedPreviewLength} chars): {cleanedContent.Substring(0, cleanedPreviewLength)}");
                }
                
                // Deserializar la respuesta del historial
                var historialResponse = JsonConvert.DeserializeObject<HistorialValesResponse>(cleanedContent);
                
                if (historialResponse != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[GetValesHistorialAsync] Historial deserializado exitosamente - Estado: {historialResponse.Estado}, TotalDatos: {historialResponse.TotalDatos}");
                    System.Diagnostics.Debug.WriteLine($"[GetValesHistorialAsync] Success flag: {historialResponse.Success}");
                    System.Diagnostics.Debug.WriteLine($"[GetValesHistorialAsync] Datos count: {historialResponse.Datos?.Count ?? 0}");
                    
                    // Log algunos elementos de muestra
                    if (historialResponse.Datos != null && historialResponse.Datos.Any())
                    {
                        foreach (var item in historialResponse.Datos.Take(2))
                        {
                            System.Diagnostics.Debug.WriteLine($"[GetValesHistorialAsync] Vale muestra: ID={item.Id}, Concepto='{item.Concepto}', Usuario='{item.Usuario}'");
                        }
                    }
                    
                    return historialResponse;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[GetValesHistorialAsync] ERROR: la respuesta del historial es null despues de deserializar");
                    return new HistorialValesResponse
                    {
                        Estado = 500,
                        Mensaje = "Error al deserializar la respuesta del servidor - JSON invalido",
                        Datos = new List<HistorialValeItem>(),
                        TotalDatos = 0
                    };
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[GetValesHistorialAsync] ERROR HTTP: {response.StatusCode} - {response.ReasonPhrase}");
                System.Diagnostics.Debug.WriteLine($"[GetValesHistorialAsync] Response body en error: {responseContent}");
                
                string errorMessage = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.InternalServerError => "Error interno del servidor",
                    System.Net.HttpStatusCode.Unauthorized => "Token de autorizacion invalido o expirado",
                    System.Net.HttpStatusCode.Forbidden => "No tiene permisos para acceder al historial",
                    System.Net.HttpStatusCode.NotFound => "Endpoint del historial no encontrado",
                    System.Net.HttpStatusCode.BadRequest => "Solicitud invalida",
                    _ => $"Error del servidor: {response.ReasonPhrase}"
                };
                
                return new HistorialValesResponse
                {
                    Estado = (int)response.StatusCode,
                    Mensaje = errorMessage,
                    Datos = new List<HistorialValeItem>(),
                    TotalDatos = 0
                };
            }
        }
        catch (HttpRequestException httpEx)
        {
            System.Diagnostics.Debug.WriteLine($"[GetValesHistorialAsync] HttpRequestException: {httpEx.Message}");
            return new HistorialValesResponse
            {
                Estado = 500,
                Mensaje = $"Error de conexion HTTP: {httpEx.Message}",
                Datos = new List<HistorialValeItem>(),
                TotalDatos = 0
            };
        }
        catch (TaskCanceledException taskEx)
        {
            System.Diagnostics.Debug.WriteLine($"[GetValesHistorialAsync] TaskCanceledException (Timeout): {taskEx.Message}");
            return new HistorialValesResponse
            {
                Estado = 408,
                Mensaje = "Timeout - La operacion tardo demasiado tiempo",
                Datos = new List<HistorialValeItem>(),
                TotalDatos = 0
            };
        }
        catch (JsonException jsonEx)
        {
            System.Diagnostics.Debug.WriteLine($"[GetValesHistorialAsync] JsonException: {jsonEx.Message}");
            return new HistorialValesResponse
            {
                Estado = 500,
                Mensaje = $"Error al procesar respuesta JSON: {jsonEx.Message}",
                Datos = new List<HistorialValeItem>(),
                TotalDatos = 0
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GetValesHistorialAsync] Excepcion general: {ex}");
            return new HistorialValesResponse
            {
                Estado = 500,
                Mensaje = $"Error inesperado: {ex.Message}",
                Datos = new List<HistorialValeItem>(),
                TotalDatos = 0
            };
        }
    }

    #endregion

    #region Vales Status and Authorization Operations (CON TOKEN BEARER)

    public async Task<List<Salida>> GetValesStatusAsync()
    {
        return await GetCatalogAsync<Salida>(AppConfigService.ApiStatusEndpoint);
    }

    public async Task<List<Salida>> GetValesAutorizacionAsync()
    {
        return await GetCatalogAsync<Salida>(AppConfigService.ApiAutorizacionEndpoint);
    }

    public async Task<List<ValePendienteApiResponse>> GetValePendientesAsync()
    {
        return await GetCatalogAsync<ValePendienteApiResponse>(AppConfigService.ValesSalidasPendienteEndpoint);
    }

    public async Task<ApiResponse<bool>> AuthorizeValeAsync(int valeId, bool authorize)
    {
        try
        {
            Debug.WriteLine($"[AuthorizeValeAsync] ↗ Inicio → ValeId={valeId}, Authorize={authorize}");

            //Preparar HttpClient
            var httpClient = await GetConfiguredHttpClientAsync();
            await EnsureAuthTokenAsync();

            //Seleccionar endpoint
            var endpoint = authorize
                ? AppConfigService.ApiAutorizacionEndpoint
                : AppConfigService.ApiCancelarEndpoint;
            var fullUrl = $"{GetFullUrl(endpoint)}?id={valeId}";
            Debug.WriteLine($"[AuthorizeValeAsync] Endpoint: {endpoint}");
            Debug.WriteLine($"[AuthorizeValeAsync] URL completa: {fullUrl}");

            //Send POST without body
            var response = await httpClient.PostAsync(fullUrl, null);
            Debug.WriteLine($"[AuthorizeValeAsync] HTTP Status: {response.StatusCode}");

            // Validar respuesta para detectar sesion caducada (401)
            if (!await ValidateHttpResponseAsync(response))
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Sesion caducada"
                };
            }

            //Leer cuerpo
            var raw = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"[AuthorizeValeAsync] RAW response: {raw}");

            //Limpiar response
            var cleaned = CleanJsonResponse(raw);
            Debug.WriteLine($"[AuthorizeValeAsync] Cleaned JSON: {cleaned}");

            var j = JObject.Parse(cleaned);
            int estado = j.Value<int>("estado");
            int? datosNull = j["datos"]?.Type == JTokenType.Null
                ? (int?)null
                : j.Value<int?>("datos");
            string msg = j.Value<string>("mensaje") ?? string.Empty;
            Debug.WriteLine($"[AuthorizeValeAsync] Parsed → estado={estado}, datos={(datosNull.HasValue ? datosNull.Value.ToString() : "null")}, mensaje=\"{msg}\"");

            // Determinar éxito
            bool success = estado == 200 && (datosNull.HasValue && datosNull.Value > 0);
            var result = new ApiResponse<bool>
            {
                Success = success,
                Message = msg,
                Data = success
            };
            Debug.WriteLine($"[AuthorizeValeAsync] Final → Success={result.Success}, Message=\"{result.Message}\", Data={result.Data}");

            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AuthorizeValeAsync] EXCEPCIÓN: {ex.Message}");
            return new ApiResponse<bool>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    #endregion

    #region Saldos operations (CON TOKEN BEARER)
    public async Task<SaldoApiResponse?> GetSaldoArticuloAsync(int almacenId, int familiaId, int subFamiliaId, int articuloId)
    {
        try
        {
            // Obtener HttpClient con configuracion mas reciente
            var httpClient = await GetConfiguredHttpClientAsync();
            
            // Asegura que el token de autenticacion este presente
            await EnsureAuthTokenAsync();

            // Construye la URL completa con el parametro 'numero'
            var fullUrl = GetFullUrl($"{AppConfigService.SaldosEndpoint}?almacen={almacenId}&familia={familiaId}&subfamilia={subFamiliaId}&numero={articuloId}");

            Debug.WriteLine($"Consultando saldo en: {fullUrl}");

            var response = await httpClient.GetAsync(fullUrl);
            
            // Validar respuesta para detectar sesion caducada (401)
            if (!await ValidateHttpResponseAsync(response))
            {
                return null; // Sesion caducada
            }
            
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"Error HTTP obteniendo saldo: {response.StatusCode}");
                // Devuelve nulo o una respuesta de error si la llamada falla
                return null;
            }

            var cleanedContent = CleanJsonResponse(responseContent);
            return JsonConvert.DeserializeObject<SaldoApiResponse>(cleanedContent);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Excepcion en GetSaldoArticuloAsync: {ex}");
            return null;
        }
    }
    #endregion

    #region Consulta de Detalles API (CON TOKEN BEARER)
    public async Task<List<ValeDetalleItemDto>> GetValeDetallesAsync(int valeId)
    {
        try
        {
            // Obtener HttpClient con configuración más reciente
            var httpClient = await GetConfiguredHttpClientAsync();
            
            // Asegura que el token de autenticación esté presente
            await EnsureAuthTokenAsync();

            // Construye la URL completa con el parámetro 'Id'
            var endpoint = $"{AppConfigService.DetallesValeEndpoint}?Id={valeId}";
            var fullUrl = GetFullUrl(endpoint);

            System.Diagnostics.Debug.WriteLine($"Consultando detalles de vale en: {fullUrl}");

            var response = await httpClient.GetAsync(fullUrl);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"Error HTTP obteniendo detalles: {response.StatusCode}");
                // Lanza una excepción para que el ViewModel la maneje
                throw new HttpRequestException($"Error del servidor: {response.StatusCode}");
            }

            var apiResponse = JsonConvert.DeserializeObject<ValeDetalleApiResponse>(CleanJsonResponse(responseContent));

            // Devuelve la lista de detalles si la respuesta fue exitosa, o una lista vacía si no.
            return apiResponse?.Datos ?? new List<ValeDetalleItemDto>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Excepción en GetValeDetallesAsync: {ex.Message}");
            // Relanza la excepción para que el ViewModel sepa que algo salió mal.
            throw;
        }
    }

    #endregion

    /// <summary>
    /// Limpia el contenido de respuesta JSON eliminando caracteres de escape y espacios en blanco innecesarios
    /// </summary>
    private static string CleanJsonResponse(string responseContent)
    {
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return string.Empty;
        }

        try
        {
            // Eliminar caracteres de escape comunes como \r\n, \t, etc.
            string cleaned = responseContent
                .Replace("\\r\\n", "") // Eliminar \r\n escapados
                .Replace("\\n", "")    // Eliminar \n escapados
                .Replace("\\r", "")    // Eliminar \r escapados  
                .Replace("\\t", "")    // Eliminar \t escapados
                .Replace("\r\n", "")   // Eliminar saltos de línea reales
                .Replace("\n", "")     // Eliminar saltos de línea reales
                .Replace("\r", "")     // Eliminar retornos de carro reales
                .Replace("\t", "")     // Eliminar tabulaciones reales
                .Trim();               // Eliminar espacios al inicio y final

            // Log para debug
            System.Diagnostics.Debug.WriteLine($"JSON Original: {responseContent}");
            System.Diagnostics.Debug.WriteLine($"JSON Limpiado: {cleaned}");
            
            return cleaned;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error limpiando JSON: {ex.Message}");
            return responseContent; // Devolver el original si hay error
        }
    }

    #region IDisposable

    public void Dispose()
    {
        try
        {
            _currentHttpClient?.Dispose();
            _currentHttpClient = null;
            
            if (_httpClientFactory is IDisposable disposableFactory)
            {
                disposableFactory.Dispose();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error disposing ApiService: {ex.Message}");
        }
    }

    #endregion
}