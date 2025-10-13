using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views.DamageAssessment;

public partial class DamageAssessmentPendingPage : ContentPage
{
    public DamageAssessmentPendingPage(DamageAssessmentPendingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
    }
}
