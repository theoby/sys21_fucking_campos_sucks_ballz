using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views.RodenticideConsumption
{
    public partial class RodenticideConsumptionPage : ContentPage
    {
        public RodenticideConsumptionPage(RodenticideConsumptionViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is RodenticideConsumptionViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }
    }
}