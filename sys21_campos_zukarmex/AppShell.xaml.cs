using sys21_campos_zukarmex.Views;
using sys21_campos_zukarmex.Services;

namespace sys21_campos_zukarmex;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes for navigation
        Routing.RegisterRoute("loading", typeof(LoadingPage));
        Routing.RegisterRoute("login", typeof(LoginPage));
        Routing.RegisterRoute("adminconfig", typeof(AdminConfigPage));
        Routing.RegisterRoute("sync", typeof(SyncPage));
        Routing.RegisterRoute("home", typeof(HomePage));
        Routing.RegisterRoute("vale", typeof(ValePage));
        Routing.RegisterRoute("status", typeof(StatusPage));
        Routing.RegisterRoute("autorizacion", typeof(AuthorizationPage));
        Routing.RegisterRoute("historial1", typeof(HistorialPage));
        Routing.RegisterRoute("historial2", typeof(HistorialPage));
        Routing.RegisterRoute("historial3", typeof(HistorialPage));
        Routing.RegisterRoute(nameof(Views.RatTrappingPage), typeof(Views.RatTrappingPage));
        Routing.RegisterRoute(nameof(Views.RodenticideConsumptionPage), typeof(Views.RodenticideConsumptionPage));
        Routing.RegisterRoute(nameof(Views.DamageAssessmentPage), typeof(Views.DamageAssessmentPage));
        Routing.RegisterRoute(nameof(Views.IrrigationLinePage), typeof(Views.IrrigationLinePage));
        // Register new routes
        Routing.RegisterRoute(nameof(AgregarArticuloPage), typeof(AgregarArticuloPage));
        Routing.RegisterRoute("navigationdemo", typeof(NavigationDemoPage));

        // Configure flyout behavior
        ConfigureFlyoutBehavior();
    }

    private void ConfigureFlyoutBehavior()
    {
        // Configure flyout to close after selection
        this.Navigated += OnShellNavigated;
        
        // Add gesture support for flyout
        this.PropertyChanged += OnShellPropertyChanged;
    }

    private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        // Auto-close flyout after navigation
        if (Shell.Current.FlyoutIsPresented)
        {
            // Add a small delay to allow user to see the selection
            Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.StartTimer(TimeSpan.FromMilliseconds(300), () =>
            {
                Shell.Current.FlyoutIsPresented = false;
                return false;
            });
        }
    }

    private void OnShellPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Handle property changes if needed for enhanced navigation experience
        if (e.PropertyName == nameof(FlyoutIsPresented))
        {
            System.Diagnostics.Debug.WriteLine($"Flyout is now: {(FlyoutIsPresented ? "Open" : "Closed")}");
        }
    }

    // Event handler for logout button
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        try
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Cerrar Sesi�n", 
                "�Est� seguro que desea cerrar sesi�n?", 
                "S�", 
                "No");
                
            if (confirm)
            {
                // Close flyout first
                FlyoutIsPresented = false;
                
                try
                {
                    // Get services from DI container
                    var serviceProvider = Handler?.MauiContext?.Services;
                    if (serviceProvider != null)
                    {
                        var sessionService = serviceProvider.GetService<SessionService>();
                        var connectivityService = serviceProvider.GetService<ConnectivityService>();
                        
                        // Clear session
                        if (sessionService != null)
                        {
                            await sessionService.ClearSessionAsync();
                            System.Diagnostics.Debug.WriteLine("? Sesi�n limpiada correctamente");
                        }
                        
                        // Stop connectivity monitoring
                        if (connectivityService != null)
                        {
                            connectivityService.StopMonitoring();
                            System.Diagnostics.Debug.WriteLine("? Monitoreo de conectividad detenido");
                        }
                    }
                }
                catch (Exception serviceEx)
                {
                    System.Diagnostics.Debug.WriteLine($"?? Error accediendo a servicios durante logout: {serviceEx.Message}");
                    // Continue with navigation even if service cleanup fails
                }
                
                // Navigate to login
                await Shell.Current.GoToAsync("//login");
                System.Diagnostics.Debug.WriteLine("? Navegaci�n a login completada");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error durante logout: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", "Error al cerrar sesi�n", "OK");
        }
    }

    // Event handler for settings menu item
    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        try
        {
            // Close flyout
            FlyoutIsPresented = false;
            
            // Navigate to navigation demo page
            await Shell.Current.GoToAsync("//navigationdemo");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error navigating to settings: {ex.Message}");
        }
    }

    // Method to programmatically open/close flyout (can be called from other parts of the app)
    public void ToggleFlyout()
    {
        FlyoutIsPresented = !FlyoutIsPresented;
    }

    // Method to show flyout from gesture or button
    public void ShowFlyout()
    {
        FlyoutIsPresented = true;
    }

    // Method to hide flyout
    public void HideFlyout()
    {
        FlyoutIsPresented = false;
    }
}