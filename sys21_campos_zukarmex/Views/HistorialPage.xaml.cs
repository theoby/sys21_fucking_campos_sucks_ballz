using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views;

public partial class HistorialPage : ContentPage
{
    public HistorialPage(HistorialViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is HistorialViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}