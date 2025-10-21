using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class RatTrappingHistoryViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;
        private readonly DatabaseService _databaseService; 
        private readonly SessionService _sessionService;

        [ObservableProperty]
        private ObservableCollection<SalidaTrampeoRatas> historyCaptures = new();

        [ObservableProperty]
        private bool isRefreshing; 

        public RatTrappingHistoryViewModel(ApiService apiService, DatabaseService databaseService, SessionService sessionService)
        {
            _apiService = apiService;
            _databaseService = databaseService;
            _sessionService = sessionService;
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
                var session = await _sessionService.GetCurrentSessionAsync();
                var zafraList = await _databaseService.GetAllAsync<Zafra>();
                var allCampos = await _databaseService.GetAllAsync<Campo>();
                var filteredCampos = session.TipoUsuario == 1 ? allCampos : allCampos.Where(c => c.IdInspector == session.IdInspector).ToList();

                // 2. Cargar Historial de la API
                var listFromApi = await _apiService.GetRatTrappingHistoryAsync();

                foreach (var item in listFromApi)
                {
                    item.ZafraNombre = zafraList.FirstOrDefault(z => z.Id == item.IdTemporada)?.Nombre ?? "Zafra N/D";
                    item.CampoNombre = filteredCampos.FirstOrDefault(c => c.Id == item.IdCampo)?.Nombre ?? "Predio N/D";
                }
;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    HistoryCaptures.Clear();
                    foreach (var item in listFromApi.OrderByDescending(i => i.Fecha))
                    {
                        HistoryCaptures.Add(item);
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