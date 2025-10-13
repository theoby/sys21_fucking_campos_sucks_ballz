using sys21_campos_zukarmex.ViewModels;
using sys21_campos_zukarmex.Views.Base;

namespace sys21_campos_zukarmex.Views;

public partial class HomePage : ScrollablePage
{
    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        
        // Customize the page for the new navigation system
        Title = "Inicio";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is HomeViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }

    protected override void OnFlyoutStateChanged(bool isOpen)
    {
        // You can add custom behavior when the flyout opens/closes
        System.Diagnostics.Debug.WriteLine($"HomePage: Flyout is now {(isOpen ? "open" : "closed")}");
    }

    // Event handlers for quick action buttons
    private async void OnNewValeClicked(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("//vale");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error navigating to vale: {ex.Message}");
        }
    }

    private async void OnStatusClicked(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("//status");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error navigating to status: {ex.Message}");
        }
    }

    private async void OnMaquinariaClicked(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("//vale");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error navigating to maquinaria: {ex.Message}");
        }
    }

    private async void OnConfigClicked(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("//adminconfig");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error navigating to config: {ex.Message}");
        }
    }

    private async void OnGeneralNavClicked(object sender, EventArgs e)
    {
        // 1. Verificar si el remitente es un Botón
        if (sender is Button button)
        {
            // 2. Intentar obtener el CommandParameter, que contiene la ruta de navegación
            if (button.CommandParameter is string route)
            {
                // 3. Verificar que la ruta no esté vacía
                if (!string.IsNullOrWhiteSpace(route))
                {
                    try
                    {
                        // Ejecuta la navegación a la ruta definida en el CommandParameter
                        await Shell.Current.GoToAsync(route);
                        Console.WriteLine($"Navegación exitosa a: {route}");
                    }
                    catch (Exception ex)
                    {
                        // Muestra un error si la ruta no existe o no se puede navegar
                        Console.WriteLine($"ERROR DE NAVEGACIÓN: No se pudo navegar a {route}. Mensaje: {ex.Message}");
                        // Aquí podrías mostrar un aviso al usuario si quieres
                    }
                }
            }
        }
    }
}