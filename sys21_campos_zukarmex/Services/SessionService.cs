using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Text;

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
        // Primero verificar sesión activa
        var session = await GetCurrentSessionAsync();
        if (session != null && session.ExpirationDate > DateTime.Now)
        {
            return true;
        }

        // Si no hay sesión activa, verificar token en configuración
        return await HasValidTokenInConfigurationAsync();
    }

    /// <summary>
    /// Verifica si hay un token válido considerando tanto Session como Configuración
    /// Prioridad: 1. Session activa, 2. Token en Configuración
    /// </summary>
    public async Task<bool> HasValidTokenInConfigurationAsync()
    {
        try
        {
            // Primero verificar si hay una sesión activa con token válido
            var currentSession = await GetCurrentSessionAsync();
            if (currentSession != null && !string.IsNullOrEmpty(currentSession.Token) && currentSession.ExpirationDate > DateTime.Now)
            {
                System.Diagnostics.Debug.WriteLine($"?? Token válido encontrado en Session - Exp: {currentSession.ExpirationDate}");
                return true;
            }

            // Si no hay sesión activa, verificar token en configuración
            var configuraciones = await _databaseService.GetAllAsync<Configuracion>();
            var configActiva = configuraciones.OrderByDescending(c => c.Fecha).FirstOrDefault();
            
            if (configActiva != null && configActiva.HasValidToken)
            {
                System.Diagnostics.Debug.WriteLine($"?? Token válido encontrado en Configuración - Exp: {configActiva.TokenExpiration}");
                return true;
            }
            
            System.Diagnostics.Debug.WriteLine("? No hay token válido en Session ni en Configuración");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error verificando token: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Obtiene el token válido considerando tanto Session como Configuración
    /// Prioridad: 1. Session activa, 2. Token en Configuración
    /// </summary>
    public async Task<string?> GetValidTokenFromConfigurationAsync()
    {
        try
        {
            // Primero verificar si hay una sesión activa con token válido
            var currentSession = await GetCurrentSessionAsync();
            if (currentSession != null && !string.IsNullOrEmpty(currentSession.Token) && currentSession.ExpirationDate > DateTime.Now)
            {
                System.Diagnostics.Debug.WriteLine($"?? Usando token de Session activa");
                return currentSession.Token;
            }

            // Si no hay sesión activa, verificar token en configuración
            var configuraciones = await _databaseService.GetAllAsync<Configuracion>();
            var configActiva = configuraciones.OrderByDescending(c => c.Fecha).FirstOrDefault();
            
            if (configActiva != null && configActiva.HasValidToken)
            {
                System.Diagnostics.Debug.WriteLine($"?? Usando token de Configuración");
                return configActiva.Token;
            }
            
            System.Diagnostics.Debug.WriteLine("? No hay token válido disponible");
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
        // Si ya tenemos una sesión en memoria y sigue siendo válida, usarla
        if (_currentSession != null && _currentSession.ExpirationDate > DateTime.Now && _currentSession.IsActive)
            return _currentSession;

        // Buscar la sesión más reciente en la base de datos
        var sessions = await _databaseService.GetAllAsync<Session>();
        
        if (sessions == null || !sessions.Any())
        {
            _currentSession = null;
            return null;
        }

        // Obtener la sesión más nueva (por CreatedAt) que esté activa y no expirada
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

        // Asegurar que CreatedAt esté configurado correctamente
        if (newSession.CreatedAt == default(DateTime))
        {
            newSession.CreatedAt = DateTime.Now;
        }

        // Asegurar que IsActive esté en true
        newSession.IsActive = true;

        System.Diagnostics.Debug.WriteLine($"?? Guardando nueva sesión:");
        System.Diagnostics.Debug.WriteLine($"   - Usuario: {newSession.Username} ({newSession.NombreCompleto})");
        System.Diagnostics.Debug.WriteLine($"   - Empresa: {newSession.IdEmpresa}");
        System.Diagnostics.Debug.WriteLine($"   - CreatedAt: {newSession.CreatedAt}");
        System.Diagnostics.Debug.WriteLine($"   - ExpirationDate: {newSession.ExpirationDate}");
        System.Diagnostics.Debug.WriteLine($"   - IsActive: {newSession.IsActive}");
        System.Diagnostics.Debug.WriteLine($"   - Token presente: {(!string.IsNullOrEmpty(newSession.Token) ? "SÍ" : "NO")}");

        await _databaseService.ClearTableAsync<Session>();
        await _databaseService.SaveAsync(newSession);
        _currentSession = newSession;

        System.Diagnostics.Debug.WriteLine("?? ? Sesión guardada exitosamente en la base de datos");

        // Guardar también el token en la configuración
        await SaveTokenToConfigurationAsync(newSession.Token, newSession.ExpirationDate);
    }

    /// <summary>
    /// Guarda el token en la configuración activa
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
                
                System.Diagnostics.Debug.WriteLine($"?? Token guardado en configuración - Exp: {expiration}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("?? No se encontró configuración activa para guardar token");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error guardando token en configuración: {ex.Message}");
        }
    }

    public async Task ClearSessionAsync()
    {
        await _databaseService.ClearTableAsync<Session>();
        _currentSession = null;

        // Limpiar también el token de la configuración
        await ClearTokenFromConfigurationAsync();
    }

    /// <summary>
    /// Limpia el token de la configuración activa
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
                
                System.Diagnostics.Debug.WriteLine("?? Token limpiado de configuración");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error limpiando token de configuración: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifica si el token actual es diferente al almacenado en configuración
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
    /// Método de diagnóstico para verificar el estado completo de la base de datos
    /// </summary>
    public async Task<string> GetDatabaseDiagnosticAsync()
    {
        try
        {
            var diagnostic = new StringBuilder();
            diagnostic.AppendLine("=== DIAGNÓSTICO COMPLETO DE BASE DE DATOS ===");
            diagnostic.AppendLine($"Tiempo: {DateTime.Now}");
            diagnostic.AppendLine("?? LÓGICA ACTUAL: Verificando token en tabla Session únicamente");
            
            // Verificar si la base de datos existe
            var dbExists = await _databaseService.DatabaseExistsAsync();
            diagnostic.AppendLine($"Base de datos existe: {dbExists}");
            
            if (dbExists)
            {
                var dbSize = await _databaseService.GetDatabaseSizeAsync();
                diagnostic.AppendLine($"Tamaño de base de datos: {dbSize} bytes");
            }
            
            // === VERIFICAR SESSIONS (PRIORIDAD) ===
            diagnostic.AppendLine();
            diagnostic.AppendLine("=== ANÁLISIS DE SESSIONS (LÓGICA PRINCIPAL) ===");
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
                    
                    // Verificar si cumple todos los criterios para navegación automática
                    bool isValidForAutoNav = !string.IsNullOrEmpty(session.Token) && 
                                           session.ExpirationDate > DateTime.Now && 
                                           session.IsActive;
                    diagnostic.AppendLine($"    ¿Válida para auto-navegación?: {(isValidForAutoNav ? "SÍ" : "NO")}");
                    diagnostic.AppendLine();
                }
                
                // Analizar session activa para auto-navegación
                var activeSession = allSessions.FirstOrDefault(s => 
                    !string.IsNullOrEmpty(s.Token) && 
                    s.ExpirationDate > DateTime.Now && 
                    s.IsActive);
                    
                if (activeSession != null)
                {
                    diagnostic.AppendLine($"*** SESSION ACTIVA PARA AUTO-NAVEGACIÓN: ID {activeSession.Id} ***");
                    diagnostic.AppendLine($"    Usuario: {activeSession.Username} ({activeSession.NombreCompleto})");
                    diagnostic.AppendLine($"    Empresa: {activeSession.IdEmpresa}");
                    diagnostic.AppendLine($"    Expira: {activeSession.ExpirationDate}");
                    diagnostic.AppendLine($"    Tiempo restante: {(activeSession.ExpirationDate - DateTime.Now).TotalMinutes:F2} minutos");
                }
                else
                {
                    diagnostic.AppendLine("*** NO HAY SESSION ACTIVA VÁLIDA PARA AUTO-NAVEGACIÓN ***");
                }
            }
            else
            {
                diagnostic.AppendLine("*** NO HAY SESSIONS EN LA BASE DE DATOS ***");
            }
            
            // === VERIFICAR CONFIGURACIONES (INFORMACIÓN) ===
            diagnostic.AppendLine();
            diagnostic.AppendLine("=== ANÁLISIS DE CONFIGURACIONES (SOLO INFORMATIVO) ===");
            var allConfigs = await _databaseService.GetAllAsync<Configuracion>();
            diagnostic.AppendLine($"Total configuraciones en BD: {allConfigs?.Count ?? 0}");
            
            if (allConfigs != null && allConfigs.Any())
            {
                foreach (var config in allConfigs.OrderByDescending(c => c.Fecha))
                {
                    diagnostic.AppendLine($"  Configuración ID: {config.Id}");
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
            
            diagnostic.AppendLine("=== FIN DIAGNÓSTICO ===");
            return diagnostic.ToString();
        }
        catch (Exception ex)
        {
            return $"ERROR EN DIAGNÓSTICO: {ex.Message}";
        }
    }

    /// <summary>
    /// Obtiene un diagnóstico específico del problema de navegación
    /// </summary>
    public async Task<string> GetNavigationDiagnosticAsync()
    {
        try
        {
            var diagnostic = new StringBuilder();
            diagnostic.AppendLine("=== DIAGNÓSTICO DE NAVEGACIÓN ===");
            diagnostic.AppendLine($"Tiempo: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            diagnostic.AppendLine();
            
            // Verificar sesión actual
            diagnostic.AppendLine("1. VERIFICACIÓN DE SESIÓN ACTUAL:");
            var currentSession = await GetCurrentSessionAsync();
            
            if (currentSession != null)
            {
                diagnostic.AppendLine($"   ? Sesión encontrada - ID: {currentSession.Id}");
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
                diagnostic.AppendLine("   EVALUACIÓN DE VALIDEZ:");
                diagnostic.AppendLine($"   - Tiene token: {(hasToken ? "? SÍ" : "? NO")}");
                diagnostic.AppendLine($"   - No expirada: {(notExpired ? "? SÍ" : "? NO")}");
                diagnostic.AppendLine($"   - Está activa: {(isActive ? "? SÍ" : "? NO")}");
                diagnostic.AppendLine($"   - RESULTADO: {(isValid ? "? VÁLIDA - DEBERÍA PERMITIR HOMEPAGE" : "? INVÁLIDA - DEBERÍA IR A LOGIN")}");
            }
            else
            {
                diagnostic.AppendLine("   ? NO HAY SESIÓN ACTUAL");
                diagnostic.AppendLine("   ?? RESULTADO: DEBE IR A LOGIN");
            }
            
            diagnostic.AppendLine();
            diagnostic.AppendLine("2. VERIFICACIÓN DEL MÉTODO HasValidTokenInSessionAsync:");
            bool hasValidToken = await HasValidTokenInSessionAsync();
            diagnostic.AppendLine($"   Resultado: {(hasValidToken ? "? TRUE - DEBE IR A HOMEPAGE" : "? FALSE - DEBE IR A LOGIN")}");
            
            diagnostic.AppendLine();
            diagnostic.AppendLine("3. RECOMENDACIÓN:");
            if (currentSession != null && hasValidToken)
            {
                diagnostic.AppendLine("   ?? La aplicación DEBE navegar al HOMEPAGE y PERMANECER ahí");
                diagnostic.AppendLine("   ?? Si está regresando al login, hay un problema en el HomePage o posterior");
            }
            else
            {
                diagnostic.AppendLine("   ?? La aplicación DEBE navegar al LOGIN");
                diagnostic.AppendLine("   ?? Verificar por qué no se detectó sesión válida");
            }
            
            return diagnostic.ToString();
        }
        catch (Exception ex)
        {
            return $"ERROR EN DIAGNÓSTICO DE NAVEGACIÓN: {ex.Message}\nStackTrace: {ex.StackTrace}";
        }
    }

    /// <summary>
    /// Verifica si hay un token válido específicamente en la tabla Session
    /// Este método determina si la aplicación debe ir directamente al homepage
    /// Solo verifica Session, no Configuración
    /// </summary>
    public async Task<bool> HasValidTokenInSessionAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("?? === Iniciando verificación de token en Session (LÓGICA PRINCIPAL) ===");
            
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
                System.Diagnostics.Debug.WriteLine($"   - Token: {(string.IsNullOrEmpty(session.Token) ? "VACÍO" : "PRESENTE")}");
                System.Diagnostics.Debug.WriteLine($"   - Token length: {session.Token?.Length ?? 0}");
                System.Diagnostics.Debug.WriteLine($"   - CreatedAt: {session.CreatedAt}");
                System.Diagnostics.Debug.WriteLine($"   - ExpirationDate: {session.ExpirationDate}");
                System.Diagnostics.Debug.WriteLine($"   - IsActive: {session.IsActive}");
                System.Diagnostics.Debug.WriteLine($"   - Expirado: {session.ExpirationDate <= DateTime.Now}");
                System.Diagnostics.Debug.WriteLine($"   - Tiempo actual: {DateTime.Now}");
                System.Diagnostics.Debug.WriteLine($"   - Tiempo restante: {(session.ExpirationDate - DateTime.Now).TotalMinutes:F2} minutos");
            }

            // Buscar la sesión más reciente que tenga token válido
            var activeSession = sessions
                .Where(s => !string.IsNullOrEmpty(s.Token) && 
                           s.ExpirationDate > DateTime.Now &&
                           s.IsActive)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefault();
            
            if (activeSession != null)
            {
                System.Diagnostics.Debug.WriteLine($"? TOKEN VÁLIDO ENCONTRADO EN SESSION MÁS RECIENTE - IR A HOMEPAGE:");
                System.Diagnostics.Debug.WriteLine($"   - Session ID: {activeSession.Id}");
                System.Diagnostics.Debug.WriteLine($"   - Usuario: {activeSession.Username} ({activeSession.NombreCompleto})");
                System.Diagnostics.Debug.WriteLine($"   - Empresa ID: {activeSession.IdEmpresa}");
                System.Diagnostics.Debug.WriteLine($"   - Token length: {activeSession.Token.Length}");
                System.Diagnostics.Debug.WriteLine($"   - Creada: {activeSession.CreatedAt}");
                System.Diagnostics.Debug.WriteLine($"   - Expira: {activeSession.ExpirationDate}");
                System.Diagnostics.Debug.WriteLine($"   - IsActive: {activeSession.IsActive}");
                System.Diagnostics.Debug.WriteLine($"   - Tiempo restante: {(activeSession.ExpirationDate - DateTime.Now).TotalMinutes:F2} minutos");
                System.Diagnostics.Debug.WriteLine("?? RESULTADO: La aplicación irá directamente al HOMEPAGE");
                
                // Cargar esta sesión en memoria para uso futuro
                _currentSession = activeSession;
                
                return true;
            }
            
            System.Diagnostics.Debug.WriteLine("? NO HAY TOKEN VÁLIDO EN SESSION - IR A LOGIN");
            System.Diagnostics.Debug.WriteLine("   Razones posibles:");
            System.Diagnostics.Debug.WriteLine("   - No hay sessions con token");
            System.Diagnostics.Debug.WriteLine("   - Todas las sessions están expiradas");
            System.Diagnostics.Debug.WriteLine("   - Todas las sessions están inactivas (IsActive = false)");
            System.Diagnostics.Debug.WriteLine("?? RESULTADO: La aplicación irá al LOGIN");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error verificando token en Session: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"? StackTrace: {ex.StackTrace}");
            System.Diagnostics.Debug.WriteLine("?? RESULTADO: Error - La aplicación irá al LOGIN por seguridad");
            return false;
        }
    }

    /// <summary>
    /// Carga la sesión más reciente de la base de datos al inicializar la aplicación
    /// Este método debe ser llamado al inicio para verificar si hay una sesión activa
    /// </summary>
    public async Task<Session?> LoadMostRecentSessionAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("?? === CARGANDO SESIÓN MÁS RECIENTE AL INICIAR APLICACIÓN ===");
            
            var sessions = await _databaseService.GetAllAsync<Session>();
            
            if (sessions == null || !sessions.Any())
            {
                System.Diagnostics.Debug.WriteLine("?? No hay sesiones guardadas en la base de datos");
                _currentSession = null;
                return null;
            }
            
            System.Diagnostics.Debug.WriteLine($"?? Total de sesiones encontradas: {sessions.Count}");
            
            // Obtener la sesión más reciente (ordenada por CreatedAt)
            var mostRecentSession = sessions
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefault();
            
            if (mostRecentSession != null)
            {
                System.Diagnostics.Debug.WriteLine($"?? SESIÓN MÁS RECIENTE ENCONTRADA:");
                System.Diagnostics.Debug.WriteLine($"   - ID: {mostRecentSession.Id}");
                System.Diagnostics.Debug.WriteLine($"   - Usuario: {mostRecentSession.Username} ({mostRecentSession.NombreCompleto})");
                System.Diagnostics.Debug.WriteLine($"   - Empresa: {mostRecentSession.IdEmpresa}");
                System.Diagnostics.Debug.WriteLine($"   - Creada: {mostRecentSession.CreatedAt}");
                System.Diagnostics.Debug.WriteLine($"   - Expira: {mostRecentSession.ExpirationDate}");
                System.Diagnostics.Debug.WriteLine($"   - IsActive: {mostRecentSession.IsActive}");
                System.Diagnostics.Debug.WriteLine($"   - Token presente: {(!string.IsNullOrEmpty(mostRecentSession.Token) ? "SÍ" : "NO")}");
                
                // Verificar si es válida para uso
                bool isValidForUse = !string.IsNullOrEmpty(mostRecentSession.Token) && 
                                   mostRecentSession.ExpirationDate > DateTime.Now && 
                                   mostRecentSession.IsActive;
                
                System.Diagnostics.Debug.WriteLine($"   - Es válida para usar: {(isValidForUse ? "SÍ" : "NO")}");
                
                if (isValidForUse)
                {
                    System.Diagnostics.Debug.WriteLine($"   - Tiempo restante: {(mostRecentSession.ExpirationDate - DateTime.Now).TotalMinutes:F2} minutos");
                    _currentSession = mostRecentSession;
                    System.Diagnostics.Debug.WriteLine("?? ? Sesión válida cargada en memoria");
                }
                else
                {
                    if (string.IsNullOrEmpty(mostRecentSession.Token))
                        System.Diagnostics.Debug.WriteLine("   - Razón de invalidez: Token vacío");
                    else if (mostRecentSession.ExpirationDate <= DateTime.Now)
                        System.Diagnostics.Debug.WriteLine("   - Razón de invalidez: Sesión expirada");
                    else if (!mostRecentSession.IsActive)
                        System.Diagnostics.Debug.WriteLine("   - Razón de invalidez: Sesión inactiva");
                    
                    _currentSession = null;
                    System.Diagnostics.Debug.WriteLine("?? ? Sesión encontrada pero no es válida");
                }
                
                return mostRecentSession;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("?? No se pudo obtener la sesión más reciente (error inesperado)");
                _currentSession = null;
                return null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"?? ? Error cargando sesión más reciente: {ex.Message}");
            _currentSession = null;
            return null;
        }
    }

    /// <summary>
    /// Método de conveniencia para verificar rápidamente si hay una sesión activa válida
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
            System.Diagnostics.Debug.WriteLine($"Error verificando sesión activa: {ex.Message}");
            return false;
        }
    }
}