using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class RodenticideConsumptionHistoryViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;

        [ObservableProperty]
        private ObservableCollection<SalidaRodenticida> historyConsumptions = new();

        [ObservableProperty]
        private bool isRefreshing;

        public RodenticideConsumptionHistoryViewModel(ApiService apiService)
        {
            _apiService = apiService;
            Title = "Historial de Consumos";
        }

        [RelayCommand]
        private async Task LoadHistoryAsync()
        {
            if (IsBusy) return;
            SetBusy(true);
            IsRefreshing = true;

            try
            {
                var list = await _apiService.GetRodenticideHistoryAsync();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    HistoryConsumptions.Clear();
                    foreach (var item in list) HistoryConsumptions.Add(item);
                });
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudo cargar el historial: {ex.Message}", "OK");
            }
            finally
            {
                SetBusy(false);
                IsRefreshing = false;
            }
        }
    }
}