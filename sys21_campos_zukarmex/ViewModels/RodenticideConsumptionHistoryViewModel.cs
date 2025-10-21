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
        private readonly DatabaseService _databaseService;
        private readonly SessionService _sessionService;

        [ObservableProperty]
        private ObservableCollection<SalidaRodenticida> historyConsumptions = new();

        [ObservableProperty]
        private bool isRefreshing;

        public RodenticideConsumptionHistoryViewModel(ApiService apiService, DatabaseService databaseService, SessionService sessionService)
        {
            _apiService = apiService;
            _databaseService = databaseService; // <-- AÑADIDO
            _sessionService = sessionService;
            Title = "Historial de Consumos";
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

                var listFromApi = await _apiService.GetRodenticideHistoryAsync();

                foreach (var item in listFromApi)
                {
                    item.ZafraNombre = zafraList.FirstOrDefault(z => z.Id == item.IdTemporada)?.Nombre ?? "Zafra N/D";
                    item.CampoNombre = filteredCampos.FirstOrDefault(c => c.Id == item.IdCampo)?.Nombre ?? "Predio N/D";
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    HistoryConsumptions.Clear();
                    foreach (var item in listFromApi.OrderByDescending(i => i.Fecha))
                    {
                        HistoryConsumptions.Add(item);
                    }
                });

                if (!listFromApi.Any())
                {
                    await Shell.Current.DisplayAlert("Información", "No se encontraron registros en el historial.", "OK");
                }
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

        [RelayCommand]
        public async Task RefreshAsync()
        {
            await LoadHistoryAsync();
        }
    }
}
