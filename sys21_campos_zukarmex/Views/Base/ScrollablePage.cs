using sys21_campos_zukarmex.Extensions;

namespace sys21_campos_zukarmex.Views.Base;

/// <summary>
/// Base page optimizada para páginas que contienen ScrollView
/// Evita interferencias entre gestos de swipe y scroll
/// </summary>
public class ScrollablePage : ContentPage
{
    private bool _hasScrollOptimization = false;

    public ScrollablePage()
    {
        this.Appearing += OnScrollablePageAppearing;
    }

    private void OnScrollablePageAppearing(object? sender, EventArgs e)
    {
        if (!_hasScrollOptimization)
        {
            OptimizeScrollGestures();
            _hasScrollOptimization = true;
        }
    }

    /// <summary>
    /// Optimiza los gestos para pages con ScrollView
    /// </summary>
    private void OptimizeScrollGestures()
    {
        try
        {
            // En lugar de agregar gestos de pan que interfieren con scroll,
            // agregamos un área específica de swipe en el borde izquierdo
            this.AddDedicatedFlyoutSwipeArea(25); // 25 pixels desde el borde izquierdo
            
            System.Diagnostics.Debug.WriteLine("ScrollablePage: Área de swipe dedicada agregada");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error optimizando gestos de scroll: {ex.Message}");
        }
    }

    /// <summary>
    /// Crea un botón de menú hamburguesa específico para páginas scrollables
    /// </summary>
    protected Button CreateScrollFriendlyMenuButton(Color? iconColor = null)
    {
        var hamburgerButton = new Button
        {
            Text = "?", // Mejor icono de hamburguesa
            FontSize = 20,
            TextColor = iconColor ?? Colors.White,
            BackgroundColor = Colors.Transparent,
            Padding = new Thickness(10),
            WidthRequest = 50,
            HeightRequest = 50,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start
        };

        hamburgerButton.AddFlyoutTapGesture();
        return hamburgerButton;
    }

    /// <summary>
    /// Override para manejar cambios de estado del flyout
    /// </summary>
    protected virtual void OnFlyoutStateChanged(bool isOpen)
    {
        // Override en clases derivadas si es necesario
    }
}