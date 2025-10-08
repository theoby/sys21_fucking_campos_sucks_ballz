using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Models.DTOs.Authentication;
using sys21_campos_zukarmex.Services;

namespace sys21_campos_zukarmex.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly ApiService _apiService;
    private readonly DatabaseService _databaseService;
    private readonly SessionService _sessionService;

    public LoginViewModel(ApiService apiService, DatabaseService databaseService, SessionService sessionService)
    {
        _apiService = apiService;
        _databaseService = databaseService;
        _sessionService = sessionService;
        Title = "Iniciar Sesion";
        Empresas = new ObservableCollection<Empresa>();
    }

    [ObservableProperty]
    private string usuario = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private Empresa? selectedEmpresa;

    [ObservableProperty]
    private ObservableCollection<Empresa> empresas;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isEmpresaRequired = true;

    [ObservableProperty]
    private bool showUrlConfigWarning = false;

    [ObservableProperty]
    private string urlConfigMessage = string.Empty;

    public override async Task InitializeAsync()
    {
        System.Diagnostics.Debug.WriteLine("?? Inicializando LoginViewModel...");

        // NO verificar token ni navegar autom�ticamente aqu�
        // La navegaci�n inicial ya se maneja en App.xaml.cs
        // Solo preparar la pantalla de login

        // Verificar configuraci�n de URL antes de cargar empresas
        await ValidateUrlConfigurationAsync();

        // Cargar empresas para mostrar en el dropdown
        await LoadEmpresasAsync();
        
        System.Diagnostics.Debug.WriteLine("? LoginViewModel inicializado - pantalla lista para uso");
    }

    [RelayCommand]
    private async Task LoadEmpresasAsync()
    {
        try
        {
            // Re-validar configuraci�n de URL antes de cargar empresas
            await ValidateUrlConfigurationAsync();
            
            // First check if we have empresas in database quickly
            var hasEmpresas = await AppConfigService.HasEmpresasInDatabaseAsync();
            
            if (hasEmpresas)
            {
                // Load from database
                var empresas = await _databaseService.GetAllAsync<Empresa>();
                Empresas.Clear();
                foreach (var empresa in empresas)
                {
                    Empresas.Add(empresa);
                }
                
                var stats = await AppConfigService.GetEmpresaStatsAsync();
                System.Diagnostics.Debug.WriteLine($"Empresas cargadas desde BD: {stats.TotalCount} (Promotoras: {stats.PromororasCount})");
            }
            else
            {
                // No empresas in database - force sync
                System.Diagnostics.Debug.WriteLine("No hay empresas en BD - Iniciando sincronizacion forzada");
                
                try
                {
                    var syncResult = await AppConfigService.ForceEmpresasSyncAsync(_apiService);
                    
                    if (syncResult.Success && syncResult.EmpresasCount > 0)
                    {
                        // Load newly synced empresas
                        var empresas = await _databaseService.GetAllAsync<Empresa>();
                        Empresas.Clear();
                        foreach (var empresa in empresas)
                        {
                            Empresas.Add(empresa);
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"Sincronizacion forzada exitosa: {syncResult.EmpresasCount} empresas");
                    }
                    else
                    {
                        ErrorMessage = $"No se pudieron sincronizar las empresas: {syncResult.ErrorMessage}";
                        System.Diagnostics.Debug.WriteLine($"Error en sincronizacion forzada: {syncResult.ErrorMessage}");
                    }
                }
                catch (Exception syncEx)
                {
                    ErrorMessage = $"Error sincronizando empresas: {syncEx.Message}";
                    System.Diagnostics.Debug.WriteLine($"Excepcion en sincronizacion forzada: {syncEx}");
                }
            }
            
            // Final validation
            if (Empresas.Count == 0)
            {
                ErrorMessage = "No se encontraron empresas disponibles. Verifique la configuracion de la API.";
                System.Diagnostics.Debug.WriteLine("No hay empresas disponibles despues de todos los intentos");
            }
            else
            {
                ErrorMessage = string.Empty; // Clear any previous error
                System.Diagnostics.Debug.WriteLine($"Total empresas disponibles para login: {Empresas.Count}");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al cargar empresas: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Error general en LoadEmpresasAsync: {ex}");
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Usuario) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Por favor ingrese usuario y contrasena";
            return;
        }

        SetBusy(true);
        ErrorMessage = string.Empty;

        try
        {
            // Verificar si es el usuario administrador del sistema
            if (AdminUserService.IsAdminUser(Usuario, Password))
            {
                // Usuario administrador - no requiere empresa
                var adminUser = AdminUserService.CreateAdminUser();
                
                // Crear sesion especial para admin
                var adminSession = new Session  
                {
                    UserId = adminUser.Id,
                    Username = adminUser.Username,
                    NombreUsuario = adminUser.NombreUsuario,
                    NombreCompleto = adminUser.NombreCompleto,
                    IdEmpresa = 0,
                    Token = "ADMIN_SESSION_TOKEN",
                    CreatedAt = DateTime.Now,
                    ExpiresAt = DateTime.Now.AddDays(1),
                    ExpirationDate = DateTime.Now.AddDays(1),
                    IsActive = true,
                    TipoUsuario = 0,
                    IdInspector = 0,
                    IsPromotora = false
                };

                await _sessionService.SaveSessionAsync(adminSession);
                
                // Navegar a la pantalla de configuracion de administrador
                await Shell.Current.GoToAsync("//adminconfig");
                return;
            }

            // Usuario normal - requiere empresa
            if (SelectedEmpresa == null)
            {
                ErrorMessage = "Por favor seleccione una empresa";
                return;
            }

            var loginRequest = new LoginRequest
            {
                Usuario = Usuario,
                Password = Password,
                IdEmpresa = SelectedEmpresa.Id,
                IdApp = 1
            };

            var response = await _apiService.LoginAsync(loginRequest);

            if (response.Success && !string.IsNullOrEmpty(response.Token))
            {
                System.Diagnostics.Debug.WriteLine(">>> LOGIN EXITOSO. Token recibido. Preparando para navegar...");

                _apiService.SetAuthToken(response.Token);

                if (response.Session != null)
                {
                    // Guardar sesi�n (autom�ticamente guarda token en configuraci�n)
                    await _sessionService.SaveSessionAsync(response.Session);
                    System.Diagnostics.Debug.WriteLine(">>> Sesi�n de usuario guardada correctamente con token en configuraci�n.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(">>> ADVERTENCIA: La respuesta de la API no incluyo un objeto de sesion.");
                }

                System.Diagnostics.Debug.WriteLine(">>> Intentando navegar a la ruta '//sync' en el hilo principal...");

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await Shell.Current.GoToAsync("//sync");
                        System.Diagnostics.Debug.WriteLine(">>> COMANDO DE NAVEGACION A '//sync' EJECUTADO SIN ERRORES.");
                    }
                    catch (Exception navEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"xxx ERROR DURANTE LA NAVEGACION!!: {navEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"xxx StackTrace: {navEx.StackTrace}");
                    }
                });
            }
            else
            {
                await Shell.Current.DisplayAlert("Error de Autentificacion", "Por favor revise sus datos", "OK");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task ShowEmpresaStatusAsync()
    {
        try
        {
            var stats = await AppConfigService.GetEmpresaStatsAsync();
            
            if (stats.HasEmpresas)
            {
                var message = $"Estado de Empresas:\n\n" +
                             $"Total: {stats.TotalCount}\n" +
                             $"Promotoras: {stats.PromororasCount}\n" +
                             $"No Promotoras: {stats.NonPromororasCount}\n" +
                             $"Rango IDs: {stats.FirstEmpresaId} - {stats.LastEmpresaId}";
                
                await Shell.Current.DisplayAlert("Estado de Empresas", message, "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert("Sin Empresas", 
                    "No hay empresas en la base de datos.\n\nSe intentara sincronizar automaticamente.", "OK");
                
                // Force reload empresas
                await LoadEmpresasAsync();
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error consultando estado: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task ForceSyncEmpresasAsync()
    {
        if (IsBusy) return;
        
        SetBusy(true);
        ErrorMessage = "Sincronizando empresas desde la API...";
        
        try
        {
            var syncResult = await AppConfigService.ForceEmpresasSyncAsync(_apiService);
            
            if (syncResult.Success)
            {
                // Reload empresas list
                await LoadEmpresasAsync();
                
                ErrorMessage = string.Empty;
                await Shell.Current.DisplayAlert("Exito", 
                    $"Se sincronizaron {syncResult.EmpresasCount} empresas correctamente", "OK");
            }
            else
            {
                ErrorMessage = $"Error sincronizando: {syncResult.ErrorMessage}";
                await Shell.Current.DisplayAlert("Error", 
                    $"No se pudieron sincronizar las empresas:\n{syncResult.ErrorMessage}", "OK");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
            await Shell.Current.DisplayAlert("Error", $"Error en sincronizacion: {ex.Message}", "OK");
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task RefreshUrlConfigurationAsync()
    {
        await ValidateUrlConfigurationAsync();
        
        if (!ShowUrlConfigWarning)
        {
            // Si ya no hay advertencia, recargar empresas
            await LoadEmpresasAsync();
        }
    }

    [RelayCommand]
    private async Task EntrarConfiguracionAsync()
    {
        try
        {
            SetBusy(true);
            ErrorMessage = string.Empty; // Limpiar cualquier mensaje de error previo
            
            System.Diagnostics.Debug.WriteLine("?? Iniciando acceso directo a configuraci�n...");
            
            // Crear usuario administrador autom�ticamente
            var adminUser = AdminUserService.CreateAdminUser();
            
            // Crear sesi�n especial para admin
            var adminSession = new Session  
            {
                UserId = adminUser.Id,
                Username = adminUser.Username,
                NombreUsuario = adminUser.NombreUsuario,
                NombreCompleto = adminUser.NombreCompleto,
                IdEmpresa = 0,
                Token = "ADMIN_SESSION_TOKEN",
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddDays(1),
                ExpirationDate = DateTime.Now.AddDays(1),
                IsActive = true,
                TipoUsuario = 0,
                IdInspector = 0,
                IsPromotora = false
            };

            await _sessionService.SaveSessionAsync(adminSession);
            
            System.Diagnostics.Debug.WriteLine("?? Sesi�n de administrador creada exitosamente");
            
            // Navegar a la pantalla de configuraci�n de administrador
            await Shell.Current.GoToAsync("//adminconfig");
            
            System.Diagnostics.Debug.WriteLine("?? Navegaci�n a AdminConfig completada");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error accediendo a configuraci�n: {ex.Message}");
            ErrorMessage = $"Error al acceder a configuraci�n: {ex.Message}";
            await Shell.Current.DisplayAlert("Error", $"Error al acceder a configuraci�n: {ex.Message}", "OK");
        }
        finally
        {
            SetBusy(false);
        }
    }
    
    partial void OnUsuarioChanged(string value)
    {
        // Ocultar requisito de empresa si es usuario admin
        IsEmpresaRequired = !AdminUserService.IsAdminUser(value, Password);
    }

    partial void OnPasswordChanged(string value)
    {
        // Ocultar requisito de empresa si es usuario admin
        IsEmpresaRequired = !AdminUserService.IsAdminUser(Usuario, value);
    }

    /// <summary>
    /// Valida si hay una configuraci�n de URL v�lida en la base de datos
    /// </summary>
    private async Task ValidateUrlConfigurationAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== Validando configuraci�n de URL ===");

            // Verificar si existe configuraci�n en la base de datos
            var todasConfiguraciones = await _databaseService.GetAllAsync<Configuracion>();
            var configuracionesValidas = todasConfiguraciones?.Where(c => !string.IsNullOrWhiteSpace(c.Ruta)).ToList() ?? new List<Configuracion>();
            
            // Verificar si la URL actual es v�lida (no vac�a)
            var urlActual = AppConfigService.ApiBaseUrl;
            bool urlValidaEnBD = configuracionesValidas.Any();
            bool urlActualValida = !string.IsNullOrWhiteSpace(urlActual) && urlActual != AppConfigService.FallbackUrl;
            
            System.Diagnostics.Debug.WriteLine($"Configuraciones v�lidas en BD: {configuracionesValidas.Count}");
            System.Diagnostics.Debug.WriteLine($"URL actual: '{urlActual}'");
            System.Diagnostics.Debug.WriteLine($"URL v�lida en BD: {urlValidaEnBD}, URL actual v�lida: {urlActualValida}");
            
            if (!urlValidaEnBD || !urlActualValida)
            {
                // No hay configuraci�n v�lida
                ShowUrlConfigWarning = true;
                UrlConfigMessage = "No se encontr� un URL en la configuraci�n, favor de entrar como administrador y configurar aplicaci�n";
                
                System.Diagnostics.Debug.WriteLine("?? No se encontr� configuraci�n de URL v�lida");
                return;
            }

            // Hay configuraci�n v�lida
            ShowUrlConfigWarning = false;
            UrlConfigMessage = string.Empty;
            System.Diagnostics.Debug.WriteLine($"? Configuraci�n de URL v�lida encontrada: {urlActual}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error validando configuraci�n de URL: {ex.Message}");
            
            // En caso de error, mostrar aviso por seguridad
            ShowUrlConfigWarning = true;
            UrlConfigMessage = "No se encontr� un URL en la configuraci�n, favor de entrar como administrador y configurar aplicaci�n";
        }
    }

    [RelayCommand]
    private async Task ShowLastSessionInfoAsync()
    {
        try
        {
            SetBusy(true);
            System.Diagnostics.Debug.WriteLine("?? === CONSULTANDO INFORMACI�N DE LA �LTIMA SESI�N ===");
            
            // Obtener todas las sesiones de la base de datos
            var sessions = await _databaseService.GetAllAsync<Session>();
            
            if (sessions == null || !sessions.Any())
            {
                await Shell.Current.DisplayAlert("Sin Sesiones", 
                    "No hay sesiones guardadas en la base de datos.", "OK");
                return;
            }
            
            // Obtener la sesi�n m�s reciente (por CreatedAt)
            var lastSession = sessions.OrderByDescending(s => s.CreatedAt).FirstOrDefault();
            
            if (lastSession == null)
            {
                await Shell.Current.DisplayAlert("Error", 
                    "No se pudo obtener la �ltima sesi�n.", "OK");
                return;
            }
            
            // Construir el mensaje con la informaci�n de la sesi�n
            var sessionInfo = $"?? INFORMACI�N DE LA �LTIMA SESI�N\n\n" +
                             $"?? ID: {lastSession.Id}\n" +
                             $"?? Usuario: {lastSession.Username}\n" +
                             $"?? Nombre: {lastSession.NombreCompleto}\n" +
                             $"?? Empresa ID: {lastSession.IdEmpresa}\n" +
                             $"?? Token: {(string.IsNullOrEmpty(lastSession.Token) ? "? NO PRESENTE" : "? PRESENTE")}\n";
                             
            if (!string.IsNullOrEmpty(lastSession.Token))
            {
                sessionInfo += $"?? Token Length: {lastSession.Token.Length} caracteres\n";
                sessionInfo += $"?? Token Preview: {lastSession.Token.Substring(0, Math.Min(20, lastSession.Token.Length))}...\n";
            }
            
            sessionInfo += $"? Creada: {lastSession.CreatedAt:dd/MM/yyyy HH:mm:ss}\n" +
                          $"? Expira: {lastSession.ExpirationDate:dd/MM/yyyy HH:mm:ss}\n" +
                          $"?? Activa: {(lastSession.IsActive ? "? S�" : "? NO")}\n" +
                          $"?? Online: {(lastSession.IsOnline ? "? S�" : "? NO")}\n" +
                          $"?? Tipo Usuario: {lastSession.TipoUsuario}\n" +
                          $"?? Inspector ID: {lastSession.IdInspector}\n" +
                          $"?? Es Promotora: {(lastSession.IsPromotora ? "? S�" : "? NO")}\n\n";
            
            // Verificar validez
            var timeRemaining = lastSession.ExpirationDate - DateTime.Now;
            bool isExpired = lastSession.ExpirationDate <= DateTime.Now;
            bool hasToken = !string.IsNullOrEmpty(lastSession.Token);
            bool isValid = hasToken && !isExpired && lastSession.IsActive;
            
            sessionInfo += $"?? Tiempo restante: {(isExpired ? "?? EXPIRADA" : $"{timeRemaining.TotalMinutes:F1} minutos")}\n";
            sessionInfo += $"? ESTADO: {(isValid ? "?? V�LIDA - PUEDE IR AL HOME" : "?? INV�LIDA - DEBE IR AL LOGIN")}\n\n";
            
            if (!isValid)
            {
                sessionInfo += "? RAZONES DE INVALIDEZ:\n";
                if (!hasToken)
                    sessionInfo += "   � Token vac�o o ausente\n";
                if (isExpired)
                    sessionInfo += "   � Sesi�n expirada\n";
                if (!lastSession.IsActive)
                    sessionInfo += "   � Sesi�n marcada como inactiva\n";
            }
            
            // Mostrar informaci�n adicional
            sessionInfo += $"\n?? ESTAD�STICAS:\n" +
                          $"   � Total de sesiones en BD: {sessions.Count}\n" +
                          $"   � Sesiones activas: {sessions.Count(s => s.IsActive)}\n" +
                          $"   � Sesiones con token: {sessions.Count(s => !string.IsNullOrEmpty(s.Token))}\n" +
                          $"   � Sesiones v�lidas: {sessions.Count(s => !string.IsNullOrEmpty(s.Token) && s.ExpirationDate > DateTime.Now && s.IsActive)}";
            
            await Shell.Current.DisplayAlert("Informaci�n de Sesi�n", sessionInfo, "OK");
            
            System.Diagnostics.Debug.WriteLine($"?? Informaci�n de �ltima sesi�n mostrada - ID: {lastSession.Id}, Usuario: {lastSession.Username}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"?? ? Error obteniendo informaci�n de sesi�n: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", 
                $"Error al obtener informaci�n de la sesi�n:\n{ex.Message}", "OK");
        }
        finally
        {
            SetBusy(false);
        }
    }
}