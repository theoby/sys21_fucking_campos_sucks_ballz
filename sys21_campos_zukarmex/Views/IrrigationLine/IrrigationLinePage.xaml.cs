namespace sys21_campos_zukarmex.Views.IrrigationLine;
using sys21_campos_zukarmex.ViewModels;

public partial class IrrigationLinePage : ContentPage
{
	public IrrigationLinePage(IrrigationLineViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is IrrigationLineViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}