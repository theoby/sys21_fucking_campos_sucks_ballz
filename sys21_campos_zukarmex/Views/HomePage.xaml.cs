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
}