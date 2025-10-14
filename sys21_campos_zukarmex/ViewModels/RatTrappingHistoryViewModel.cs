using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class RatTrappingHistoryViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;

        [ObservableProperty]
        private ObservableCollection<SalidaTrampeoRatas> historyCaptures = new();

        [ObservableProperty]
        private bool isRefreshing; // Para el RefreshView

        public RatTrappingHistoryViewModel(ApiService apiService)
        {
            _apiService = apiService;
            Title = "Historial de Trampeos";
        }

        [RelayCommand]
        private async Task PageAppearingAsync()
        {
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
                var list = await _apiService.GetRatTrappingHistoryAsync();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    HistoryCaptures.Clear();
                    foreach (var item in list)
                    {
                        HistoryCaptures.Add(item);
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
    }
}