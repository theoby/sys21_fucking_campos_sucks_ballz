using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views.MachineryUsage;

public partial class MachineryUsagePage : ContentPage
{
    public MachineryUsagePage(MachineryUsageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is MachineryUsageViewModel vm)
            await vm.InitializeAsync();
    }
}