using System.Diagnostics;
using sys21_campos_zukarmex.ViewModels;

namespace sys21_campos_zukarmex.Views.RatTrapping
{
    [QueryProperty(nameof(RecordId), "recordId")]
    public partial class RatTrappingPage : ContentPage
    {
        public RatTrappingPage(RatTrappingViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        private string _recordId;
        public string RecordId
        {
            get => _recordId;
            set
            {
                _recordId = value;
                Debug.WriteLine($"[Page] RecordId setter invoked with value = '{value}'");
                OnRecordIdChanged(value);
            }
        }
        private async void OnRecordIdChanged(string value)
        {
            Debug.WriteLine($"[Page] OnRecordIdChanged called with '{value}'");

            if (BindingContext is RatTrappingViewModel vm)
            {
                Debug.WriteLine("[Page] BindingContext is RatTrappingViewModel, attempting to parse id...");
                if (int.TryParse(value, out var id))
                {
                    Debug.WriteLine($"[Page] Parsed id = {id}. Calling vm.LoadCaptureForEditAsync...");
                    await vm.LoadCaptureForEditAsync(id);
                    Debug.WriteLine($"[Page] BindingContextChanged fired. BindingContext is now {BindingContext?.GetType().Name ?? "null"}");
                    Debug.WriteLine("[Page] vm.LoadCaptureForEditAsync returned.");
                }
                else
                {
                    Debug.WriteLine($"[Page] Failed to parse recordId '{value}' as int.");
                }
            }
            else
            {
                Debug.WriteLine($"[Page] BindingContext is NOT RatTrappingViewModel (it is {BindingContext?.GetType().Name ?? "null"})");
            }
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