using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views.MachineryUsage 
{
    public partial class MachineryUsagePendingPage : ContentPage
    {
        private readonly MachineryUsagePendingViewModel _viewModel;

        public MachineryUsagePendingPage(MachineryUsagePendingViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_viewModel.LoadPendingMachineryUsagesCommand.CanExecute(null))
            {
                await _viewModel.LoadPendingMachineryUsagesCommand.ExecuteAsync(null);
            }
        }
    }
}