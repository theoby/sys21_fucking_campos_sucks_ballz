using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views;

public partial class StatusPage : ContentPage
{
    public StatusPage(StatusViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is StatusViewModel vm)
        {
            // Llamamos al nuevo comando que se encarga de la carga inicial
            _ = vm.PageAppearingCommand.ExecuteAsync(null);
        }
    }
}