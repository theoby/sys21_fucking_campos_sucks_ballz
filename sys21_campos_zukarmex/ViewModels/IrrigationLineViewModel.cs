using CommunityToolkit.Mvvm.ComponentModel;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class IrrigationLineViewModel : ObservableObject
    {
        [ObservableProperty]
        private string title = "Línea de Riego";

        public IrrigationLineViewModel()
        {
           
        }
    }
}