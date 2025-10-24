using Newtonsoft.Json;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Models.DTOs.Authentication;
using sys21_campos_zukarmex.Services;
using System.Text;
using static sys21_campos_zukarmex.Models.DTOs.Authentication.UserData;

public class SessionService
{
    private readonly DatabaseService _databaseService;
    private Session? _currentSession;

    public SessionService(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<bool> IsLoggedInAsync()
    {
        // Primero verificar sesi�n activa
        var session = await GetCurrentSessionAsync();
        if (session != null && session.ExpirationDate > DateTime.Now)
        {
            return true;
        }

        // Si no hay sesi�n activa, verificar token en configuraci�n
        return await HasValidTokenInConfigurationAsync();
    }

    private readonly Dictionary<string, string> _routePermissionMap = new Dictionary<string, string>
        {
            // App de Campo
            { "home", "App de Campo" },

            // Uso de Maquinaria
            { "machineryusage", "Uso de Maquinaria" },
            { "machineryusagepending", "Uso de Maquinaria" },
            { "machineryusagehistory", "Uso de Maquinaria" },

            // Precipitación Pluvial
            { "rainfall", "Precipitación Pluvial" },
            { "rainfallpending", "Precipitación Pluvial" },
            { "rainfallhistory", "Precipitación Pluvial" },

            // Trampeo de Rata
            { "rattrapping", "Trampeo de Rata" },
            { "rattrappingpending", "Trampeo de Rata" },
            { "rattrappinghistory", "Trampeo de Rata" },

            // Consumo de Rodenticida
            { "rodenticideconsumption", "Consumo de Rodenticida" },
            { "rodenticidepending", "Consumo de Rodenticida" },
            { "rodenticidehistory", "Consumo de Rodenticida" },

            // Muestreo de Daño
            { "damageassessment", "Muestreo de Daño" },
            { "damageassessmentpending", "Muestreo de Daño" },
            { "damageassessmenthistory", "Muestreo de Daño" },

            // Captura de Linea de Riego
            { "irrigationline", "Captura de Linea de Riego" },
            { "irrigationlinepending", "Captura de Linea de Riego" },
            { "irrigationlinehistory", "Captura de Linea de Riego" }
        };

    public async Task<bool> CheckPermissionForRouteAsync(string route)
    {
        try
        {
            // Limpiamos la ruta (ej. "//home" -> "home")
            var cleanRoute = route.Replace("//", "").ToLower();

            var session = await GetCurrentSessionAsync();

            // El admin local (Token especial) o un usuario sin sesión (aún no logueado) siempre deben pasar
            if (session == null)
            {
                return true;
            }

            // Si no hay permisos guardados, denegar acceso por seguridad.
            if (string.IsNullOrWhiteSpace(session.PermisosJson))
            {
                return false;
            }

            // Si la ruta no está en nuestro mapa, la dejamos pasar (ej. Pendientes, Historial, Sync, etc.)
            if (!_routePermissionMap.TryGetValue(cleanRoute, out var appName))
            {
                return true;
            }

            // Deserializa la lista de permisos
            var permisos = JsonConvert.DeserializeObject<List<Permiso>>(session.PermisosJson);
            if (permisos == null) return false;

            // Busca el permiso específico
            var permiso = permisos.FirstOrDefault(p => p.NombreApp.Equals(appName, StringComparison.OrdinalIgnoreCase));

            // Devuelve 'true' si el permiso se encontró y TienePermiso es 'true'
            return permiso?.TienePermiso ?? false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al chequear permisos: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Verifica si hay un token v�lido considerando tanto Session como Configuraci�n
    /// Prioridad: 1. Session activa, 2. Token en Configuraci�n
    /// </summary>
    public async Task<bool> HasValidTokenInConfigurationAsync()
    {
        try
        {
            // Primero verificar si hay una sesi�n activa con token v�lido
            var currentSession = await GetCurrentSessionAsync();
            if (currentSession != null && !string.IsNullOrEmpty(currentSession.Token) && currentSession.ExpirationDate > DateTime.Now)
            {
                System.Diagnostics.Debug.WriteLine($"?? Token v�lido encontrado en Session - Exp: {currentSession.ExpirationDate}");
                return true;
            }

            // Si no hay sesi�n activa, verificar token en configuraci�n
            var configuraciones = await _databaseService.GetAllAsync<Configuracion>();
            var configActiva = configuraciones.OrderByDescending(c => c.Fecha).FirstOrDefault();
            
            if (configActiva != null && configActiva.HasValidToken)
            {
                System.Diagnostics.Debug.WriteLine($"?? Token v�lido encontrado en Configuraci�n - Exp: {configActiva.TokenExpiration}");
                return true;
            }
            
            System.Diagnostics.Debug.WriteLine("? No hay token v�lido en Session ni en Configuraci�n");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error verificando token: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Obtiene el token v�lido considerando tanto Session como Configuraci�n
    /// Prioridad: 1. Session activa, 2. Token en Configuraci�n
    /// </summary>
    public async Task<string?> GetValidTokenFromConfigurationAsync()
    {
        try
        {
            // Primero verificar si hay una sesi�n activa con token v�lido
            var currentSession = await GetCurrentSessionAsync();
            if (currentSession != null && !string.IsNullOrEmpty(currentSession.Token) && currentSession.ExpirationDate > DateTime.Now)
            {
                System.Diagnostics.Debug.WriteLine($"?? Usando token de Session activa");
                return currentSession.Token;
            }

            // Si no hay sesi�n activa, verificar token en configuraci�n
            var configuraciones = await _databaseService.GetAllAsync<Configuracion>();
            var configActiva = configuraciones.OrderByDescending(c => c.Fecha).FirstOrDefault();
            
            if (configActiva != null && configActiva.HasValidToken)
            {
                System.Diagnostics.Debug.WriteLine($"?? Usando token de Configuraci�n");
                return configActiva.Token;
            }
            
            System.Diagnostics.Debug.WriteLine("? No hay token v�lido disponible");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error obteniendo token: {ex.Message}");
            return null;
        }
    }

    public async Task<Session?> GetCurrentSessionAsync()
    {
        // Si ya tenemos una sesi�n en memoria y sigue siendo v�lida, usarla
        if (_currentSession != null && _currentSession.ExpirationDate > DateTime.Now && _currentSession.IsActive)
            return _currentSession;

        // Buscar la sesi�n m�s reciente en la base de datos
        var sessions = await _databaseService.GetAllAsync<Session>();
        
        if (sessions == null || !sessions.Any())
        {
            _currentSession = null;
            return null;
        }

        // Obtener la sesi�n m�s nueva (por CreatedAt) que est� activa y no expirada
        _currentSession = sessions
            .Where(s => s.IsActive && s.ExpirationDate > DateTime.Now && !string.IsNullOrEmpty(s.Token))
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefault();
        
        return _currentSession;
    }

    public async Task SaveSessionAsync(Session newSession)
    {
        var previousUserName = Preferences.Get("LastUser", string.Empty);
        var previousEmpresaId = Preferences.Get("LastEmpresaId", 0);

        if (string.IsNullOrEmpty(previousUserName) || previousUserName != newSession.Username || previousEmpresaId != newSession.IdEmpresa)
        {
            await _databaseService.ClearUserTransactionDataAsync();
        }

        Preferences.Set("LastUser", newSession.Username);
        Preferences.Set("LastEmpresaId", newSession.IdEmpresa);

        // Asegurar que CreatedAt est� configurado correctamente
        if (newSession.CreatedAt == default(DateTime))
        {
            newSession.CreatedAt = DateTime.Now;
        }

        // Asegurar que IsActive est� en true
        newSession.IsActive = true;

        System.Diagnostics.Debug.WriteLine($"?? Guardando nueva sesi�n:");
        System.Diagnostics.Debug.WriteLine($"   - Usuario: {newSession.Username} ({newSession.NombreCompleto})");
        System.Diagnostics.Debug.WriteLine($"   - Empresa: {newSession.IdEmpresa}");
        System.Diagnostics.Debug.WriteLine($"   - CreatedAt: {newSession.CreatedAt}");
        System.Diagnostics.Debug.WriteLine($"   - ExpirationDate: {newSession.ExpirationDate}");
        System.Diagnostics.Debug.WriteLine($"   - IsActive: {newSession.IsActive}");
        System.Diagnostics.Debug.WriteLine($"   - Token presente: {(!string.IsNullOrEmpty(newSession.Token) ? "S�" : "NO")}");

        await _databaseService.ClearTableAsync<Session>();
        await _databaseService.SaveAsync(newSession);
        _currentSession = newSession;

        System.Diagnostics.Debug.WriteLine("?? ? Sesi�n guardada exitosamente en la base de datos");

        // Guardar tambi�n el token en la configuraci�n
        await SaveTokenToConfigurationAsync(newSession.Token, newSession.ExpirationDate);
    }

    /// <summary>
    /// Guarda el token en la configuraci�n activa
    /// </summary>
    private async Task SaveTokenToConfigurationAsync(string token, DateTime expiration)
    {
        try
        {
            var configuraciones = await _databaseService.GetAllAsync<Configuracion>();
            var configActiva = configuraciones.OrderByDescending(c => c.Fecha).FirstOrDefault();
            
            if (configActiva != null)
            {
                configActiva.Token = token;
                configActiva.TokenExpiration = expiration;
                await _databaseService.SaveAsync(configActiva);
                
                System.Diagnostics.Debug.WriteLine($"?? Token guardado en configuraci�n - Exp: {expiration}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("?? No se encontr� configuraci�n activa para guardar token");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error guardando token en configuraci�n: {ex.Message}");
        }
    }

    public async Task ClearSessionAsync()
    {
        await _databaseService.ClearTableAsync<Session>();
        _currentSession = null;

        // Limpiar tambi�n el token de la configuraci�n
        await ClearTokenFromConfigurationAsync();
    }

    /// <summary>
    /// Limpia el token de la configuraci�n activa
    /// </summary>
    private async Task ClearTokenFromConfigurationAsync()
    {
        try
        {
            var configuraciones = await _databaseService.GetAllAsync<Configuracion>();
            var configActiva = configuraciones.OrderByDescending(c => c.Fecha).FirstOrDefault();
            
            if (configActiva != null)
            {
                configActiva.Token = string.Empty;
                configActiva.TokenExpiration = null;
                await _databaseService.SaveAsync(configActiva);
                
                System.Diagnostics.Debug.WriteLine("?? Token limpiado de configuraci�n");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error limpiando token de configuraci�n: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifica si el token actual es diferente al almacenado en configuraci�n
    /// Usado para detectar cambios de token en respuestas 401
    /// </summary>
    public async Task<bool> IsTokenDifferentFromStoredAsync(string currentToken)
    {
        try
        {
            var storedToken = await GetValidTokenFromConfigurationAsync();
            var isDifferent = !string.Equals(storedToken, currentToken, StringComparison.Ordinal);
            
            System.Diagnostics.Debug.WriteLine($"?? Comparando tokens - Diferentes: {isDifferent}");
            
            return isDifferent;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error comparando tokens: {ex.Message}");
            return true; // Asumir diferente en caso de error
        }
    }

    public async Task<bool> CanAuthorizeAsync()
    {
        var session = await GetCurrentSessionAsync();
        if (session == null) return false;

        return session.TipoUsuario == AppConfigService.UserTypeAdmin || 
               session.TipoUsuario == AppConfigService.UserTypeSupervisor;
    }

    public async Task<string> GetCurrentUserNameAsync()
    {
        var session = await GetCurrentSessionAsync();
        return session?.NombreCompleto ?? "Usuario";
    }

    public async Task<string> GetCurrentUserRoleAsync()
    {
        var session = await GetCurrentSessionAsync();
        if (session == null) return "Invitado";

        return session.TipoUsuario switch
        {
            AppConfigService.UserTypeAdmin => "Administrador",
            AppConfigService.UserTypeSupervisor => "Supervisor",
            AppConfigService.UserTypeRegular => "Usuario",
            _ => "Desconocido"
        };
    }

    /// <summary>
    /// M�todo de diagn�stico para verificar el estado completo de la base de datos
    /// </summary>
    public async Task<string> GetDatabaseDiagnosticAsync()
    {
        try
        {
            var diagnostic = new StringBuilder();
            diagnostic.AppendLine("=== DIAGN�STICO COMPLETO DE BASE DE DATOS ===");
            diagnostic.AppendLine($"Tiempo: {DateTime.Now}");
            diagnostic.AppendLine("?? L�GICA ACTUAL: Verificando token en tabla Session �nicamente");
            
            // Verificar si la base de datos existe
            var dbExists = await _databaseService.DatabaseExistsAsync();
            diagnostic.AppendLine($"Base de datos existe: {dbExists}");
            
            if (dbExists)
            {
                var dbSize = await _databaseService.GetDatabaseSizeAsync();
                diagnostic.AppendLine($"Tama�o de base de datos: {dbSize} bytes");
            }
            
            // === VERIFICAR SESSIONS (PRIORIDAD) ===
            diagnostic.AppendLine();
            diagnostic.AppendLine("=== AN�LISIS DE SESSIONS (L�GICA PRINCIPAL) ===");
            var allSessions = await _databaseService.GetAllAsync<Session>();
            diagnostic.AppendLine($"Total sessions en BD: {allSessions?.Count ?? 0}");
            
            if (allSessions != null && allSessions.Any())
            {
                foreach (var session in allSessions.OrderByDescending(s => s.CreatedAt))
                {
                    diagnostic.AppendLine($"  Session ID: {session.Id}");
                    diagnostic.AppendLine($"    Usuario: '{session.Username}'");
                    diagnostic.AppendLine($"    Nombre completo: '{session.NombreCompleto}'");
                    diagnostic.AppendLine($"    Empresa ID: {session.IdEmpresa}");
                    diagnostic.AppendLine($"    Token presente: {!string.IsNullOrEmpty(session.Token)}");
                    diagnostic.AppendLine($"    Token length: {session.Token?.Length ?? 0}");
                    diagnostic.AppendLine($"    ExpirationDate: {session.ExpirationDate}");
                    diagnostic.AppendLine($"    IsActive: {session.IsActive}");
                    diagnostic.AppendLine($"    Expirada: {session.ExpirationDate <= DateTime.Now}");
                    diagnostic.AppendLine($"    Tiempo restante: {(session.ExpirationDate - DateTime.Now).TotalMinutes:F2} minutos");
                    diagnostic.AppendLine($"    CreatedAt: {session.CreatedAt}");
                    
                    // Verificar si cumple todos los criterios para navegaci�n autom�tica
                    bool isValidForAutoNav = !string.IsNullOrEmpty(session.Token) && 
                                           session.ExpirationDate > DateTime.Now && 
                                           session.IsActive;
                    diagnostic.AppendLine($"    �V�lida para auto-navegaci�n?: {(isValidForAutoNav ? "S�" : "NO")}");
                    diagnostic.AppendLine();
                }
                
                // Analizar session activa para auto-navegaci�n
                var activeSession = allSessions.FirstOrDefault(s => 
                    !string.IsNullOrEmpty(s.Token) && 
                    s.ExpirationDate > DateTime.Now && 
                    s.IsActive);
                    
                if (activeSession != null)
                {
                    diagnostic.AppendLine($"*** SESSION ACTIVA PARA AUTO-NAVEGACI�N: ID {activeSession.Id} ***");
                    diagnostic.AppendLine($"    Usuario: {activeSession.Username} ({activeSession.NombreCompleto})");
                    diagnostic.AppendLine($"    Empresa: {activeSession.IdEmpresa}");
                    diagnostic.AppendLine($"    Expira: {activeSession.ExpirationDate}");
                    diagnostic.AppendLine($"    Tiempo restante: {(activeSession.ExpirationDate - DateTime.Now).TotalMinutes:F2} minutos");
                }
                else
                {
                    diagnostic.AppendLine("*** NO HAY SESSION ACTIVA V�LIDA PARA AUTO-NAVEGACI�N ***");
                }
            }
            else
            {
                diagnostic.AppendLine("*** NO HAY SESSIONS EN LA BASE DE DATOS ***");
            }
            
            // === VERIFICAR CONFIGURACIONES (INFORMACI�N) ===
            diagnostic.AppendLine();
            diagnostic.AppendLine("=== AN�LISIS DE CONFIGURACIONES (SOLO INFORMATIVO) ===");
            var allConfigs = await _databaseService.GetAllAsync<Configuracion>();
            diagnostic.AppendLine($"Total configuraciones en BD: {allConfigs?.Count ?? 0}");
            
            if (allConfigs != null && allConfigs.Any())
            {
                foreach (var config in allConfigs.OrderByDescending(c => c.Fecha))
                {
                    diagnostic.AppendLine($"  Configuraci�n ID: {config.Id}");
                    diagnostic.AppendLine($"    Dispositivo: '{config.Dispositivo}'");
                    diagnostic.AppendLine($"    Ruta: '{config.Ruta}'");
                    diagnostic.AppendLine($"    Fecha: {config.Fecha}");
                    diagnostic.AppendLine($"    Token presente: {!string.IsNullOrEmpty(config.Token)}");
                    diagnostic.AppendLine($"    Token length: {config.Token?.Length ?? 0}");
                    diagnostic.AppendLine($"    Token expiration: {config.TokenExpiration}");
                    diagnostic.AppendLine($"    HasValidToken: {config.HasValidToken}");
                    if (config.TokenExpiration.HasValue)
                    {
                        diagnostic.AppendLine($"    Expirada: {config.TokenExpiration <= DateTime.Now}");
                        diagnostic.AppendLine($"    Tiempo restante: {(config.TokenExpiration.Value - DateTime.Now).TotalMinutes:F2} minutos");
                    }
                    diagnostic.AppendLine();
                }
            }
            
            diagnostic.AppendLine("=== FIN DIAGN�STICO ===");
            return diagnostic.ToString();
        }
        catch (Exception ex)
        {
            return $"ERROR EN DIAGN�STICO: {ex.Message}";
        }
    }

    /// <summary>
    /// Obtiene un diagn�stico espec�fico del problema de navegaci�n
    /// </summary>
    public async Task<string> GetNavigationDiagnosticAsync()
    {
        try
        {
            var diagnostic = new StringBuilder();
            diagnostic.AppendLine("=== DIAGN�STICO DE NAVEGACI�N ===");
            diagnostic.AppendLine($"Tiempo: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            diagnostic.AppendLine();
            
            // Verificar sesi�n actual
            diagnostic.AppendLine("1. VERIFICACI�N DE SESI�N ACTUAL:");
            var currentSession = await GetCurrentSessionAsync();
            
            if (currentSession != null)
            {
                diagnostic.AppendLine($"   ? Sesi�n encontrada - ID: {currentSession.Id}");
                diagnostic.AppendLine($"   ?? Usuario: {currentSession.Username} ({currentSession.NombreCompleto})");
                diagnostic.AppendLine($"   ?? Empresa ID: {currentSession.IdEmpresa}");
                diagnostic.AppendLine($"   ?? Token: {(string.IsNullOrEmpty(currentSession.Token) ? "AUSENTE" : $"PRESENTE ({currentSession.Token.Length} chars)")}");
                diagnostic.AppendLine($"   ? Creada: {currentSession.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                diagnostic.AppendLine($"   ? Expira: {currentSession.ExpirationDate:yyyy-MM-dd HH:mm:ss}");
                diagnostic.AppendLine($"   ?? Tiempo restante: {(currentSession.ExpirationDate - DateTime.Now).TotalMinutes:F1} min");
                diagnostic.AppendLine($"   ?? Activa: {currentSession.IsActive}");
                diagnostic.AppendLine($"   ?? Online: {currentSession.IsOnline}");
                
                // Evaluar validez
                bool hasToken = !string.IsNullOrEmpty(currentSession.Token);
                bool notExpired = currentSession.ExpirationDate > DateTime.Now;
                bool isActive = currentSession.IsActive;
                bool isValid = hasToken && notExpired && isActive;
                
                diagnostic.AppendLine();
                diagnostic.AppendLine("   EVALUACI�N DE VALIDEZ:");
                diagnostic.AppendLine($"   - Tiene token: {(hasToken ? "? S�" : "? NO")}");
                diagnostic.AppendLine($"   - No expirada: {(notExpired ? "? S�" : "? NO")}");
                diagnostic.AppendLine($"   - Est� activa: {(isActive ? "? S�" : "? NO")}");
                diagnostic.AppendLine($"   - RESULTADO: {(isValid ? "? V�LIDA - DEBER�A PERMITIR HOMEPAGE" : "? INV�LIDA - DEBER�A IR A LOGIN")}");
            }
            else
            {
                diagnostic.AppendLine("   ? NO HAY SESI�N ACTUAL");
                diagnostic.AppendLine("   ?? RESULTADO: DEBE IR A LOGIN");
            }
            
            diagnostic.AppendLine();
            diagnostic.AppendLine("2. VERIFICACI�N DEL M�TODO HasValidTokenInSessionAsync:");
            bool hasValidToken = await HasValidTokenInSessionAsync();
            diagnostic.AppendLine($"   Resultado: {(hasValidToken ? "? TRUE - DEBE IR A HOMEPAGE" : "? FALSE - DEBE IR A LOGIN")}");
            
            diagnostic.AppendLine();
            diagnostic.AppendLine("3. RECOMENDACI�N:");
            if (currentSession != null && hasValidToken)
            {
                diagnostic.AppendLine("   ?? La aplicaci�n DEBE navegar al HOMEPAGE y PERMANECER ah�");
                diagnostic.AppendLine("   ?? Si est� regresando al login, hay un problema en el HomePage o posterior");
            }
            else
            {
                diagnostic.AppendLine("   ?? La aplicaci�n DEBE navegar al LOGIN");
                diagnostic.AppendLine("   ?? Verificar por qu� no se detect� sesi�n v�lida");
            }
            
            return diagnostic.ToString();
        }
        catch (Exception ex)
        {
            return $"ERROR EN DIAGN�STICO DE NAVEGACI�N: {ex.Message}\nStackTrace: {ex.StackTrace}";
        }
    }

    /// <summary>
    /// Verifica si hay un token v�lido espec�ficamente en la tabla Session
    /// Este m�todo determina si la aplicaci�n debe ir directamente al homepage
    /// Solo verifica Session, no Configuraci�n
    /// </summary>
    public async Task<bool> HasValidTokenInSessionAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("?? === Iniciando verificaci�n de token en Session (L�GICA PRINCIPAL) ===");
            
            var sessions = await _databaseService.GetAllAsync<Session>();
            System.Diagnostics.Debug.WriteLine($"?? Total de sessions encontradas en BD: {sessions?.Count ?? 0}");
            
            if (sessions == null || !sessions.Any())
            {
                System.Diagnostics.Debug.WriteLine("? No hay sessions en la base de datos - IR A LOGIN");
                return false;
            }

            // Log de todas las sessions para debugging
            foreach (var session in sessions.OrderByDescending(s => s.CreatedAt))
            {
                System.Diagnostics.Debug.WriteLine($"?? Session ID: {session.Id}");
                System.Diagnostics.Debug.WriteLine($"   - Usuario: {session.Username} ({session.NombreCompleto})");
                System.Diagnostics.Debug.WriteLine($"   - Empresa: {session.IdEmpresa}");
                System.Diagnostics.Debug.WriteLine($"   - Token: {(string.IsNullOrEmpty(session.Token) ? "VAC�O" : "PRESENTE")}");
                System.Diagnostics.Debug.WriteLine($"   - Token length: {session.Token?.Length ?? 0}");
                System.Diagnostics.Debug.WriteLine($"   - CreatedAt: {session.CreatedAt}");
                System.Diagnostics.Debug.WriteLine($"   - ExpirationDate: {session.ExpirationDate}");
                System.Diagnostics.Debug.WriteLine($"   - IsActive: {session.IsActive}");
                System.Diagnostics.Debug.WriteLine($"   - Expirado: {session.ExpirationDate <= DateTime.Now}");
                System.Diagnostics.Debug.WriteLine($"   - Tiempo actual: {DateTime.Now}");
                System.Diagnostics.Debug.WriteLine($"   - Tiempo restante: {(session.ExpirationDate - DateTime.Now).TotalMinutes:F2} minutos");
            }

            // Buscar la sesi�n m�s reciente que tenga token v�lido
            var activeSession = sessions
                .Where(s => !string.IsNullOrEmpty(s.Token) && 
                           s.ExpirationDate > DateTime.Now &&
                           s.IsActive)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefault();
            
            if (activeSession != null)
            {
                System.Diagnostics.Debug.WriteLine($"? TOKEN V�LIDO ENCONTRADO EN SESSION M�S RECIENTE - IR A HOMEPAGE:");
                System.Diagnostics.Debug.WriteLine($"   - Session ID: {activeSession.Id}");
                System.Diagnostics.Debug.WriteLine($"   - Usuario: {activeSession.Username} ({activeSession.NombreCompleto})");
                System.Diagnostics.Debug.WriteLine($"   - Empresa ID: {activeSession.IdEmpresa}");
                System.Diagnostics.Debug.WriteLine($"   - Token length: {activeSession.Token.Length}");
                System.Diagnostics.Debug.WriteLine($"   - Creada: {activeSession.CreatedAt}");
                System.Diagnostics.Debug.WriteLine($"   - Expira: {activeSession.ExpirationDate}");
                System.Diagnostics.Debug.WriteLine($"   - IsActive: {activeSession.IsActive}");
                System.Diagnostics.Debug.WriteLine($"   - Tiempo restante: {(activeSession.ExpirationDate - DateTime.Now).TotalMinutes:F2} minutos");
                System.Diagnostics.Debug.WriteLine("?? RESULTADO: La aplicaci�n ir� directamente al HOMEPAGE");
                
                // Cargar esta sesi�n en memoria para uso futuro
                _currentSession = activeSession;
                
                return true;
            }
            
            System.Diagnostics.Debug.WriteLine("? NO HAY TOKEN V�LIDO EN SESSION - IR A LOGIN");
            System.Diagnostics.Debug.WriteLine("   Razones posibles:");
            System.Diagnostics.Debug.WriteLine("   - No hay sessions con token");
            System.Diagnostics.Debug.WriteLine("   - Todas las sessions est�n expiradas");
            System.Diagnostics.Debug.WriteLine("   - Todas las sessions est�n inactivas (IsActive = false)");
            System.Diagnostics.Debug.WriteLine("?? RESULTADO: La aplicaci�n ir� al LOGIN");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error verificando token en Session: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"? StackTrace: {ex.StackTrace}");
            System.Diagnostics.Debug.WriteLine("?? RESULTADO: Error - La aplicaci�n ir� al LOGIN por seguridad");
            return false;
        }
    }

    /// <summary>
    /// Carga la sesi�n m�s reciente de la base de datos al inicializar la aplicaci�n
    /// Este m�todo debe ser llamado al inicio para verificar si hay una sesi�n activa
    /// </summary>
    public async Task<Session?> LoadMostRecentSessionAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("?? === CARGANDO SESI�N M�S RECIENTE AL INICIAR APLICACI�N ===");
            
            var sessions = await _databaseService.GetAllAsync<Session>();
            
            if (sessions == null || !sessions.Any())
            {
                System.Diagnostics.Debug.WriteLine("?? No hay sesiones guardadas en la base de datos");
                _currentSession = null;
                return null;
            }
            
            System.Diagnostics.Debug.WriteLine($"?? Total de sesiones encontradas: {sessions.Count}");
            
            // Obtener la sesi�n m�s reciente (ordenada por CreatedAt)
            var mostRecentSession = sessions
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefault();
            
            if (mostRecentSession != null)
            {
                System.Diagnostics.Debug.WriteLine($"?? SESI�N M�S RECIENTE ENCONTRADA:");
                System.Diagnostics.Debug.WriteLine($"   - ID: {mostRecentSession.Id}");
                System.Diagnostics.Debug.WriteLine($"   - Usuario: {mostRecentSession.Username} ({mostRecentSession.NombreCompleto})");
                System.Diagnostics.Debug.WriteLine($"   - Empresa: {mostRecentSession.IdEmpresa}");
                System.Diagnostics.Debug.WriteLine($"   - Creada: {mostRecentSession.CreatedAt}");
                System.Diagnostics.Debug.WriteLine($"   - Expira: {mostRecentSession.ExpirationDate}");
                System.Diagnostics.Debug.WriteLine($"   - IsActive: {mostRecentSession.IsActive}");
                System.Diagnostics.Debug.WriteLine($"   - Token presente: {(!string.IsNullOrEmpty(mostRecentSession.Token) ? "S�" : "NO")}");
                
                // Verificar si es v�lida para uso
                bool isValidForUse = !string.IsNullOrEmpty(mostRecentSession.Token) && 
                                   mostRecentSession.ExpirationDate > DateTime.Now && 
                                   mostRecentSession.IsActive;
                
                System.Diagnostics.Debug.WriteLine($"   - Es v�lida para usar: {(isValidForUse ? "S�" : "NO")}");
                
                if (isValidForUse)
                {
                    System.Diagnostics.Debug.WriteLine($"   - Tiempo restante: {(mostRecentSession.ExpirationDate - DateTime.Now).TotalMinutes:F2} minutos");
                    _currentSession = mostRecentSession;
                    System.Diagnostics.Debug.WriteLine("?? ? Sesi�n v�lida cargada en memoria");
                }
                else
                {
                    if (string.IsNullOrEmpty(mostRecentSession.Token))
                        System.Diagnostics.Debug.WriteLine("   - Raz�n de invalidez: Token vac�o");
                    else if (mostRecentSession.ExpirationDate <= DateTime.Now)
                        System.Diagnostics.Debug.WriteLine("   - Raz�n de invalidez: Sesi�n expirada");
                    else if (!mostRecentSession.IsActive)
                        System.Diagnostics.Debug.WriteLine("   - Raz�n de invalidez: Sesi�n inactiva");
                    
                    _currentSession = null;
                    System.Diagnostics.Debug.WriteLine("?? ? Sesi�n encontrada pero no es v�lida");
                }
                
                return mostRecentSession;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("?? No se pudo obtener la sesi�n m�s reciente (error inesperado)");
                _currentSession = null;
                return null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"?? ? Error cargando sesi�n m�s reciente: {ex.Message}");
            _currentSession = null;
            return null;
        }
    }

    /// <summary>
    /// M�todo de conveniencia para verificar r�pidamente si hay una sesi�n activa v�lida
    /// </summary>
    public async Task<bool> HasActiveSessionAsync()
    {
        try
        {
            var session = await GetCurrentSessionAsync();
            return session != null && 
                   !string.IsNullOrEmpty(session.Token) && 
                   session.ExpirationDate > DateTime.Now && 
                   session.IsActive;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error verificando sesi�n activa: {ex.Message}");
            return false;
        }
    }
}