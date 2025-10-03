using CommunityToolkit.Mvvm.ComponentModel;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class RodenticideConsumptionViewModel : ObservableObject
    {
        [ObservableProperty]
        private string title = "Consumo de Rodenticida";

        public RodenticideConsumptionViewModel()
        {
          
        }
    }
}