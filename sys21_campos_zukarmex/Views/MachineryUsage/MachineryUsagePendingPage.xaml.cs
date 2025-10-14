using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views.MachineryUsage;

public partial class MachineryUsagePendingPage : ContentPage
{
    public MachineryUsagePendingPage(MachineryUsagePendingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
    }
}
