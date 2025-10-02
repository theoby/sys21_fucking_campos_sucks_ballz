using Microsoft.Maui.Controls;

namespace sys21_campos_zukarmex.Extensions;

/// <summary>
/// Extension methods to add swipe-to-open flyout functionality to any page
/// </summary>
public static class FlyoutGestureExtensions
{
    /// <summary>
    /// Adds a swipe gesture to open the flyout menu from the left edge of the page
    /// </summary>
    /// <param name="page">The page to add the gesture to</param>
    /// <param name="swipeThreshold">The distance from the left edge where the swipe should trigger (default: 50)</param>
    public static void AddFlyoutSwipeGesture(this ContentPage page, double swipeThreshold = 50)
    {
        var panGestureRecognizer = new PanGestureRecognizer();
        panGestureRecognizer.PanUpdated += (sender, e) =>
        {
            HandleFlyoutPanGesture(e, swipeThreshold);
        };
        
        // Add to the main content's gesture recognizers, but check if it's a ScrollView
        if (page.Content != null)
        {
            // If the content is a ScrollView, skip adding pan gesture to avoid scroll interference
            if (page.Content is ScrollView)
            {
                System.Diagnostics.Debug.WriteLine("ScrollView detected - Skipping pan gesture to avoid scroll interference");
                return;
            }
            
            page.Content.GestureRecognizers.Add(panGestureRecognizer);
        }
    }

    /// <summary>
    /// Adds a tap gesture to toggle the flyout menu
    /// </summary>
    /// <param name="view">The view to add the tap gesture to (usually a hamburger menu button)</param>
    public static void AddFlyoutTapGesture(this View view)
    {
        var tapGestureRecognizer = new TapGestureRecognizer();
        tapGestureRecognizer.Tapped += (sender, e) =>
        {
            ToggleFlyout();
        };
        
        view.GestureRecognizers.Add(tapGestureRecognizer);
    }

    /// <summary>
    /// Creates a dedicated swipe area for flyout gesture without interfering with page content
    /// </summary>
    /// <param name="page">The page to add the swipe area to</param>
    /// <param name="areaWidth">Width of the swipe area from the left edge (default: 20)</param>
    public static void AddDedicatedFlyoutSwipeArea(this ContentPage page, double areaWidth = 20)
    {
        try
        {
            if (page.Content != null)
            {
                // Create a grid to overlay the swipe area
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(areaWidth, GridUnitType.Absolute) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Create invisible swipe area
                var swipeArea = new BoxView 
                { 
                    BackgroundColor = Colors.Transparent,
                    InputTransparent = false
                };

                var panGesture = new PanGestureRecognizer();
                panGesture.PanUpdated += (sender, e) => HandleFlyoutPanGesture(e, areaWidth);
                swipeArea.GestureRecognizers.Add(panGesture);

                // Add the original content to the second column
                var originalContent = page.Content;
                grid.Children.Add(swipeArea);
                Grid.SetColumn(swipeArea, 0);
                
                grid.Children.Add(originalContent);
                Grid.SetColumn(originalContent, 1);

                page.Content = grid;
                
                System.Diagnostics.Debug.WriteLine($"Dedicated swipe area added: {areaWidth}px wide");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding dedicated swipe area: {ex.Message}");
        }
    }

    private static void HandleFlyoutPanGesture(PanUpdatedEventArgs e, double swipeThreshold)
    {
        try
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    // Check if the gesture started near the left edge
                    if (e.TotalX < swipeThreshold)
                    {
                        // Prepare for potential flyout opening
                    }
                    break;

                case GestureStatus.Running:
                    // Check if user is swiping right from the left edge
                    // Make it more restrictive to avoid interfering with vertical scrolling
                    if (e.TotalX > 100 && Math.Abs(e.TotalY) < 30) // Reduced vertical tolerance
                    {
                        // Visual feedback could be added here (like showing a glimpse of the flyout)
                    }
                    break;

                case GestureStatus.Completed:
                    // If swipe was significant enough and mostly horizontal, open the flyout
                    if (e.TotalX > 120 && Math.Abs(e.TotalY) < 50 && !Shell.Current.FlyoutIsPresented)
                    {
                        // More restrictive requirements for horizontal swipe
                        Shell.Current.FlyoutIsPresented = true;
                    }
                    break;

                case GestureStatus.Canceled:
                    // Handle gesture cancellation if needed
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error handling flyout pan gesture: {ex.Message}");
        }
    }

    private static void ToggleFlyout()
    {
        try
        {
            Shell.Current.FlyoutIsPresented = !Shell.Current.FlyoutIsPresented;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error toggling flyout: {ex.Message}");
        }
    }

    /// <summary>
    /// Opens the flyout menu programmatically
    /// </summary>
    public static void OpenFlyout()
    {
        try
        {
            Shell.Current.FlyoutIsPresented = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening flyout: {ex.Message}");
        }
    }

    /// <summary>
    /// Closes the flyout menu programmatically
    /// </summary>
    public static void CloseFlyout()
    {
        try
        {
            Shell.Current.FlyoutIsPresented = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error closing flyout: {ex.Message}");
        }
    }
}