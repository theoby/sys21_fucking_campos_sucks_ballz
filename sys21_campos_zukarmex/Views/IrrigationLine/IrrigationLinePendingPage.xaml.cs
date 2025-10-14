using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views.IrrigationLine
{
    public partial class IrrigationLinePendingPage : ContentPage
    {
        private readonly IrrigationLinePendingViewModel _viewModel;
        public IrrigationLinePendingPage(IrrigationLinePendingViewModel viewModel)
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