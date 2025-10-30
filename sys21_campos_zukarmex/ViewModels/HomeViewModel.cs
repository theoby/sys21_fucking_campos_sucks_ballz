using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Services;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Models.DTOs.Sync;
using System.Collections.ObjectModel;
using sys21_campos_zukarmex.Services;
using System.Collections.ObjectModel;
using System.Text;

namespace sys21_campos_zukarmex.ViewModels;

public partial class HomeViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;
    private readonly SessionService _sessionService;
    private readonly SyncService _syncService;
    private readonly ConnectivityService _connectivityService;
    private readonly ApiService _apiService;
    private readonly NavigationService _navigationService;

    public HomeViewModel(DatabaseService databaseService, SessionService sessionService, SyncService syncService, ConnectivityService connectivityService, ApiService apiService, NavigationService navigationService)
    {
        _databaseService = databaseService;
        _sessionService = sessionService;
        _syncService = syncService;
        _connectivityService = connectivityService;
        _apiService = apiService;
        _navigationService = navigationService;
        Title = "Inicio";
        
        // Inicializar conectividad
        InitializeConnectivity(_connectivityService);
        
        // Suscribirse a solicitudes de sincronizacion
        _connectivityService.SyncRequested += OnSyncRequestedFromConnectivity;
    }

    [ObservableProperty]
    private string welcomeMessage = "Bienvenido a sys21_campos_zukarmex";

    [ObservableProperty]
    private string userName = string.Empty;

    [ObservableProperty]
    private string companyName = string.Empty;

    [ObservableProperty]
    private string userRole = string.Empty;

    [ObservableProperty]
    private int totalVales;

    [ObservableProperty]
    private int pendingVales;

    [ObservableProperty]
    private int syncedVales;

    [ObservableProperty]
    private bool isSyncInProgress;

    [ObservableProperty]
    private string syncProgress = string.Empty;

    [ObservableProperty]
    private int syncProgressValue;

    [ObservableProperty]
    private string lastSyncMessage = string.Empty;

    [ObservableProperty]
    private bool hasPendingValesOffline;

    [ObservableProperty]
    private int valesToAuthorize;

    [ObservableProperty]
    private bool hasValesToAuthorize;

    [ObservableProperty]
    private bool canAuthorize;

    public override async Task InitializeAsync()
    {
        System.Diagnostics.Debug.WriteLine("?? === INICIANDO INICIALIZACI�N DE HOMEVIEWMODEL (MODO PERMISIVO) ===");
        System.Diagnostics.Debug.WriteLine($"?? Tiempo actual: {DateTime.Now}");
        
        try
        {
            // Verificaci�n menos agresiva - solo verificar que hay una sesi�n
            System.Diagnostics.Debug.WriteLine("?? Verificando disponibilidad de sesi�n...");
            var currentSession = await _sessionService.GetCurrentSessionAsync();
            
            if (currentSession == null)
            {
                System.Diagnostics.Debug.WriteLine("?? No hay sesi�n disponible en HomePage");
                // No redirigir autom�ticamente, puede ser un problema temporal
                WelcomeMessage = "Sesi�n no disponible";
                UserName = "Usuario";
                UserRole = "Desconocido";
                CompanyName = "No disponible";
                return;
            }
            
            System.Diagnostics.Debug.WriteLine("? Sesi�n encontrada en HomePage:");
            System.Diagnostics.Debug.WriteLine($"   - Usuario: {currentSession.Username} ({currentSession.NombreCompleto})");
            System.Diagnostics.Debug.WriteLine($"   - Empresa ID: {currentSession.IdEmpresa}");
            System.Diagnostics.Debug.WriteLine($"   - Token presente: {(!string.IsNullOrEmpty(currentSession.Token) ? "S�" : "NO")}");
            System.Diagnostics.Debug.WriteLine($"   - Expira: {currentSession.ExpirationDate}");
            System.Diagnostics.Debug.WriteLine($"   - IsActive: {currentSession.IsActive}");
            
            // Configurar token en ApiService si est� disponible
            if (!string.IsNullOrEmpty(currentSession.Token))
            {
                _apiService.SetAuthToken(currentSession.Token);
                System.Diagnostics.Debug.WriteLine("?? Token configurado en ApiService");
            }
            
            // Continuar con la inicializaci�n normal sin verificaciones estrictas
            System.Diagnostics.Debug.WriteLine("?? Procediendo con inicializaci�n normal...");
            
            await LoadUserInfoAsync();
            await LoadStatisticsAsync();
            
            CanAuthorize = await _sessionService.CanAuthorizeAsync();
            if (CanAuthorize)
            {
                await GetPendingValesCountAsync();
            }
            
            System.Diagnostics.Debug.WriteLine("? === INICIALIZACI�N DE HOMEVIEWMODEL COMPLETADA EXITOSAMENTE ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? === ERROR EN INICIALIZACI�N DE HOMEVIEWMODEL ===");
            System.Diagnostics.Debug.WriteLine($"? Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"? StackTrace: {ex.StackTrace}");
            
            // En caso de error, mostrar valores por defecto en lugar de redirigir
            WelcomeMessage = "Error cargando informaci�n";
            UserName = "Usuario";
            UserRole = "Desconocido";
            CompanyName = "Error";
            
            // Solo mostrar un mensaje de error sin navegar
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.DisplayAlert("Advertencia", 
                    "Hubo un problema cargando la informaci�n. Algunos datos pueden no estar disponibles.", "OK");
            });
        }
    }

    protected override void OnConnectivityStateChanged(bool isConnected)
    {
        base.OnConnectivityStateChanged(isConnected);
        
        // Actualizar estadisticas cuando cambia la conectividad
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await LoadStatisticsAsync();
        });
    }

    private async Task LoadUserInfoAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("?? === INICIANDO CARGA DE INFORMACI�N DE USUARIO (MODO TOLERANTE) ===");
            
            // Obtener sesi�n de forma segura
            var session = await _sessionService.GetCurrentSessionAsync();
            
            if (session == null)
            {
                System.Diagnostics.Debug.WriteLine("?? No hay sesi�n disponible - usando valores por defecto");
                WelcomeMessage = "Sesi�n no disponible";
                UserName = "Usuario";
                UserRole = "Desconocido";
                CompanyName = "No disponible";
                return; // Retornar sin lanzar excepci�n
            }
            
            System.Diagnostics.Debug.WriteLine($"? Sesi�n encontrada - Usuario: {session.Username}, Empresa ID: {session.IdEmpresa}");
            
            // Obtener informaci�n de usuario de forma segura
            try
            {
                UserName = await _sessionService.GetCurrentUserNameAsync() ?? "Usuario";
                System.Diagnostics.Debug.WriteLine($"?? Nombre de usuario: '{UserName}'");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"?? Error obteniendo nombre de usuario: {ex.Message}");
                UserName = session.NombreCompleto ?? session.Username ?? "Usuario";
            }
            
            // Obtener rol de usuario de forma segura
            try
            {
                UserRole = await _sessionService.GetCurrentUserRoleAsync() ?? "Desconocido";
                System.Diagnostics.Debug.WriteLine($"?? Rol de usuario: '{UserRole}'");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"?? Error obteniendo rol de usuario: {ex.Message}");
                UserRole = "Usuario est�ndar";
            }
            
            // Obtener informaci�n de empresa de forma segura
            try
            {
                var empresas = await _databaseService.GetAllAsync<Empresa>();
                
                if (empresas != null && empresas.Any())
                {
                    var empresa = empresas.FirstOrDefault(e => e.Id == session.IdEmpresa);
                    if (empresa != null)
                    {
                        CompanyName = empresa.Nombre;
                        System.Diagnostics.Debug.WriteLine($"?? Empresa encontrada: '{CompanyName}' (ID: {empresa.Id})");
                    }
                    else
                    {
                        CompanyName = $"Empresa ID: {session.IdEmpresa}";
                        System.Diagnostics.Debug.WriteLine($"?? No se encontr� empresa con ID {session.IdEmpresa}");
                    }
                }
                else
                {
                    CompanyName = "Sin empresas en BD";
                    System.Diagnostics.Debug.WriteLine("?? No hay empresas en la base de datos");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"?? Error obteniendo informaci�n de empresa: {ex.Message}");
                CompanyName = $"Empresa ID: {session.IdEmpresa}";
            }
            
            // Configurar mensaje de bienvenida
            WelcomeMessage = string.IsNullOrEmpty(UserName) || UserName == "Usuario" 
                ? "Bienvenido" 
                : $"Bienvenido, {UserName}";
            
            System.Diagnostics.Debug.WriteLine($"?? Mensaje de bienvenida: '{WelcomeMessage}'");
            System.Diagnostics.Debug.WriteLine("? === CARGA DE INFORMACI�N DE USUARIO COMPLETADA (MODO TOLERANTE) ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? === ERROR GENERAL EN CARGA DE INFORMACI�N DE USUARIO ===");
            System.Diagnostics.Debug.WriteLine($"? Error: {ex.Message}");
            
            // Valores por defecto seguros - NO relanzar excepci�n
            WelcomeMessage = "Error cargando informaci�n";
            UserName = "Usuario";
            UserRole = "Desconocido";
            CompanyName = "Error";
            
            System.Diagnostics.Debug.WriteLine("??? Valores por defecto aplicados - continuando sin interrumpir la aplicaci�n");
        }
    }

    private async Task LoadStatisticsAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("?? === INICIANDO CARGA DE ESTAD�STICAS ===");
            
            System.Diagnostics.Debug.WriteLine("?? Obteniendo todas las salidas de la base de datos...");
            var salidas = await _databaseService.GetAllAsync<Salida>();
            
            if (salidas != null)
            {
                TotalVales = salidas.Count;
                PendingVales = salidas.Count(s => !s.Status);
                SyncedVales = salidas.Count(s => s.Status);
                
                System.Diagnostics.Debug.WriteLine($"?? Estad�sticas calculadas:");
                System.Diagnostics.Debug.WriteLine($"   - Total vales: {TotalVales}");
                System.Diagnostics.Debug.WriteLine($"   - Vales pendientes: {PendingVales}");
                System.Diagnostics.Debug.WriteLine($"   - Vales sincronizados: {SyncedVales}");
                
                // Determinar si hay vales pendientes creados offline
                HasPendingValesOffline = PendingVales > 0;
                System.Diagnostics.Debug.WriteLine($"   - Tiene vales offline pendientes: {HasPendingValesOffline}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("?? La consulta de salidas devolvi� null");
                TotalVales = 0;
                PendingVales = 0;
                SyncedVales = 0;
                HasPendingValesOffline = false;
            }
            
            System.Diagnostics.Debug.WriteLine("? === CARGA DE ESTAD�STICAS COMPLETADA ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? === ERROR EN CARGA DE ESTAD�STICAS ===");
            System.Diagnostics.Debug.WriteLine($"? Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"? StackTrace: {ex.StackTrace}");
            
            // Establecer valores por defecto en caso de error
            TotalVales = 0;
            PendingVales = 0;
            SyncedVales = 0;
            HasPendingValesOffline = false;
            
            // No re-lanzar la excepci�n para que no falle toda la inicializaci�n
            // Solo registrar el error
        }
    }

    [RelayCommand]
    private async Task NavigateToValesAsync()
    {
        await Shell.Current.GoToAsync("//vale");
    }

    [RelayCommand]
    private async Task NavigateToStatusAsync()
    {
        await Shell.Current.GoToAsync("//status");
    }

    [RelayCommand]
    private async Task NavigateToAuthorizationAsync()
    {
        if (!IsFeatureAvailable("autorizacion"))
        {
            await ShowFeatureUnavailableMessageAsync("autorizacion");
            return;
        }

        if (await _sessionService.CanAuthorizeAsync())
        {
            await Shell.Current.GoToAsync("//autorizacion");
        }
        else
        {
            await Shell.Current.DisplayAlert("Acceso Denegado", 
                "No tiene permisos para autorizar vales", "OK");
        }
    }

    [RelayCommand]
    private async Task NavigateToHistorialAsync()
    {
        if (!IsFeatureAvailable("historial"))
        {
            await ShowFeatureUnavailableMessageAsync("historial");
            return;
        }

        await Shell.Current.GoToAsync("//historial");
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var confirm = await Shell.Current.DisplayAlert("Cerrar Sesion", 
            "Esta seguro que desea cerrar sesion?", "Si", "No");

        if (confirm)
        {
            await _sessionService.ClearSessionAsync();
            _connectivityService.StopMonitoring();
            await Shell.Current.GoToAsync("//login");
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await InitializeAsync();
    }

    [RelayCommand]
    private async Task SincronizarCatalogosCompletaAsync()
    {
        if (!IsConnected)
        {
            await Shell.Current.DisplayAlert("Sin Conexion", 
                "La sincronizacion requiere conexion a internet.", "OK");
            return;
        }

        if (IsBusy || IsSyncInProgress) return;

        var confirm = await Shell.Current.DisplayAlert("Sincronizacion Completa", 
            "Desea sincronizar todos los catalogos? Esto puede tomar varios minutos y reemplazara todos los datos locales.", 
            "Si, Sincronizar", "Cancelar");

        if (!confirm) return;

        await ExecuteSyncAsync();
    }

    [RelayCommand]
    private async Task SyncPendingValesAsync()
    {
        if (!IsConnected)
        {
            await Shell.Current.DisplayAlert("Sin Conexion", 
                "La sincronizacion de vales requiere conexion a internet.", "OK");
            return;
        }

        if (!HasPendingValesOffline)
        {
            await Shell.Current.DisplayAlert("Info", 
                "No hay vales pendientes de sincronizacion.", "OK");
            return;
        }

        var confirm = await Shell.Current.DisplayAlert("Sincronizar Vales", 
            $"Desea sincronizar {PendingVales} vales pendientes?", 
            "Si, Sincronizar", "Cancelar");

        if (confirm)
        {
            await ExecuteValesSyncAsync();
        }
    }
    //Asignar valor actual a la variable global de Vales pendientes
    private async Task GetPendingValesCountAsync()
    {
        var valesToAuthorize = await _syncService.GetPendingValesCountAsync();
        if (valesToAuthorize != 0)
            HasValesToAuthorize = true;
        else
            HasValesToAuthorize = false;

        ValesToAuthorize = valesToAuthorize;
    }

    private async Task ExecuteSyncAsync()
    {
        IsSyncInProgress = true;
        SyncProgressValue = 0;
        SyncProgress = "Iniciando sincronizacion completa...";
        LastSyncMessage = string.Empty;

        try
        {
            // Configurar progreso
            var progress = new Progress<SyncStatus>(status =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SyncProgress = $"Sincronizando {status.CatalogName}... {status.Progress}%";
                    SyncProgressValue = status.Progress;
                });
            });

            // Ejecutar sincronizacion completa forzada
            var results = await _syncService.ForceFullResyncAsync(progress);

            // Verificar resultados
            var successCount = results.Count(r => r.Success);
            var totalCount = results.Count;
            var totalRecords = results.Where(r => r.Success).Sum(r => r.RecordsCount);

            SyncProgress = $"Sincronizacion completada: {successCount}/{totalCount} catalogos";
            SyncProgressValue = 100;

            if (successCount == totalCount)
            {
                LastSyncMessage = $"Sincronizacion exitosa: {totalRecords} registros actualizados";
                await Shell.Current.DisplayAlert("Exito", 
                    $"Sincronizacion completada exitosamente\n{successCount} catalogos sincronizados\n{totalRecords} registros actualizados", "OK");
            }
            else
            {
                var failedCount = totalCount - successCount;
                LastSyncMessage = $"Sincronizacion parcial: {successCount}/{totalCount} catalogos";
                await Shell.Current.DisplayAlert("Advertencia", 
                    $"Sincronizacion parcial\n{successCount} exitosos, {failedCount} fallidos\n{totalRecords} registros actualizados", "OK");
            }

            // Actualizar estadisticas despues de la sincronizacion
            await LoadStatisticsAsync();
        }
        catch (Exception ex)
        {
            SyncProgress = "Error en la sincronizacion";
            LastSyncMessage = $"Error: {ex.Message}";
            await Shell.Current.DisplayAlert("Error", $"Error durante la sincronizacion: {ex.Message}", "OK");
        }
        finally
        {
            IsSyncInProgress = false;
        }
    }

    private async Task ExecuteValesSyncAsync()
    {
        await LoadStatisticsAsync();
        if (PendingVales == 0)
        {
            await Shell.Current.DisplayAlert("Informacion", "No hay vales pendientes para sincronizar.", "OK");
            return;
        }

        SetBusy(true);
        var ValesExitosos = new List<string>();
        var ValesFallidos = new List<string>();

        try
        {
            // Obtener la lista de vales pendientes desde la base de datos
            var todosLosVales = await _databaseService.GetAllAsync<Salida>();
            var valesASincronizar = todosLosVales.Where(v => !v.Status).ToList();

            foreach (var vale in valesASincronizar)
            {
                try
                {
                    // Cargar los detalles del vale desde la BD antes de enviarlo
                    vale.SalidaDetalle = await _databaseService.GetDetallesBySalidaAsync(vale.Id);

                    // Llamar al ApiService para guardar el vale
                    var apiResponse = await _apiService.SaveValeAsync(vale);

                    if (apiResponse.Success)
                    {
                        // Si la API tuvo exito, se borra de la BD local (esta logica ya esta en tu ApiService)
                        ValesExitosos.Add($"Vale #{vale.Id} ({vale.Concepto})");
                    }
                    else
                    {
                        ValesFallidos.Add($"Vale #{vale.Id}: {apiResponse.Message}");
                    }
                }
                catch (Exception ex)
                {
                    ValesFallidos.Add($"Vale #{vale.Id}: {ex.Message}");
                }
            }

            // Construir y mostrar el mensaje de resumen
            var resumen = new StringBuilder();
            resumen.AppendLine($"Sincronizacion finalizada.\n");
            resumen.AppendLine($"Exitos: {ValesExitosos.Count}");
            resumen.AppendLine($"Fallidos: {ValesFallidos.Count}\n");

            if (ValesExitosos.Any())
            {
                resumen.AppendLine("Vales Sincronizados:");
                foreach (var exito in ValesExitosos)
                {
                    resumen.AppendLine($"- {exito}");
                }
            }

            if (ValesFallidos.Any())
            {
                resumen.AppendLine("\nVales con Error:");
                foreach (var fallo in ValesFallidos)
                {
                    resumen.AppendLine($"- {fallo}");
                }
            }

            await Shell.Current.DisplayAlert("Resumen de Sincronizacion", resumen.ToString(), "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Ocurrio un error inesperado: {ex.Message}", "OK");
        }
        finally
        {
            // Al final, refresca las estadisticas de la HomePage para que se actualicen los contadores
            await LoadStatisticsAsync();
            SetBusy(false);
        }
    }

    private async void OnSyncRequestedFromConnectivity()
    {
        if (HasPendingValesOffline)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var result = await Shell.Current.DisplayAlert(
                    "Sincronizar Vales Pendientes",
                    $"Se detectaron {PendingVales} vales creados offline. Desea sincronizarlos ahora?",
                    "Sincronizar",
                    "Mas Tarde"
                );

                if (result)
                {
                    await ExecuteValesSyncAsync();
                }
            });
        }
    }

    protected override void Cleanup()
    {
        if (_connectivityService != null)
        {
            _connectivityService.SyncRequested -= OnSyncRequestedFromConnectivity;
        }
        base.Cleanup();
    }

    [RelayCommand]
    private async Task ShowConfiguracionInfoAsync()
    {
        try
        {
            var configuraciones = await _databaseService.GetAllAsync<Configuracion>();
            var info = new StringBuilder();
            
            info.AppendLine("=== INFORMACI�N DE CONFIGURACIONES ===");
            info.AppendLine($"Total configuraciones: {configuraciones?.Count ?? 0}");
            info.AppendLine($"Fecha consulta: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            info.AppendLine();
            
            if (configuraciones != null && configuraciones.Any())
            {
                foreach (var config in configuraciones.OrderByDescending(c => c.Fecha))
                {
                    info.AppendLine($"?? CONFIGURACI�N ID: {config.Id}");
                    info.AppendLine($"   Dispositivo: {config.Dispositivo}");
                    info.AppendLine($"   Ruta: {config.Ruta}");
                    info.AppendLine($"   Fecha: {config.Fecha:dd/MM/yyyy HH:mm:ss}");
                    info.AppendLine($"   Token presente: {(!string.IsNullOrEmpty(config.Token) ? "S�" : "NO")}");
                    
                    if (!string.IsNullOrEmpty(config.Token))
                    {
                        info.AppendLine($"   Token longitud: {config.Token.Length} caracteres");
                        info.AppendLine($"   Token preview: {config.Token.Substring(0, Math.Min(20, config.Token.Length))}...");
                    }
                    
                    if (config.TokenExpiration.HasValue)
                    {
                        info.AppendLine($"   Token expira: {config.TokenExpiration:dd/MM/yyyy HH:mm:ss}");
                        var timeRemaining = config.TokenExpiration.Value - DateTime.Now;
                        info.AppendLine($"   Tiempo restante: {timeRemaining.TotalMinutes:F1} minutos");
                        info.AppendLine($"   �Expirado?: {(config.TokenExpiration <= DateTime.Now ? "S�" : "NO")}");
                    }
                    else
                    {
                        info.AppendLine($"   Token expira: NUNCA (permanente)");
                    }
                    
                    info.AppendLine($"   HasValidToken: {config.HasValidToken}");
                    info.AppendLine();
                }
                
                // Mostrar configuraci�n activa
                var activeConfig = configuraciones
                    .Where(c => !string.IsNullOrEmpty(c.Token))
                    .OrderByDescending(c => c.Fecha)
                    .FirstOrDefault();
                    
                if (activeConfig != null)
                {
                    info.AppendLine("?? CONFIGURACI�N ACTIVA DETECTADA:");
                    info.AppendLine($"   ID: {activeConfig.Id}");
                    info.AppendLine($"   Dispositivo: {activeConfig.Dispositivo}");
                    info.AppendLine($"   Token v�lido: {activeConfig.HasValidToken}");
                }
                else
                {
                    info.AppendLine("? NO HAY CONFIGURACI�N ACTIVA CON TOKEN");
                }
            }
            else
            {
                info.AppendLine("? NO HAY CONFIGURACIONES EN LA BASE DE DATOS");
            }
            
            await Shell.Current.DisplayAlert("Informaci�n de Configuraciones", info.ToString(), "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error obteniendo informaci�n de configuraci�n: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task ShowSessionInfoAsync()
    {
        try
        {
            var sessions = await _databaseService.GetAllAsync<Session>();
            var info = new StringBuilder();
            
            info.AppendLine("=== INFORMACI�N DE SESSIONS ===");
            info.AppendLine($"Total sessions: {sessions?.Count ?? 0}");
            info.AppendLine($"Fecha consulta: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            info.AppendLine();
            
            if (sessions != null && sessions.Any())
            {
                foreach (var session in sessions.OrderByDescending(s => s.CreatedAt))
                {
                    info.AppendLine($"?? SESSION ID: {session.Id}");
                    info.AppendLine($"   Usuario: {session.Username}");
                    info.AppendLine($"   Nombre: {session.NombreCompleto}");
                    info.AppendLine($"   Empresa ID: {session.IdEmpresa}");
                    info.AppendLine($"   Tipo Usuario: {session.TipoUsuario}");
                    info.AppendLine($"   Inspector ID: {session.IdInspector}");
                    info.AppendLine($"   Es Promotora: {session.IsPromotora}");
                    info.AppendLine($"   Es Online: {session.IsOnline}");
                    info.AppendLine($"   Activa: {session.IsActive}");
                    info.AppendLine($"   Creada: {session.CreatedAt:dd/MM/yyyy HH:mm:ss}");
                    info.AppendLine($"   Expira: {session.ExpirationDate:dd/MM/yyyy HH:mm:ss}");
                    
                    var timeRemaining = session.ExpirationDate - DateTime.Now;
                    info.AppendLine($"   Tiempo restante: {timeRemaining.TotalMinutes:F1} minutos");
                    info.AppendLine($"   �Expirada?: {(session.ExpirationDate <= DateTime.Now ? "S�" : "NO")}");
                    
                    info.AppendLine($"   Token presente: {(!string.IsNullOrEmpty(session.Token) ? "S�" : "NO")}");
                    
                    if (!string.IsNullOrEmpty(session.Token))
                    {
                        info.AppendLine($"   Token longitud: {session.Token.Length} caracteres");
                        info.AppendLine($"   Token preview: {session.Token.Substring(0, Math.Min(20, session.Token.Length))}...");
                    }
                    
                    // Verificar si cumple todos los criterios para ser v�lida
                    bool isValidSession = !string.IsNullOrEmpty(session.Token) && 
                                         session.ExpirationDate > DateTime.Now && 
                                         session.IsActive;
                    info.AppendLine($"   �Session v�lida?: {(isValidSession ? "S�" : "NO")}");
                    info.AppendLine();
                }
                
                // Mostrar session activa
                var activeSession = sessions.FirstOrDefault(s => 
                    !string.IsNullOrEmpty(s.Token) && 
                    s.ExpirationDate > DateTime.Now && 
                    s.IsActive);
                    
                if (activeSession != null)
                {
                    info.AppendLine("?? SESSION ACTIVA DETECTADA:");
                    info.AppendLine($"   ID: {activeSession.Id}");
                    info.AppendLine($"   Usuario: {activeSession.Username} ({activeSession.NombreCompleto})");
                    info.AppendLine($"   Empresa: {activeSession.IdEmpresa}");
                }
                else
                {
                    info.AppendLine("? NO HAY SESSION ACTIVA V�LIDA");
                }
            }
            else
            {
                info.AppendLine("? NO HAY SESSIONS EN LA BASE DE DATOS");
            }
            
            await Shell.Current.DisplayAlert("Informaci�n de Sessions", info.ToString(), "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error obteniendo informaci�n de session: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task ShowNavigationDiagnosticAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("?? === GENERANDO DIAGN�STICO DE NAVEGACI�N MEJORADO ===");
            
            // Usar el NavigationService para obtener diagn�stico m�s completo
            var navigationDiagnostic = await _navigationService.GetNavigationDiagnosticAsync();
            
            // Tambi�n obtener diagn�stico del SessionService
            var sessionDiagnostic = await _sessionService.GetNavigationDiagnosticAsync();
            
            var fullDiagnostic = $"{navigationDiagnostic}\n\n{sessionDiagnostic}";
            
            System.Diagnostics.Debug.WriteLine("?? === DIAGN�STICO COMPLETO ===");
            System.Diagnostics.Debug.WriteLine(fullDiagnostic);
            System.Diagnostics.Debug.WriteLine("?? === FIN DIAGN�STICO ===");
            
            await Shell.Current.DisplayAlert("Diagn�stico Completo", fullDiagnostic, "OK");
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error generando diagn�stico: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"? {errorMsg}");
            await Shell.Current.DisplayAlert("Error", errorMsg, "OK");
        }
    }

    [RelayCommand]
    private async Task TestNavigationAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("?? === TEST DE NAVEGACI�N DESDE HOMEPAGE ===");
            
            // Verificar estado actual
            var currentSession = await _sessionService.GetCurrentSessionAsync();
            if (currentSession != null)
            {
                System.Diagnostics.Debug.WriteLine($"?? Sesi�n actual: ID {currentSession.Id}, Usuario: {currentSession.Username}");
                System.Diagnostics.Debug.WriteLine($"?? Token presente: {!string.IsNullOrEmpty(currentSession.Token)}");
                System.Diagnostics.Debug.WriteLine($"?? Expira: {currentSession.ExpirationDate}");
                System.Diagnostics.Debug.WriteLine($"?? IsActive: {currentSession.IsActive}");
                
                var timeRemaining = currentSession.ExpirationDate - DateTime.Now;
                System.Diagnostics.Debug.WriteLine($"?? Tiempo restante: {timeRemaining.TotalMinutes:F1} minutos");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("?? NO HAY SESI�N ACTUAL");
            }
            
            // Usar NavigationService para diagn�stico
            var navigationDiagnostic = await _navigationService.GetNavigationDiagnosticAsync();
            
            await Shell.Current.DisplayAlert("Test de Navegaci�n", 
                $"Estado actual: {(currentSession != null ? "SESI�N ENCONTRADA" : "SIN SESI�N")}\n\n" +
                $"Deber�a estar en HOME: {(currentSession != null && !string.IsNullOrEmpty(currentSession.Token) && currentSession.ExpirationDate > DateTime.Now)}\n\n" +
                "Ver logs para detalles completos.", "OK");
                
            System.Diagnostics.Debug.WriteLine("?? === FIN TEST DE NAVEGACI�N ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"?? ? Error en test de navegaci�n: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", $"Error en test: {ex.Message}", "OK");
        }
    }
}