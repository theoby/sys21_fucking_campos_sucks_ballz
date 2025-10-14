using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views.RodenticideConsumption
{
    public partial class RodenticideConsumptionHistoryPage : ContentPage
    {
        private readonly RodenticideConsumptionHistoryViewModel _viewModel;
        public RodenticideConsumptionHistoryPage(RodenticideConsumptionHistoryViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadHistoryCommand.ExecuteAsync(null);
        }
    }
}