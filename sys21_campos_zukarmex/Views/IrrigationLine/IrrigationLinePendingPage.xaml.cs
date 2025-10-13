using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views.IrrigationLine;

public partial class IrrigationLinePendingPage : ContentPage
{

    public IrrigationLinePendingPage(IrrigationLinePendingViewModel viewModel)
    {
       InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
    }
}
