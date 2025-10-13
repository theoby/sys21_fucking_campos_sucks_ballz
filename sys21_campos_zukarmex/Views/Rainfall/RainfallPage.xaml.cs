using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views.Rainfall;

public partial class RainfallPage : ContentPage
{
    public RainfallPage(RainfallViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is RainfallViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}