using sys21_campos_zukarmex.Views.Base;

namespace sys21_campos_zukarmex.Views;

public partial class NavigationDemoPage : BasePage
{
    public NavigationDemoPage()
    {
        InitializeComponent();
        Title = "Guía de Navegación";
    }

    private void OnToggleMenuClicked(object sender, EventArgs e)
    {
        // Toggle the flyout menu to demonstrate
        Shell.Current.FlyoutIsPresented = !Shell.Current.FlyoutIsPresented;
    }

    private async void OnGoBackClicked(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("//home");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error navigating back: {ex.Message}");
        }
    }
}