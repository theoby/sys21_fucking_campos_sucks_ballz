using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views;

public partial class DamageAssessmentPage : ContentPage
{
    public DamageAssessmentPage(DamageAssessmentViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is DamageAssessmentViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
