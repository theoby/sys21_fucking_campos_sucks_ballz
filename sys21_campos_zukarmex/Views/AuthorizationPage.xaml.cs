using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views;

public partial class AuthorizationPage : ContentPage
{
    public AuthorizationPage(AuthorizationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is AuthorizationViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}