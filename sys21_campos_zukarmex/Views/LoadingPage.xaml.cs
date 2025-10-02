using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views;

public partial class LoadingPage : ContentPage
{
    public LoadingPage(LoadingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is LoadingViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}