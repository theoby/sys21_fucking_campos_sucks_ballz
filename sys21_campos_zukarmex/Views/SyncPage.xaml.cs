using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views;

public partial class SyncPage : ContentPage
{
    public SyncPage(SyncViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is SyncViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}