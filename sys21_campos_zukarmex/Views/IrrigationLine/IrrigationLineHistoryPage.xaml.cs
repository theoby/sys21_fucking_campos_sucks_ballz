using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views.IrrigationLine;

public partial class IrrigationLineHistoryPage : ContentPage
{
    public IrrigationLineHistoryPage(IrrigationLineHistoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
      
    }
}
