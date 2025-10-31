using sys21_campos_zukarmex.Views;
using sys21_campos_zukarmex.Services;
using Microsoft.Maui.Controls;

namespace sys21_campos_zukarmex;

public partial class AppShell : Shell
{
    private readonly SessionService _sessionService;
    public AppShell(SessionService sessionService)
    {
        InitializeComponent();
        _sessionService = sessionService;

        this.Navigating += OnShellNavigating; 
        this.Navigated += AppShell_Navigated;
        RegisterRoutes();
        ConfigureFlyoutBehavior();
    }

    private void RegisterRoutes()
    {
        Routing.RegisterRoute("loading", typeof(LoadingPage));
        Routing.RegisterRoute("login", typeof(LoginPage));
        Routing.RegisterRoute("adminconfig", typeof(AdminConfigPage));
        Routing.RegisterRoute("sync", typeof(SyncPage));
        Routing.RegisterRoute("home", typeof(HomePage));
        Routing.RegisterRoute("navigationdemo", typeof(NavigationDemoPage));

        Routing.RegisterRoute(nameof(Views.Rainfall.RainfallPage), typeof(Views.Rainfall.RainfallPage));
        Routing.RegisterRoute(nameof(Views.Rainfall.RainfallPendingPage), typeof(Views.Rainfall.RainfallPendingPage));
        Routing.RegisterRoute(nameof(Views.Rainfall.RainfallHistoryPage), typeof(Views.Rainfall.RainfallHistoryPage));

        Routing.RegisterRoute(nameof(Views.MachineryUsage.MachineryUsagePage), typeof(Views.MachineryUsage.MachineryUsagePage));
        Routing.RegisterRoute(nameof(Views.MachineryUsage.MachineryUsagePendingPage), typeof(Views.MachineryUsage.MachineryUsagePendingPage));
        Routing.RegisterRoute(nameof(Views.MachineryUsage.MachineryUsageHistoryPage), typeof(Views.MachineryUsage.MachineryUsageHistoryPage));

        Routing.RegisterRoute(nameof(Views.RatTrapping.RatTrappingPage), typeof(Views.RatTrapping.RatTrappingPage));
        Routing.RegisterRoute(nameof(Views.RatTrapping.RatTrappingPendingPage), typeof(Views.RatTrapping.RatTrappingPendingPage));
        Routing.RegisterRoute(nameof(Views.RatTrapping.RatTrappingHistoryPage), typeof(Views.RatTrapping.RatTrappingHistoryPage));

        Routing.RegisterRoute(nameof(Views.RodenticideConsumption.RodenticideConsumptionPage), typeof(Views.RodenticideConsumption.RodenticideConsumptionPage));
        Routing.RegisterRoute(nameof(Views.RodenticideConsumption.RodenticideConsumptionPendingPage), typeof(Views.RodenticideConsumption.RodenticideConsumptionPendingPage));
        Routing.RegisterRoute(nameof(Views.RodenticideConsumption.RodenticideConsumptionHistoryPage), typeof(Views.RodenticideConsumption.RodenticideConsumptionHistoryPage));

        Routing.RegisterRoute(nameof(Views.DamageAssessment.DamageAssessmentPage), typeof(Views.DamageAssessment.DamageAssessmentPage));
        Routing.RegisterRoute(nameof(Views.DamageAssessment.DamageAssessmentPendingPage), typeof(Views.DamageAssessment.DamageAssessmentPendingPage));
        Routing.RegisterRoute(nameof(Views.DamageAssessment.DamageAssessmentHistoryPage), typeof(Views.DamageAssessment.DamageAssessmentHistoryPage));

        Routing.RegisterRoute(nameof(Views.IrrigationLine.IrrigationLinePage), typeof(Views.IrrigationLine.IrrigationLinePage));
        Routing.RegisterRoute(nameof(Views.IrrigationLine.IrrigationLinePendingPage), typeof(Views.IrrigationLine.IrrigationLinePendingPage));
        Routing.RegisterRoute(nameof(Views.IrrigationLine.IrrigationLineHistoryPage), typeof(Views.IrrigationLine.IrrigationLineHistoryPage));

        Routing.RegisterRoute("oneClickSync", typeof(OneClickSyncPage));
        Routing.RegisterRoute("oneClickUpload", typeof(OneClickUploadPage));
    }

    private async void OnShellNavigating(object sender, ShellNavigatingEventArgs e)
    {
        string targetRoute = e.Target.Location.OriginalString.ToLower();
        if (targetRoute == "//login" ||
            targetRoute == "//sync" ||
            targetRoute == "//loading" ||
            targetRoute == "//adminconfig")
        {
            return;
        }

        // Comprueba el permiso para la ruta de destino
        bool hasPermission = await _sessionService.CheckPermissionForRouteAsync(e.Target.Location.OriginalString);

        if (!hasPermission)
        {
            e.Cancel();
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.DisplayAlert("Acceso Denegado", "No tiene permiso para acceder a esta sección.", "OK");
            });
        }
    }

    private void AppShell_Navigated(object? sender, ShellNavigatedEventArgs e)
    {
        // No bloqueamos la navegación, sólo actualizamos la UI asíncronamente
        MainThread.BeginInvokeOnMainThread(async () => await ApplyPermissionsAsync());
    }

    private async Task ApplyPermissionsAsync()
    {
        try
        {
            var controlsToRoute = new Dictionary<string, string>
            {
                { "btnHome", "//home" },
                { "expMachinery", "//machineryUsage" },
                { "expRainfall", "//rainfall" },
                { "expRat", "//ratTrapping" },
                { "expRodenticide", "//rodenticideConsumption" },
                { "expDamage", "//damageAssessment" },
                { "expIrrigation", "//irrigationLine" } 
               
            };

            foreach (var kv in controlsToRoute)
            {
                var controlName = kv.Key;
                var route = kv.Value;

                bool hasPermission = await _sessionService.CheckPermissionForRouteAsync(route);

                // Busca el control por su nombre
                if (this.FindByName<object>(controlName) is VisualElement ve)
                {
                    ve.IsVisible = hasPermission;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ApplyPermissionsAsync error: {ex.Message}");
        }
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
            bool confirm = await Shell.Current.DisplayAlert("Cerrar Sesión", "¿Estás seguro que desea cerrar sesión?", "Sí", "No");
            if (!confirm) return;

            FlyoutIsPresented = false;

            // obtén los servicios como ya lo haces
            var serviceProvider = Handler?.MauiContext?.Services;
            var sessionSvc = serviceProvider?.GetService<SessionService>();
            var connectivitySvc = serviceProvider?.GetService<ConnectivityService>();

            if (sessionSvc != null)
            {
                await sessionSvc.ClearSessionAsync();
            }
            connectivitySvc?.StopMonitoring();

            // Navegar al login
            await Shell.Current.GoToAsync("//login");

            // REFRESCAR VISIBILIDADES inmediatamente después del logout
            await ApplyPermissionsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error OnLogoutClicked: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", "Error al cerrar sesión", "OK");
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

    // Event handler para los botones del Expander "GENERAL"
    private async void OnGeneralNavClicked(object sender, EventArgs e)
    {
        try
        {
            // Cerrar flyout inmediatamente para UI responsiva
            FlyoutIsPresented = false;

            if (sender is Button btn && btn.CommandParameter is string route && !string.IsNullOrWhiteSpace(route))
            {
                // Si la ruta comienza con "//" la tratamos como navegación absoluta, si no como relativa
                string navRoute = route.StartsWith("//") ? route : $"{route}";

                // Pequeña espera para que la animación del flyout termine (opcional)
                await Task.Delay(150);

                // Navegar a la ruta registrada
                await Shell.Current.GoToAsync(navRoute);
                System.Diagnostics.Debug.WriteLine($"? Navegación a: {navRoute}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("?? OnGeneralNavClicked: sender no es Button o no tiene CommandParameter válido.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"?? Error en OnGeneralNavClicked: {ex.Message}");
            // opcional: mostrar alerta
            await Shell.Current.DisplayAlert("Error", $"No se pudo navegar: {ex.Message}", "OK");
        }
    }

}