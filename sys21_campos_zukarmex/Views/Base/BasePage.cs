using sys21_campos_zukarmex.Extensions;

namespace sys21_campos_zukarmex.Views.Base;

/// <summary>
/// Base page class that provides common functionality including flyout gestures
/// </summary>
public class BasePage : ContentPage
{
    private bool _hasSwipeGesture = false;

    public BasePage()
    {
        // Add swipe gesture support when the page appears
        this.Appearing += OnPageAppearing;
    }

    private void OnPageAppearing(object? sender, EventArgs e)
    {
        // Add swipe gesture only once
        if (!_hasSwipeGesture)
        {
            this.AddFlyoutSwipeGesture();
            _hasSwipeGesture = true;
        }
    }

    /// <summary>
    /// Creates a hamburger menu button that can be added to any page
    /// </summary>
    /// <param name="iconColor">The color of the hamburger icon</param>
    /// <returns>A button that toggles the flyout menu</returns>
    protected Button CreateHamburgerMenuButton(Color? iconColor = null)
    {
        var hamburgerButton = new Button
        {
            Text = "?", // Hamburger icon
            FontSize = 20,
            TextColor = iconColor ?? Colors.White,
            BackgroundColor = Colors.Transparent,
            Padding = new Thickness(10),
            WidthRequest = 50,
            HeightRequest = 50
        };

        hamburgerButton.AddFlyoutTapGesture();
        return hamburgerButton;
    }

    /// <summary>
    /// Creates a hamburger menu image button
    /// </summary>
    /// <param name="iconSource">The source of the hamburger icon image</param>
    /// <returns>An ImageButton that toggles the flyout menu</returns>
    protected ImageButton CreateHamburgerMenuImageButton(string iconSource = "hamburger_menu.png")
    {
        var hamburgerImageButton = new ImageButton
        {
            Source = iconSource,
            BackgroundColor = Colors.Transparent,
            Padding = new Thickness(10),
            WidthRequest = 40,
            HeightRequest = 40
        };

        hamburgerImageButton.AddFlyoutTapGesture();
        return hamburgerImageButton;
    }

    /// <summary>
    /// Override this method to customize the page behavior when flyout opens/closes
    /// </summary>
    /// <param name="isOpen">True if flyout is opening, false if closing</param>
    protected virtual void OnFlyoutStateChanged(bool isOpen)
    {
        // Override in derived classes to handle flyout state changes
    }
}