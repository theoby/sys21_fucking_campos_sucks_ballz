using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views.RatTrapping
{
    public partial class RatTrappingPage : ContentPage
    {
        public RatTrappingPage(RatTrappingViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is RatTrappingViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }
    }
}