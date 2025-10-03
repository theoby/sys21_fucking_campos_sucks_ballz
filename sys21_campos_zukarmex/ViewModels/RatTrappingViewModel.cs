using CommunityToolkit.Mvvm.ComponentModel;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class RatTrappingViewModel : ObservableObject
    {
        [ObservableProperty]
        private string title = "Trampeo de Ratas";


        public RatTrappingViewModel()
        {
           
        }
    }
}