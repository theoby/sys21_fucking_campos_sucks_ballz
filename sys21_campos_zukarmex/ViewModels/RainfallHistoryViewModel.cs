// EN: ViewModels/RainfallHistoryViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic; 
using System.Threading.Tasks;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class RainfallHistoryViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;
        private readonly DatabaseService _databaseService;

        [ObservableProperty]
        private ObservableCollection<SalidaPrecipitacion> historyRainfalls = new();

        [ObservableProperty]
        private bool isRefreshing;

        public RainfallHistoryViewModel(ApiService apiService, DatabaseService databaseService)
        {
            _apiService = apiService;
            _databaseService = databaseService;
            Title = "Historial Pluvial";
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
                var pluviometroList = await _databaseService.GetAllAsync<Pluviometro>();

                var listFromApi = await _apiService.GetRainfallHistoryAsync();

                foreach (var item in listFromApi)
                {
                    item.PluviometroNombre = pluviometroList.FirstOrDefault(p => p.Id == item.IdPluviometro)?.Nombre ?? "Pluviómetro N/D";
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    HistoryRainfalls.Clear();
                    foreach (var item in listFromApi)
                    {
                        HistoryRainfalls.Add(item);
                    }
                });

                if (!listFromApi.Any())
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
            await LoadHistoryAsync();
        }
    }
}