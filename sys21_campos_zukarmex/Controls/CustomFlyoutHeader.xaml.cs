namespace sys21_campos_zukarmex.Controls;

public partial class CustomFlyoutHeader : ContentView
{
    public CustomFlyoutHeader()
    {
        InitializeComponent();
        LoadUserInfo();
    }

    private async void LoadUserInfo()
    {
        try
        {
            // You can integrate with your SessionService here to show current user
            // For now, we'll show a placeholder
            await Task.Delay(100); // Simulate loading
            
            if (UserLabel != null)
            {
                UserLabel.Text = "Bienvenido";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading user info: {ex.Message}");
        }
    }
}