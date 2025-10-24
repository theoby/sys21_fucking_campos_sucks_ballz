using sys21_campos_zukarmex.Services;

namespace sys21_campos_zukarmex;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;
    private static bool _initialNavigationCompleted = false;
    
    /// <summary>
    /// Servicio de conectividad global accesible desde toda la aplicación
    /// </summary>
    public static ConnectivityService? ConnectivityService { get; private set; }

    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        
        // Inicializar el servicio de conectividad global
        ConnectivityService = serviceProvider.GetService<ConnectivityService>();
        
        // Inicializar configuración guardada al arrancar la app
        _ = Task.Run(async () =>
        {
            try
            {
                var configService = serviceProvider.GetService<IConfiguracionService>();
                if (configService != null)
                {
                    await configService.InitializeFromStoredConfigAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error inicializando configuración: {ex.Message}");
            }
        });
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var sessionService = _serviceProvider.GetService<SessionService>();
        var shell = new AppShell(sessionService);
        var window = new Window(shell);
        
        // Inicializar monitoreo de conectividad cuando se crea la ventana principal
        if (ConnectivityService != null)
        {
            ConnectivityService.StartMonitoring();
        }
        
        // Configurar navegación inicial de forma más robusta
        window.Created += async (s, e) =>
        {
            // Solo realizar navegación inicial una vez
            if (!_initialNavigationCompleted)
            {
                await PerformInitialNavigationAsync(shell);
                _initialNavigationCompleted = true;
            }
        };
        
        return window;
    }

    /// <summary>
    /// Realiza la navegación inicial basada en el estado de la sesión usando NavigationService
    /// </summary>
    private async Task PerformInitialNavigationAsync(Shell shell)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("🚀 === INICIANDO NAVEGACIÓN INICIAL CON NAVIGATIONSERVICE ===");
            System.Diagnostics.Debug.WriteLine($"🕐 Tiempo actual: {DateTime.Now}");
            
            // Esperar un momento para que todos los servicios estén listos
            await Task.Delay(150);
            System.Diagnostics.Debug.WriteLine("⏱️ Delay de inicialización completado");
            
            var navigationService = _serviceProvider.GetService<NavigationService>();
            
            if (navigationService != null)
            {
                System.Diagnostics.Debug.WriteLine("✅ NavigationService obtenido correctamente");
                
                // USANDO MÉTODO ULTRA-SIMPLE PARA DEBUGGING
                System.Diagnostics.Debug.WriteLine("🎯 USANDO MÉTODO ULTRA-SIMPLE PARA DEBUGGING");
                await navigationService.SimpleNavigationAsync();
                
                System.Diagnostics.Debug.WriteLine("✅ Navegación simple completada");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ NavigationService no disponible - fallback a lógica anterior");
                
                // Fallback a la lógica anterior si no hay NavigationService
                var sessionService = _serviceProvider.GetService<SessionService>();
                var databaseService = _serviceProvider.GetService<DatabaseService>();
                
                if (sessionService != null && databaseService != null)
                {
                    System.Diagnostics.Debug.WriteLine("✅ SessionService y DatabaseService obtenidos correctamente");
                    
                    // Asegurar que la base de datos esté inicializada
                    System.Diagnostics.Debug.WriteLine("🔧 Inicializando base de datos...");
                    await databaseService.InitializeAsync();
                    System.Diagnostics.Debug.WriteLine("✅ Base de datos inicializada");
                    
                    // PASO 1: Cargar la sesión más reciente
                    System.Diagnostics.Debug.WriteLine("📱 Cargando sesión más reciente de la base de datos...");
                    var mostRecentSession = await sessionService.LoadMostRecentSessionAsync();
                    
                    if (mostRecentSession != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"📱 Sesión encontrada: ID {mostRecentSession.Id}, Usuario: {mostRecentSession.Username}");
                        
                        // PASO 2: Verificar si la sesión tiene token válido
                        bool hasValidToken = !string.IsNullOrEmpty(mostRecentSession.Token) && 
                                           mostRecentSession.ExpirationDate > DateTime.Now && 
                                           mostRecentSession.IsActive;
                        
                        System.Diagnostics.Debug.WriteLine($"🔍 Sesión tiene token válido: {hasValidToken}");
                        
                        if (hasValidToken)
                        {
                            System.Diagnostics.Debug.WriteLine("🔑 ✅ SESIÓN VÁLIDA ENCONTRADA - NAVEGANDO DIRECTAMENTE A HOMEPAGE");
                            System.Diagnostics.Debug.WriteLine($"   - Usuario: {mostRecentSession.Username} ({mostRecentSession.NombreCompleto})");
                            System.Diagnostics.Debug.WriteLine($"   - Empresa: {mostRecentSession.IdEmpresa}");

                            // Navegar directamente al home
                            await shell.GoToAsync("//home");
                            System.Diagnostics.Debug.WriteLine("🏠 ✅ Navegación a homepage completada exitosamente");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("🔑 ❌ SESIÓN ENCONTRADA PERO TOKEN INVÁLIDO - NAVEGANDO A LOGIN");
                            
                            if (string.IsNullOrEmpty(mostRecentSession.Token))
                                System.Diagnostics.Debug.WriteLine("   - Motivo: Token vacío o nulo");
                            else if (mostRecentSession.ExpirationDate <= DateTime.Now)
                                System.Diagnostics.Debug.WriteLine($"   - Motivo: Sesión expirada (Exp: {mostRecentSession.ExpirationDate}, Ahora: {DateTime.Now})");
                            else if (!mostRecentSession.IsActive)
                                System.Diagnostics.Debug.WriteLine("   - Motivo: Sesión marcada como inactiva");
                            
                            await shell.GoToAsync("//login");
                            System.Diagnostics.Debug.WriteLine("🔐 ✅ Navegación a login completada");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("📱 ❌ NO SE ENCONTRÓ NINGUNA SESIÓN - NAVEGANDO A LOGIN");
                        await shell.GoToAsync("//login");
                        System.Diagnostics.Debug.WriteLine("🔐 ✅ Navegación a login completada");
                    }
                    
                    // Ejecutar diagnóstico completo DESPUÉS de la navegación para referencia
                    System.Diagnostics.Debug.WriteLine("🔍 === EJECUTANDO DIAGNÓSTICO COMPLETO PARA REFERENCIA ===");
                    var diagnostic = await sessionService.GetDatabaseDiagnosticAsync();
                    System.Diagnostics.Debug.WriteLine(diagnostic);
                }
                else
                {
                    if (sessionService == null)
                        System.Diagnostics.Debug.WriteLine("❌ SessionService no disponible");
                    if (databaseService == null)
                        System.Diagnostics.Debug.WriteLine("❌ DatabaseService no disponible");
                        
                    System.Diagnostics.Debug.WriteLine("⚠️ Servicios no disponibles - navegando a login");
                    await shell.GoToAsync("//login");
                    System.Diagnostics.Debug.WriteLine("✅ Navegación a login completada (fallback)");
                }
            }
            
            System.Diagnostics.Debug.WriteLine("🏁 === NAVEGACIÓN INICIAL COMPLETADA ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ === ERROR EN NAVEGACIÓN INICIAL ===");
            System.Diagnostics.Debug.WriteLine($"❌ Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"❌ StackTrace: {ex.StackTrace}");
            
            // Fallback en caso de error
            await shell.GoToAsync("//login");
            System.Diagnostics.Debug.WriteLine("✅ Navegación a login completada (error fallback)");
        }
    }

    /// <summary>
    /// Método público para resetear la bandera de navegación inicial
    /// Útil para testing o reinicios de la aplicación
    /// </summary>
    public static void ResetInitialNavigation()
    {
        _initialNavigationCompleted = false;
    }

    protected override void OnSleep()
    {
        // Pausar monitoreo cuando la app se va a segundo plano
        ConnectivityService?.StopMonitoring();
        base.OnSleep();
    }

    protected override void OnResume()
    {
        // Reanudar monitoreo cuando la app vuelve al primer plano
        ConnectivityService?.StartMonitoring();
        base.OnResume();
    }
}