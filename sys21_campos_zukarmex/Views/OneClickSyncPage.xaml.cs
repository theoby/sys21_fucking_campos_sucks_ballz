using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views
{
    public partial class OneClickSyncPage : ContentPage
    {
        public OneClickSyncPage(OneClickSyncViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}