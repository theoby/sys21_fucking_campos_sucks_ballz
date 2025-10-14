using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views.Rainfall
{
    public partial class RainfallPendingPage : ContentPage
    {
        private readonly RainfallPendingViewModel _viewModel;
        public RainfallPendingPage(RainfallPendingViewModel viewModel)
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