using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views;

public partial class ValePage : ContentPage
{
    public ValePage(ValeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ValeViewModel viewModel)
        {
            await viewModel.PageAppearingCommand.ExecuteAsync(null);
        }
    }
}