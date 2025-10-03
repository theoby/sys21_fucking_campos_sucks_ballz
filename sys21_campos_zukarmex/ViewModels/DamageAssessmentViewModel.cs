using CommunityToolkit.Mvvm.ComponentModel;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class DamageAssessmentViewModel : ObservableObject
    {
        [ObservableProperty]
        private string title = "Muestreo de Daño";

        public DamageAssessmentViewModel()
        {
            
        }
    }
}