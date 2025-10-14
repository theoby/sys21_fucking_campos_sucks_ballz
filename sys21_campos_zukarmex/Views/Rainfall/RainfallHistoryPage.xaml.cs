using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views.Rainfall
{
    public partial class RainfallHistoryPage : ContentPage
    {
        private readonly RainfallHistoryViewModel _viewModel;
        public RainfallHistoryPage(RainfallHistoryViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // Usamos el comando que definimos en el ViewModel, que ahora es LoadHistoryCommand
            if (_viewModel.LoadHistoryCommand.CanExecute(null))
            {
                await _viewModel.LoadHistoryCommand.ExecuteAsync(null);
            }
        }
    }
}