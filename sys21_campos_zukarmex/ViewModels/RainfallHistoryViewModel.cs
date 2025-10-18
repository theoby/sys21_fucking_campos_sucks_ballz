// EN: ViewModels/RainfallHistoryViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class RainfallHistoryViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;

        [ObservableProperty]
        private ObservableCollection<SalidaPrecipitacion> historyRainfalls = new();

        [ObservableProperty]
        private bool isRefreshing;

        public RainfallHistoryViewModel(ApiService apiService)
        {
            _apiService = apiService;
            Title = "Historial Pluvial";
        }

        [RelayCommand]
        private async Task PageAppearingAsync()
        {
            // Llama al método de carga para evitar duplicar código
            await LoadHistoryAsync();
        }

        [RelayCommand]
        private async Task LoadHistoryAsync()
        {
            if (IsBusy) return;
            SetBusy(true);
            IsRefreshing = true;

            try
            {
                var list = await _apiService.GetRainfallHistoryAsync();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    HistoryRainfalls.Clear();
                    foreach (var item in list)
                    {
                        HistoryRainfalls.Add(item);
                    }
                });

                if (!list.Any())
                {
                    await Shell.Current.DisplayAlert("Información", "No se encontraron registros en el historial.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error de Carga", $"No se pudo cargar el historial: {ex.Message}", "OK");
            }
            finally
            {
                SetBusy(false);
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            if (IsBusy) return;

            try
            {
                IsRefreshing = true;
                await LoadHistoryAsync();
            }
            finally
            {
                IsRefreshing = false;
            }
        }
    }
}