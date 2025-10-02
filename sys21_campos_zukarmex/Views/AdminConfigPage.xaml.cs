using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views;

public partial class AdminConfigPage : ContentPage
{
    public AdminConfigPage(AdminConfigViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is AdminConfigViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}