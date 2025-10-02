using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views;

[QueryProperty(nameof(Result), "Result")]
[QueryProperty(nameof(SalidaDetalle), "SalidaDetalle")]
public partial class AgregarArticuloPage : ContentPage
{
    public string Result { get; set; } = string.Empty;
    public object SalidaDetalle { get; set; } = new();

    public AgregarArticuloPage(AgregarArticuloViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is AgregarArticuloViewModel vm)
        {
            _ = vm.PageAppearingCommand.ExecuteAsync(null);
        }
    }
}