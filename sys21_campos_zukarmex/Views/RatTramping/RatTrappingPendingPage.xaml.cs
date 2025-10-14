using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views.RatTrapping
{
    public partial class RatTrappingPendingPage : ContentPage
    {
        private readonly RatTrappingPendingViewModel _viewModel;

        public RatTrappingPendingPage(RatTrappingPendingViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_viewModel.LoadPendingCapturesCommand.CanExecute(null))
            {
                await _viewModel.LoadPendingCapturesCommand.ExecuteAsync(null);
            }
        }
    }
}