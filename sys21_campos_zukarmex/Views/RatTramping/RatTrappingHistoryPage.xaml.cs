using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views.RatTrapping
{
    public partial class RatTrappingHistoryPage : ContentPage
    {
        private readonly RatTrappingHistoryViewModel _viewModel;

        public RatTrappingHistoryPage(RatTrappingHistoryViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel; 
            BindingContext = _viewModel;
        }

        
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_viewModel.PageAppearingCommand.CanExecute(null))
            {
                await _viewModel.PageAppearingCommand.ExecuteAsync(null);
            }
        }
    }
}