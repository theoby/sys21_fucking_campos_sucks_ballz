using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace sys21_campos_zukarmex.Services;

/// <summary>
/// Servicio global para monitorear la conectividad de red en toda la aplicaci�n
/// </summary>
public partial class ConnectivityService : ObservableObject, INotifyPropertyChanged
{
    private readonly Timer _connectivityTimer;
    private readonly SessionService _sessionService;
    private bool _isMonitoring = false;
    private bool _hasShownOfflineMessage = false;
    private bool _hasShownOnlineMessage = false;
    private bool _isInitialCheckCompleted = false;
    public ConnectivityService(SessionService sessionService)
    {
        _sessionService = sessionService;
        _connectivityTimer = new Timer(CheckConnectivityCallback, null, Timeout.Infinite, Timeout.Infinite);
        
        // Suscribirse a cambios de conectividad del sistema
        Connectivity.ConnectivityChanged += OnConnectivityChanged;
    }

    #region Properties

    [ObservableProperty]
    private bool isConnected = false; // Inicializar como false hasta verificar

    [ObservableProperty]
    private bool isOnlineMode = false; // Modo online/offline

    [ObservableProperty]
    private string connectionStatus = "Verificando...";

    [ObservableProperty]
    private DateTime lastConnectedTime = DateTime.Now;

    [ObservableProperty]
    private DateTime lastDisconnectedTime;

    [ObservableProperty]
    private bool isAutorizacionEnabled = false;

    [ObservableProperty]
    private bool isHistorialEnabled = false;

    [ObservableProperty]
    private bool canSyncManually = false;

    /// <summary>
    /// Indica si la aplicaci�n debe usar APIs (true) o base de datos local (false)
    /// </summary>
    [ObservableProperty]
    private bool useApiServices = false;

    /// <summary>
    /// Indica si hay una prueba de conectividad en progreso
    /// </summary>
    [ObservableProperty]
    private bool isTestingConnectivity = false;

    #endregion

    #region Public Methods

    /// <summary>
    /// Inicia el monitoreo continuo de conectividad
    /// </summary>
    public void StartMonitoring()
    {
        if (_isMonitoring) return;

        _isMonitoring = true;
        System.Diagnostics.Debug.WriteLine("?? Iniciando monitoreo de conectividad...");
        
        // Verificaci�n inicial inmediata
        _ = CheckConnectivityAsync();
        
        // Configurar timer para verificaciones peri�dicas cada 15 segundos
        _connectivityTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(15));
    }

    /// <summary>
    /// Detiene el monitoreo de conectividad
    /// </summary>
    public void StopMonitoring()
    {
        if (!_isMonitoring) return;

        _isMonitoring = false;
        System.Diagnostics.Debug.WriteLine("?? Deteniendo monitoreo de conectividad...");
        
        _connectivityTimer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>
    /// Verifica la conectividad de forma manual con prueba real de internet
    /// </summary>
    public async Task<bool> CheckConnectivityAsync()
    {
        if (IsTestingConnectivity) return IsConnected; // Evitar m�ltiples pruebas simult�neas

        try
        {
            IsTestingConnectivity = true;
            var networkAccess = Connectivity.Current.NetworkAccess;
            var wasConnected = IsConnected;
            var wasOnlineMode = IsOnlineMode;
            
            System.Diagnostics.Debug.WriteLine($"?? Verificando conectividad - Network Access: {networkAccess}");

            bool hasInternet = false;
            bool hasValidToken = false;
            bool sessionAllowsOnline = true;

            // Verificar acceso a la red
            if (networkAccess == NetworkAccess.Internet)
            {
                // Realizar prueba real de conectividad a internet
                hasInternet = await TestInternetConnectivityAsync();
                System.Diagnostics.Debug.WriteLine($"?? Prueba de internet: {(hasInternet ? "EXITOSA" : "FALLIDA")}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"?? Sin acceso a red - Network Access: {networkAccess}");
            }

            // Verificar si hay token v�lido en configuraci�n (solo si hay internet)
            if (hasInternet)
            {
                hasValidToken = await _sessionService.HasValidTokenInConfigurationAsync();
                System.Diagnostics.Debug.WriteLine($"?? Token válido: {hasValidToken}");
            }

            // Verificar si la sesi�n permite modo online
            var currentSession = await _sessionService.GetCurrentSessionAsync();
            sessionAllowsOnline = currentSession?.IsOnline ?? true;

            // Determinar estado de conectividad
            // Solo requiere internet + token v�lido + sesi�n que permita online
            var newIsConnected = hasInternet && hasValidToken && sessionAllowsOnline;
            
            // Actualizar propiedades
            IsConnected = newIsConnected;
            IsOnlineMode = newIsConnected;
            UseApiServices = newIsConnected;

            // Actualizar estado de conexi�n
            ConnectionStatus = IsConnected ? "Conectado - Modo Online" : "Sin conexión - Modo Offline";

            // Actualizar timestamps
            if (IsConnected)
            {
                LastConnectedTime = DateTime.Now;
            }
            else
            {
                LastDisconnectedTime = DateTime.Now;
            }

            // Actualizar estados de funcionalidades
            UpdateFeatureAvailability();

            // Detectar cambios de estado y manejarlos
            if (wasConnected != IsConnected || wasOnlineMode != IsOnlineMode)
            {
                await HandleConnectivityChange(wasConnected, IsConnected);
                OnConnectivityStateChanged?.Invoke(IsConnected, UseApiServices);
            }

            System.Diagnostics.Debug.WriteLine($"?? Estado final - Conectado: {IsConnected} | Modo Online: {IsOnlineMode} | Usar APIs: {UseApiServices}");
            System.Diagnostics.Debug.WriteLine($"?? Detalles - Internet: {hasInternet} | Token: {hasValidToken} | Sesión Online: {sessionAllowsOnline}");

            return IsConnected;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error verificando conectividad: {ex.Message}");
            
            // En caso de error, usar modo offline
            IsConnected = false;
            IsOnlineMode = false;
            UseApiServices = false;
            ConnectionStatus = "Error - Modo Offline";
            
            UpdateFeatureAvailability();
            return false;
        }
        finally
        {
            IsTestingConnectivity = false;
        }
    }

    /// <summary>
    /// Realiza una prueba real de conectividad a internet
    /// </summary>
    private async Task<bool> TestInternetConnectivityAsync()
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            // Intentar conectar con el servidor de la aplicaci�n si hay URL configurada
            var appApiUrl = AppConfigService.ApiBaseUrl;
            if (!string.IsNullOrWhiteSpace(appApiUrl))
            {
                try
                {
                    var testUrl = appApiUrl.TrimEnd('/') + "/" + AppConfigService.ApiStatusEndpoint;
                    System.Diagnostics.Debug.WriteLine($"?? Probando conectividad con servidor de la app: {testUrl}");
                    
                    var response = await httpClient.GetAsync(testUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"? Conectividad confirmada con servidor de la app: {testUrl}");
                        return true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"?? Servidor de la app respondió con c�digo: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"? Falló conexión con servidor de la app: {ex.Message}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"?? No hay URL de servidor configurada en AppConfigService, probando conectividad general");
            }

            // Probar conectividad general a internet con servicios externos
            var testUrls = new[]
            {
                "https://www.google.com",
                "https://www.microsoft.com",
                "https://httpbin.org/status/200"
            };

            foreach (var url in testUrls)
            {
                try
                {
                    var response = await httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"? Conectividad a internet confirmada con: {url}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"? Falló conexión con {url}: {ex.Message}");
                    continue; // Intentar con el siguiente URL
                }
            }

            System.Diagnostics.Debug.WriteLine($"? Todas las pruebas de conectividad fallaron");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error en prueba de conectividad: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Fuerza el modo offline independientemente del estado de la red
    /// </summary>
    public async Task ForceOfflineModeAsync()
    {
        System.Diagnostics.Debug.WriteLine("?? Forzando modo offline...");
        
        var wasConnected = IsConnected;
        
        IsConnected = false;
        IsOnlineMode = false;
        UseApiServices = false;
        ConnectionStatus = "Modo Offline Forzado";
        
        UpdateFeatureAvailability();
        
        if (wasConnected)
        {
            await HandleConnectivityChange(true, false);
            OnConnectivityStateChanged?.Invoke(false, false);
        }
    }

    /// <summary>
    /// Fuerza una verificaci�n inmediata de conectividad
    /// </summary>
    public async Task<bool> ForceConnectivityCheckAsync()
    {
        System.Diagnostics.Debug.WriteLine("?? Forzando verificación de conectividad...");
        return await CheckConnectivityAsync();
    }

    /// <summary>
    /// Fuerza la actualizaci�n del estado de las funcionalidades
    /// </summary>
    public void UpdateFeatureAvailability()
    {
        // Autorizaci�n e Historial requieren internet y modo online
        IsAutorizacionEnabled = IsConnected && UseApiServices;
        IsHistorialEnabled = IsConnected && UseApiServices;
        
        // Sincronizaci�n manual disponible cuando hay datos locales y no hay conexi�n
        CanSyncManually = !IsConnected || !UseApiServices;

        System.Diagnostics.Debug.WriteLine($"?? Estados actualizados:");
        System.Diagnostics.Debug.WriteLine($"   - Autorización: {IsAutorizacionEnabled}");
        System.Diagnostics.Debug.WriteLine($"   - Historial: {IsHistorialEnabled}");
        System.Diagnostics.Debug.WriteLine($"   - Sync Manual: {CanSyncManually}");
        System.Diagnostics.Debug.WriteLine($"   - Usar APIs: {UseApiServices}");
    }

    /// <summary>
    /// Obtiene un mensaje descriptivo del estado actual
    /// </summary>
    public string GetStatusMessage()
    {
        if (IsConnected && UseApiServices)
        {
            return "Conexión activa - Todas las funciones disponibles (Modo Online)";
        }
        else if (!IsConnected)
        {
            return "Sin conexión a internet - Funcionando con datos locales (Modo Offline)";
        }
        else
        {
            return "Conectividad limitada - Usando datos locales (Modo Offline)";
        }
    }

    /// <summary>
    /// Obtiene informaci�n detallada del estado de conectividad
    /// </summary>
    public ConnectivityInfo GetDetailedConnectivityInfo()
    {
        return new ConnectivityInfo
        {
            IsConnected = IsConnected,
            IsOnlineMode = IsOnlineMode,
            UseApiServices = UseApiServices,
            ConnectionStatus = ConnectionStatus,
            LastConnectedTime = LastConnectedTime,
            LastDisconnectedTime = LastDisconnectedTime,
            StatusMessage = GetStatusMessage(),
            NetworkAccess = Connectivity.Current.NetworkAccess,
            IsTestingConnectivity = IsTestingConnectivity
        };
    }

    #endregion

    #region Private Methods

    private async void CheckConnectivityCallback(object? state)
    {
        if (!_isMonitoring) return;

        try
        {
            await CheckConnectivityAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error en verificación periódica de conectividad: {ex.Message}");
        }
    }

    private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"?? Evento de cambio de conectividad detectado: {e.NetworkAccess}");
        
        // Delay peque�o para evitar m�ltiples verificaciones r�pidas
        await Task.Delay(1000);
        await CheckConnectivityAsync();
    }

    private async Task HandleConnectivityChange(bool wasConnected, bool isNowConnected)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (!wasConnected && isNowConnected)
                {

                    if (!_isInitialCheckCompleted)
                    {
                        _isInitialCheckCompleted = true; // Marcamos que la verificación inicial ya pasó.
                        return; // Salimos para no mostrar el mensaje de "restablecida".
                    }

                    // Se recuper� la conexi�n - cambio a modo online
                    System.Diagnostics.Debug.WriteLine("? Conexión recuperada - Cambiando a modo online");
                    _hasShownOfflineMessage = false;
                    
                    if (!_hasShownOnlineMessage)
                    {
                        _hasShownOnlineMessage = true;
                        await ShowConnectivityRestoredMessageAsync();
                    }
                }
                else if (wasConnected && !isNowConnected)
                {
                    // Se perdi� la conexi�n - cambio a modo offline
                    System.Diagnostics.Debug.WriteLine("? Conexión perdida - Cambiando a modo offline");
                    _hasShownOnlineMessage = false;
                    
                    if (!_hasShownOfflineMessage)
                    {
                        _hasShownOfflineMessage = true;
                        await ShowOfflineMessageAsync();
                    }
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error manejando cambio de conectividad: {ex.Message}");
        }
    }

    private async Task ShowOfflineMessageAsync()
    {
        try
        {
            if (Shell.Current != null)
            {
                await Shell.Current.DisplayAlert(
                    "Modo Offline Activado",
                    "Sin conexión a internet detectada. La aplicación funcionará en modo offline usando datos locales. Solo podrás crear vales y sincronizarlos más tarde.",
                    "Entendido"
                );
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error mostrando mensaje offline: {ex.Message}");
        }
    }

    private async Task ShowConnectivityRestoredMessageAsync()
    {
        try
        {
            if (Shell.Current != null)
            {
                var result = await Shell.Current.DisplayAlert(
                    "Conexión Reestablecida - Modo Online",
                    "Se detectó conexi�n a internet. La aplicaci�n ahora funciona en modo online con acceso completo a todas las funcionalidades. ¿Deseas sincronizar los datos locales?",
                    "Sincronizar Ahora",
                    "Más Tarde"
                );

                if (result)
                {
                    await TriggerSyncRequestAsync();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error mostrando mensaje de conexi�n restaurada: {ex.Message}");
        }
    }

    private async Task TriggerSyncRequestAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("?? Disparando solicitud de sincronización...");
            
            // Dispara un evento para que otras partes de la app puedan reaccionar
            SyncRequested?.Invoke();
            
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("//home");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error activando solicitud de sync: {ex.Message}");
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// Evento que se dispara cuando cambia el estado de conectividad
    /// </summary>
    public event EventHandler<bool>? ConnectivityChanged;

    /// <summary>
    /// Evento que se dispara cuando el usuario solicita sincronizaci�n
    /// </summary>
    public event Action? SyncRequested;

    /// <summary>
    /// Evento que se dispara cuando cambia el estado de conectividad con informaci�n detallada
    /// </summary>
    public event Action<bool, bool>? OnConnectivityStateChanged; // (isConnected, useApiServices)

    /// <summary>
    /// Dispara el evento de cambio de conectividad (mantener por compatibilidad)
    /// </summary>
    protected virtual void OnConnectivityChanged(bool isConnected)
    {
        ConnectivityChanged?.Invoke(this, isConnected);
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        try
        {
            _connectivityTimer?.Dispose();
            Connectivity.ConnectivityChanged -= OnConnectivityChanged;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error disposing ConnectivityService: {ex.Message}");
        }
    }

    #endregion
}

/// <summary>
/// Informaci�n detallada del estado de conectividad
/// </summary>
public class ConnectivityInfo
{
    public bool IsConnected { get; set; }
    public bool IsOnlineMode { get; set; }
    public bool UseApiServices { get; set; }
    public string ConnectionStatus { get; set; } = string.Empty;
    public DateTime LastConnectedTime { get; set; }
    public DateTime LastDisconnectedTime { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public NetworkAccess NetworkAccess { get; set; }
    public bool IsTestingConnectivity { get; set; }
}