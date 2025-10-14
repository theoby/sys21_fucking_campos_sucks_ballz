using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views.RodenticideConsumption
{
    public partial class RodenticideConsumptionPendingPage : ContentPage
    {
        private readonly RodenticideConsumptionPendingViewModel _viewModel;
        public RodenticideConsumptionPendingPage(RodenticideConsumptionPendingViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadPendingCommand.ExecuteAsync(null);
        }
    }
}