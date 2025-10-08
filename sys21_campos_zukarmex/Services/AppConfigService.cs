using sys21_campos_zukarmex.Models;

namespace sys21_campos_zukarmex.Services;

public static class AppConfigService
{
    private static string _dynamicApiBaseUrl = ""; // Default URL - debe estar vac�a, se obtiene de BD
    private static string _fallbackUrl = ""; // Fallback URL - debe estar vac�a, se obtiene de BD
    private static DatabaseService? _databaseService;
    private static bool _isInitialized = false;
    
    public static string ApiBaseUrl => _dynamicApiBaseUrl;
    
    public const string DatabaseName = "sys21_campos_zukarmex.db3";
    
    // API Endpoints
    public const string LoginEndpoint = "Auth/iniciar_sesion";
    public const string ValesSalidaEndpoint = "ValesDeSalida/vales_salida";
    public const string ApiStatusEndpoint = "api_status";
    public const string ApiAutorizacionEndpoint = "ValesDeSalida/Autorizar_Vale";
    public const string ApiCancelarEndpoint = "ValesDeSalida/Cancelar_Vale";

    // Catalog Endpoints
    public const string AlmacenesEndpoint = "Catalogos/Almacen";
    public const string ArticulosEndpoint = "Articulos/Articulos";
    public const string CamposEndpoint = "Catalogos/campos";
    public const string EmpresasEndpoint = "Catalogos/empresaslogin";
    public const string FamiliasEndpoint = "Articulos/Familias";
    public const string InspectoresEndpoint = "Catalogos/inspectores";
    public const string LotesEndpoint = "Catalogos/lotes";
    public const string MaquinariasEndpoint = "Catalogos/Maquinaria";
    public const string RecetasEndpoint = "Recetas/Recetas";
    public const string SubFamiliasEndpoint = "Articulos/SubFamilias";
    public const string ValesSalidasActualesEndpoint = "ValesDeSalida/Obtener_Vales";
    public const string ValesSalidasPendienteEndpoint = "ValesDeSalida/Obtener_Vales_Pendientes";
    public const string SaldosEndpoint = "Articulos/Saldos";
    public const string DetallesValeEndpoint = "ValesDeSalida/Obtener_Vale_Detalle";
    public const string RatCapturesEndpoint = "Trampeo/Captura"; //Falta definir con Roberto
    public const string IrrigationEntriesEndpoint = "Riego/Captura";

    // User Types
    public const int UserTypeAdmin = 1;
    public const int UserTypeSupervisor = 2;
    public const int UserTypeRegular = 3;
    
    // App Settings
    public const int SyncTimeoutSeconds = 100;
    public const int MaxRetryAttempts = 3;
    
    /// <summary>
    /// Inicializar el servicio con el DatabaseService para cargar configuracion
    /// </summary>
    public static void Initialize(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        _isInitialized = true;
    }
    
    
    public static async Task<string> GetApiBaseUrlFromDatabaseAsync()
    {
        try
        {
            if (_databaseService == null || !_isInitialized)
            {
                System.Diagnostics.Debug.WriteLine("DatabaseService no inicializado, usando URL por defecto");
                return _fallbackUrl;
            }

            var repo = new Repositories.ConfiguracionRepository(_databaseService);
            var configuracionActiva = await repo.GetConfiguracionActivaAsync();

            if (configuracionActiva != null && !string.IsNullOrWhiteSpace(configuracionActiva.Ruta))
            {
                var rutaFormateada = configuracionActiva.Ruta.TrimEnd('/') + "/";
                System.Diagnostics.Debug.WriteLine($"URL obtenida de BD (la mas reciente): {rutaFormateada}");
                return rutaFormateada;
            }

            System.Diagnostics.Debug.WriteLine("No se encontro configuracion en BD, usando URL por defecto");
            return _fallbackUrl;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo URL de BD: {ex.Message}");
            return _fallbackUrl;
        }
    }
    
    /// <summary>
    /// Cargar y aplicar la URL desde la base de datos
    /// </summary>
    public static async Task LoadUrlFromDatabaseAsync()
    {
        try
        {
            var urlFromDb = await GetApiBaseUrlFromDatabaseAsync();
            _dynamicApiBaseUrl = urlFromDb;
            System.Diagnostics.Debug.WriteLine($"URL cargada desde BD: {_dynamicApiBaseUrl}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cargando URL desde BD: {ex.Message}");
            _dynamicApiBaseUrl = _fallbackUrl;
        }
    }
    
    /// <summary>
    /// Actualizar la URL base de la API dinamicamente
    /// </summary>
    public static void UpdateApiBaseUrl(string newBaseUrl)
    {
        if (!string.IsNullOrWhiteSpace(newBaseUrl))
        {
            _dynamicApiBaseUrl = newBaseUrl.TrimEnd('/') + "/";
            System.Diagnostics.Debug.WriteLine($"URL actualizada manualmente: {_dynamicApiBaseUrl}");
        }
    }
    
    /// <summary>
    /// Obtener URL completa para un endpoint
    /// </summary>
    public static string GetFullUrl(string endpoint)
    {
        return ApiBaseUrl + endpoint.TrimStart('/');
    }
    
    /// <summary>
    /// Resetear a URL por defecto
    /// </summary>
    public static void ResetToDefaultUrl()
    {
        _dynamicApiBaseUrl = _fallbackUrl;
        System.Diagnostics.Debug.WriteLine($"URL reseteada a defecto: {_dynamicApiBaseUrl}");
    }
    
    /// <summary>
    /// Verificar si el servicio esta inicializado
    /// </summary>
    public static bool IsInitialized => _isInitialized;
    
    /// <summary>
    /// Obtener la URL de fallback
    /// </summary>
    public static string FallbackUrl => _fallbackUrl;

    /// <summary>
    /// Validar que la base de datos tenga empresas guardadas y forzar sincronizacion si es necesario
    /// </summary>
    public static async Task<EmpresaValidationResult> ValidateAndEnsureEmpresasAsync(ApiService apiService)
    {
        var result = new EmpresaValidationResult();
        
        try
        {
            if (_databaseService == null || !_isInitialized)
            {
                result.IsValid = false;
                result.Message = "DatabaseService no inicializado";
                result.RequiresSync = false;
                return result;
            }

            System.Diagnostics.Debug.WriteLine("=== Iniciando validacion de empresas en BD ===");

            // Verificar si hay empresas en la base de datos
            var empresasEnBD = await _databaseService.GetAllAsync<Empresa>();
            var cantidadEmpresas = empresasEnBD?.Count ?? 0;

            System.Diagnostics.Debug.WriteLine($"Empresas encontradas en BD: {cantidadEmpresas}");

            if (cantidadEmpresas > 0)
            {
                // Hay empresas en BD - validacion exitosa
                result.IsValid = true;
                result.Message = $"Se encontraron {cantidadEmpresas} empresas en la base de datos";
                result.RequiresSync = false;
                result.EmpresasCount = cantidadEmpresas;
                
                System.Diagnostics.Debug.WriteLine("Validacion exitosa - Empresas encontradas en BD");
                return result;
            }

            // No hay empresas en BD - intentar sincronizacion forzada
            System.Diagnostics.Debug.WriteLine("No se encontraron empresas en BD - Forzando sincronizacion");
            result.RequiresSync = true;

            // Intentar sincronizar empresas desde la API
            var empresasFromApi = await ForceEmpresasSyncAsync(apiService);
            
            if (empresasFromApi.Success)
            {
                result.IsValid = true;
                result.Message = $"Sincronizacion forzada exitosa: {empresasFromApi.EmpresasCount} empresas sincronizadas";
                result.EmpresasCount = empresasFromApi.EmpresasCount;
                result.WasSyncForced = true;
                
                System.Diagnostics.Debug.WriteLine($"Sincronizacion forzada exitosa: {empresasFromApi.EmpresasCount} empresas");
            }
            else
            {
                result.IsValid = false;
                result.Message = $"Error en sincronizacion forzada: {empresasFromApi.ErrorMessage}";
                result.EmpresasCount = 0;
                result.WasSyncForced = false;
                
                System.Diagnostics.Debug.WriteLine($"Error en sincronizacion forzada: {empresasFromApi.ErrorMessage}");
            }

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Excepcion en ValidateAndEnsureEmpresasAsync: {ex}");
            
            result.IsValid = false;
            result.Message = $"Excepcion durante la validacion: {ex.Message}";
            result.RequiresSync = false;
            result.EmpresasCount = 0;
            
            return result;
        }
    }

    /// <summary>
    /// Forzar sincronizacion de empresas desde la API
    /// </summary>
    public static async Task<ForceSyncResult> ForceEmpresasSyncAsync(ApiService apiService)
    {
        var result = new ForceSyncResult();
        
        try
        {
            System.Diagnostics.Debug.WriteLine("=== Iniciando sincronizacion forzada de empresas ===");
            System.Diagnostics.Debug.WriteLine($"URL API: {ApiBaseUrl}");
            System.Diagnostics.Debug.WriteLine($"Endpoint: {EmpresasEndpoint}");

            // Intentar obtener empresas desde la API
            var empresasFromApi = await apiService.GetEmpresasAsync();

            if (empresasFromApi != null && empresasFromApi.Any())
            {
                System.Diagnostics.Debug.WriteLine($"API devolvio {empresasFromApi.Count} empresas");

                // Limpiar tabla de empresas actual
                if (_databaseService != null)
                {
                    await _databaseService.ClearTableAsync<Empresa>();
                    System.Diagnostics.Debug.WriteLine("Tabla de empresas limpiada");

                    // Insertar nuevas empresas
                    var insertedCount = await _databaseService.InsertAllAsync(empresasFromApi);
                    System.Diagnostics.Debug.WriteLine($"Empresas insertadas en BD: {insertedCount}");

                    // Verificar insercion
                    var verificacion = await _databaseService.GetAllAsync<Empresa>();
                    var cantidadFinal = verificacion?.Count ?? 0;

                    if (cantidadFinal > 0)
                    {
                        result.Success = true;
                        result.EmpresasCount = cantidadFinal;
                        
                        // Log de las primeras empresas para verificacion
                        foreach (var empresa in empresasFromApi.Take(3))
                        {
                            System.Diagnostics.Debug.WriteLine($"  - Empresa ID: {empresa.Id}, Nombre: {empresa.Nombre ?? "Sin nombre"}, IsPromotora: {empresa.IsPromotora}");
                        }
                    }
                    else
                    {
                        result.Success = false;
                        result.ErrorMessage = "Las empresas no se guardaron correctamente en la base de datos";
                    }
                }
                else
                {
                    result.Success = false;
                    result.ErrorMessage = "DatabaseService no disponible para guardar empresas";
                }
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "La API no devolvio empresas o devolvio una lista vacia";
                System.Diagnostics.Debug.WriteLine("API devolvio lista vacia de empresas");
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"Excepcion en ForceEmpresasSyncAsync: {ex}");
        }

        return result;
    }

    /// <summary>
    /// Verificar si hay empresas en la base de datos de forma rapida
    /// </summary>
    public static async Task<bool> HasEmpresasInDatabaseAsync()
    {
        try
        {
            if (_databaseService == null || !_isInitialized)
            {
                return false;
            }

            var count = await _databaseService.CountAsync<Empresa>();
            return count > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error verificando empresas en BD: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Obtener estadisticas de empresas en la base de datos
    /// </summary>
    public static async Task<EmpresaStats> GetEmpresaStatsAsync()
    {
        var stats = new EmpresaStats();
        
        try
        {
            if (_databaseService == null || !_isInitialized)
            {
                return stats;
            }

            var empresas = await _databaseService.GetAllAsync<Empresa>();
            if (empresas != null)
            {
                stats.TotalCount = empresas.Count;
                stats.PromororasCount = empresas.Count(e => e.IsPromotora);
                stats.NonPromororasCount = empresas.Count(e => !e.IsPromotora);
                stats.HasEmpresas = empresas.Any();
                
                if (empresas.Any())
                {
                    stats.FirstEmpresaId = empresas.First().Id;
                    stats.LastEmpresaId = empresas.Last().Id;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo estadisticas de empresas: {ex.Message}");
        }

        return stats;
    }
}

/// <summary>
/// Resultado de la validacion de empresas
/// </summary>
public class EmpresaValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool RequiresSync { get; set; }
    public bool WasSyncForced { get; set; }
    public int EmpresasCount { get; set; }
}

/// <summary>
/// Resultado de la sincronizacion forzada
/// </summary>
public class ForceSyncResult
{
    public bool Success { get; set; }
    public int EmpresasCount { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Estadisticas de empresas en la base de datos
/// </summary>
public class EmpresaStats
{
    public int TotalCount { get; set; }
    public int PromororasCount { get; set; }
    public int NonPromororasCount { get; set; }
    public bool HasEmpresas { get; set; }
    public int FirstEmpresaId { get; set; }
    public int LastEmpresaId { get; set; }
}