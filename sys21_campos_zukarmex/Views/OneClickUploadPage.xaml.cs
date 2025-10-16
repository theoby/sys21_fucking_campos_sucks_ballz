using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views
{
    public partial class OneClickUploadPage : ContentPage
    {
        private readonly OneClickUploadViewModel _viewModel;
        public OneClickUploadPage(OneClickUploadViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadPendingCountsCommand.ExecuteAsync(null);
        }
    }
}