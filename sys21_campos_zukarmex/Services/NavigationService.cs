using sys21_campos_zukarmex.Models;
using System.Text;

namespace sys21_campos_zukarmex.Services;

/// <summary>
/// Servicio centralizado para manejo de navegaci�n din�mica basada en sesiones
/// </summary>
public class NavigationService
{
    private readonly SessionService _sessionService;
    private readonly DatabaseService _databaseService;
    private static bool _isNavigating = false;
    private static bool _isInitialized = false;

    public NavigationService(SessionService sessionService, DatabaseService databaseService)
    {
        _sessionService = sessionService;
        _databaseService = databaseService;
    }

    /// <summary>
    /// Determina la ruta inicial basada en el estado de la sesi�n
    /// </summary>
    public async Task<string> DetermineInitialRouteAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("?? === DETERMINANDO RUTA INICIAL ===");
            
            // Verificar si la base de datos est� inicializada
            await _databaseService.InitializeAsync();
            
            // Cargar la sesi�n m�s reciente
            var session = await _sessionService.LoadMostRecentSessionAsync();
            
            if (session == null)
            {
                System.Diagnostics.Debug.WriteLine("?? No hay sesiones ? LOGIN");
                return "//login";
            }
            
            // Verificar validez de la sesi�n
            bool isValid = await IsSessionValidForNavigationAsync(session);
            
            if (isValid)
            {
                System.Diagnostics.Debug.WriteLine("?? Sesi�n v�lida ? HOME");
                return "//home";
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("?? Sesi�n inv�lida ? LOGIN");
                // NO limpiar autom�ticamente la sesi�n aqu� - puede estar interfiriendo
                // await _sessionService.ClearSessionAsync();
                return "//login";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"?? ? Error determinando ruta: {ex.Message}");
            return "//login"; // Fallback seguro
        }
    }

    /// <summary>
    /// Navega a la ruta determinada de forma segura
    /// </summary>
    public async Task NavigateToInitialRouteAsync()
    {
        try
        {
            if (_isNavigating)
            {
                System.Diagnostics.Debug.WriteLine("?? ?? Navegaci�n ya en progreso - cancelando nueva navegaci�n");
                return;
            }

            if (_isInitialized)
            {
                System.Diagnostics.Debug.WriteLine("?? ?? Navegaci�n inicial ya completada - cancelando nueva navegaci�n");
                return;
            }

            _isNavigating = true;
            System.Diagnostics.Debug.WriteLine("?? ?? BLOQUEANDO navegaci�n - iniciando proceso");
            
            var route = await DetermineInitialRouteAsync();
            
            System.Diagnostics.Debug.WriteLine($"?? ?? Navegando a: {route}");
            
            // Agregar peque�o delay antes de navegar para evitar conflictos
            await Task.Delay(50);
            
            await Shell.Current.GoToAsync(route);
            
            _isInitialized = true;
            System.Diagnostics.Debug.WriteLine("?? ? Navegaci�n inicial completada - marcando como inicializada");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"?? ? Error en navegaci�n: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"?? ? StackTrace: {ex.StackTrace}");
            
            // Fallback absoluto
            try
            {
                await Shell.Current.GoToAsync("//login");
                System.Diagnostics.Debug.WriteLine("?? ? Fallback a login exitoso");
            }
            catch (Exception fallbackEx)
            {
                System.Diagnostics.Debug.WriteLine($"?? ?? Error en fallback: {fallbackEx.Message}");
            }
        }
        finally
        {
            _isNavigating = false;
            System.Diagnostics.Debug.WriteLine("?? ?? DESBLOQUEANDO navegaci�n - proceso completado");
        }
    }

    /// <summary>
    /// Verifica si debe mantenerse en la p�gina actual o navegar
    /// </summary>
    public async Task<bool> ShouldStayOnCurrentPageAsync()
    {
        try
        {
            var session = await _sessionService.GetCurrentSessionAsync();
            
            if (session == null)
            {
                System.Diagnostics.Debug.WriteLine("?? No hay sesi�n ? debe salir");
                return false;
            }
            
            return await IsSessionValidForNavigationAsync(session);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"?? ? Error verificando permanencia: {ex.Message}");
            return false; // Por seguridad, salir de la p�gina
        }
    }

    /// <summary>
    /// Verifica si una sesi�n es v�lida para navegaci�n (m�s permisiva que las verificaciones estrictas)
    /// </summary>
    private async Task<bool> IsSessionValidForNavigationAsync(Session session)
    {
        try
        {
            // Criterios b�sicos para navegaci�n
            bool hasToken = !string.IsNullOrEmpty(session.Token);
            bool isActive = session.IsActive;
            bool notExpired = session.ExpirationDate > DateTime.Now;
            
            // Log detallado para debugging
            System.Diagnostics.Debug.WriteLine($"?? Validaci�n de sesi�n ID {session.Id}:");
            System.Diagnostics.Debug.WriteLine($"   - Token presente: {hasToken}");
            System.Diagnostics.Debug.WriteLine($"   - Est� activa: {isActive}");
            System.Diagnostics.Debug.WriteLine($"   - No expirada: {notExpired}");
            
            if (notExpired)
            {
                var timeRemaining = session.ExpirationDate - DateTime.Now;
                System.Diagnostics.Debug.WriteLine($"   - Tiempo restante: {timeRemaining.TotalMinutes:F1} min");
            }
            
            bool isValid = hasToken && isActive && notExpired;
            System.Diagnostics.Debug.WriteLine($"?? Resultado: {(isValid ? "? V�LIDA" : "? INV�LIDA")}");
            
            return isValid;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"?? ? Error validando sesi�n: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Navega de forma segura con validaci�n de sesi�n
    /// </summary>
    public async Task NavigateWithSessionValidationAsync(string route)
    {
        try
        {
            // Verificar sesi�n antes de navegar
            var shouldProceed = await ShouldStayOnCurrentPageAsync();
            
            if (!shouldProceed)
            {
                System.Diagnostics.Debug.WriteLine("?? Sesi�n inv�lida, redirigiendo a login");
                await _sessionService.ClearSessionAsync();
                await Shell.Current.GoToAsync("//login");
                return;
            }
            
            // Navegar normalmente
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"?? ? Error en navegaci�n segura: {ex.Message}");
            await Shell.Current.GoToAsync("//login");
        }
    }

    /// <summary>
    /// Resetea el estado de navegaci�n (para testing)
    /// </summary>
    public static void ResetNavigationState()
    {
        _isNavigating = false;
        _isInitialized = false;
        System.Diagnostics.Debug.WriteLine("?? Estado de navegaci�n reseteado");
    }

    /// <summary>
    /// Verifica si la navegaci�n ya fue inicializada
    /// </summary>
    public static bool IsInitialized => _isInitialized;

    /// <summary>
    /// Genera un diagn�stico completo del estado de navegaci�n
    /// </summary>
    public async Task<string> GetNavigationDiagnosticAsync()
    {
        try
        {
            var diagnostic = new StringBuilder();
            diagnostic.AppendLine("?? === DIAGN�STICO COMPLETO DE NAVEGACI�N ===");
            diagnostic.AppendLine($"?? Fecha: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            diagnostic.AppendLine($"?? Navegaci�n inicializada: {IsInitialized}");
            diagnostic.AppendLine($"?? Navegaci�n en progreso: {_isNavigating}");
            diagnostic.AppendLine();
            
            // Verificar estado de la base de datos
            try
            {
                await _databaseService.InitializeAsync();
                diagnostic.AppendLine("? Base de datos: INICIALIZADA");
            }
            catch (Exception ex)
            {
                diagnostic.AppendLine($"? Base de datos: ERROR - {ex.Message}");
            }
            
            // Verificar sesiones
            var sessions = await _databaseService.GetAllAsync<Session>();
            diagnostic.AppendLine($"?? Total sesiones en BD: {sessions?.Count ?? 0}");
            
            if (sessions != null && sessions.Any())
            {
                var mostRecent = sessions.OrderByDescending(s => s.CreatedAt).FirstOrDefault();
                if (mostRecent != null)
                {
                    diagnostic.AppendLine($"?? Sesi�n m�s reciente:");
                    diagnostic.AppendLine($"   - ID: {mostRecent.Id}");
                    diagnostic.AppendLine($"   - Usuario: {mostRecent.Username}");
                    diagnostic.AppendLine($"   - Creada: {mostRecent.CreatedAt:dd/MM/yyyy HH:mm:ss}");
                    diagnostic.AppendLine($"   - Expira: {mostRecent.ExpirationDate:dd/MM/yyyy HH:mm:ss}");
                    diagnostic.AppendLine($"   - Activa: {mostRecent.IsActive}");
                    diagnostic.AppendLine($"   - Token presente: {!string.IsNullOrEmpty(mostRecent.Token)}");
                    
                    bool isValid = await IsSessionValidForNavigationAsync(mostRecent);
                    diagnostic.AppendLine($"   - V�lida para navegaci�n: {isValid}");
                    
                    var route = await DetermineInitialRouteAsync();
                    diagnostic.AppendLine($"?? Ruta determinada: {route}");
                }
            }
            else
            {
                diagnostic.AppendLine("? No hay sesiones en la base de datos");
                diagnostic.AppendLine("?? Ruta determinada: //login (sin sesiones)");
            }
            
            // Informaci�n de la sesi�n actual
            try
            {
                var currentSession = await _sessionService.GetCurrentSessionAsync();
                if (currentSession != null)
                {
                    diagnostic.AppendLine();
                    diagnostic.AppendLine("?? Sesi�n actual en memoria:");
                    diagnostic.AppendLine($"   - ID: {currentSession.Id}");
                    diagnostic.AppendLine($"   - Usuario: {currentSession.Username}");
                    diagnostic.AppendLine($"   - V�lida: {await IsSessionValidForNavigationAsync(currentSession)}");
                }
                else
                {
                    diagnostic.AppendLine();
                    diagnostic.AppendLine("? No hay sesi�n actual en memoria");
                }
            }
            catch (Exception ex)
            {
                diagnostic.AppendLine();
                diagnostic.AppendLine($"? Error obteniendo sesi�n actual: {ex.Message}");
            }
            
            diagnostic.AppendLine();
            diagnostic.AppendLine("?? === FIN DIAGN�STICO ===");
            
            return diagnostic.ToString();
        }
        catch (Exception ex)
        {
            return $"? Error generando diagn�stico: {ex.Message}";
        }
    }

    /// <summary>
    /// M�todo de emergencia para forzar navegaci�n al home si hay una sesi�n v�lida
    /// SOLO PARA DEBUGGING - fuerza ir al home sin importar otras verificaciones
    /// </summary>
    public async Task ForceNavigateToHomeIfValidSessionAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("?? === MODO EMERGENCIA: FORZANDO NAVEGACI�N AL HOME ===");
            
            // Verificar si hay alguna sesi�n con token
            var sessions = await _databaseService.GetAllAsync<Session>();
            
            if (sessions == null || !sessions.Any())
            {
                System.Diagnostics.Debug.WriteLine("?? No hay sesiones - ir a login");
                await Shell.Current.GoToAsync("//login");
                return;
            }
            
            var sessionWithToken = sessions
                .Where(s => !string.IsNullOrEmpty(s.Token))
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefault();
            
            if (sessionWithToken != null)
            {
                System.Diagnostics.Debug.WriteLine($"?? SESI�N CON TOKEN ENCONTRADA - FORZANDO A HOME:");
                System.Diagnostics.Debug.WriteLine($"   - ID: {sessionWithToken.Id}");
                System.Diagnostics.Debug.WriteLine($"   - Usuario: {sessionWithToken.Username}");
                System.Diagnostics.Debug.WriteLine($"   - Token: PRESENTE ({sessionWithToken.Token.Length} chars)");
                System.Diagnostics.Debug.WriteLine($"   - Creada: {sessionWithToken.CreatedAt}");
                System.Diagnostics.Debug.WriteLine($"   - Expira: {sessionWithToken.ExpirationDate}");
                System.Diagnostics.Debug.WriteLine($"   - IsActive: {sessionWithToken.IsActive}");
                
                // FORZAR navegaci�n al home sin m�s verificaciones
                await Shell.Current.GoToAsync("//home");
                System.Diagnostics.Debug.WriteLine("?? ? NAVEGACI�N FORZADA AL HOME COMPLETADA");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("?? No hay sesiones con token - ir a login");
                await Shell.Current.GoToAsync("//login");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"?? ? Error en navegaci�n forzada: {ex.Message}");
            await Shell.Current.GoToAsync("//login");
        }
    }

    /// <summary>
    /// M�todo simple y directo para navegaci�n inicial - SIN verificaciones complejas
    /// </summary>
    public async Task SimpleNavigationAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("?? === NAVEGACI�N SIMPLE Y DIRECTA ===");
            
            // Verificar base de datos
            await _databaseService.InitializeAsync();
            
            // Obtener sesiones
            var sessions = await _databaseService.GetAllAsync<Session>();
            
            if (sessions == null || !sessions.Any())
            {
                System.Diagnostics.Debug.WriteLine("?? Sin sesiones ? LOGIN");
                await Shell.Current.GoToAsync("//login");
                return;
            }
            
            // Buscar sesi�n m�s reciente con token
            var recentSessionWithToken = sessions
                .Where(s => !string.IsNullOrEmpty(s.Token))
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefault();
            
            if (recentSessionWithToken != null)
            {
                System.Diagnostics.Debug.WriteLine($"?? Sesi�n con token encontrada: {recentSessionWithToken.Username}");
                System.Diagnostics.Debug.WriteLine($"?? Token length: {recentSessionWithToken.Token.Length}");
                System.Diagnostics.Debug.WriteLine($"?? Expira: {recentSessionWithToken.ExpirationDate}");
                System.Diagnostics.Debug.WriteLine($"?? Expirada: {recentSessionWithToken.ExpirationDate <= DateTime.Now}");
                
                // Ir al home SIN importar si est� expirada (para test)
                System.Diagnostics.Debug.WriteLine("?? ? NAVEGANDO A HOME");
                await Shell.Current.GoToAsync("//home");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("?? Sin sesiones con token ? LOGIN");
                await Shell.Current.GoToAsync("//login");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"?? ? Error: {ex.Message}");
            await Shell.Current.GoToAsync("//login");
        }
    }
}