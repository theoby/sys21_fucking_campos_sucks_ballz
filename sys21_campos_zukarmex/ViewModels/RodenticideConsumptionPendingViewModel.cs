using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class RodenticideConsumptionPendingViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly ApiService _apiService;
        private readonly SessionService _sessionService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPendingItems), nameof(PendingCount))]
        private ObservableCollection<SalidaRodenticida> pendingConsumptions = new();

        [ObservableProperty]
        private bool isRefreshing;
        public int PendingCount => PendingConsumptions.Count;
        public bool HasPendingItems => PendingConsumptions.Any();

        public RodenticideConsumptionPendingViewModel(DatabaseService databaseService, ApiService apiService, SessionService sessionService)
        {
            _databaseService = databaseService;
            _apiService = apiService;
            _sessionService = sessionService;
            Title = "Consumos Pendientes";
        }

        [RelayCommand]
        public async Task LoadPendingAsync()
        {
            if (IsBusy) return;
            SetBusy(true);
            try
            {
                var session = await _sessionService.GetCurrentSessionAsync();

                var zafraList = await _databaseService.GetAllAsync<Zafra>();
                var allCampos = await _databaseService.GetAllAsync<Campo>();
                var filteredCampos = session.TipoUsuario == 1 ? allCampos : allCampos.Where(c => c.IdInspector == session.IdInspector).ToList();

                var list = await _databaseService.GetAllAsync<SalidaRodenticida>();

                foreach (var item in list)
                {
                    item.ZafraNombre = zafraList.FirstOrDefault(z => z.Id == item.IdTemporada)?.Nombre ?? "Zafra N/D";
                    item.CampoNombre = filteredCampos.FirstOrDefault(c => c.Id == item.IdCampo)?.Nombre ?? "Predio N/D";
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    PendingConsumptions.Clear();
                    foreach (var item in list.OrderByDescending(i => i.Fecha)) PendingConsumptions.Add(item);
                    OnPropertyChanged(nameof(PendingCount));
                    OnPropertyChanged(nameof(HasPendingItems));
                });
            }
            catch (Exception ex) { await Shell.Current.DisplayAlert("Error", $"No se pudieron cargar registros: {ex.Message}", "OK"); }
            finally { SetBusy(false); }
        }

        [RelayCommand]
        public async Task SendAllPendingAsync()
        {
            if (!HasPendingItems)
            {
                await Shell.Current.DisplayAlert("Sincronización", "No hay consumos pendientes para enviar.", "OK");
                return;
            }
            if (!await Shell.Current.DisplayAlert("Confirmar", $"Se enviarán {PendingCount} registros. ¿Desea continuar?", "Sí, Enviar", "Cancelar")) return;

            if (IsBusy) return;
            SetBusy(true);

            var itemsToSend = PendingConsumptions.ToList();
            try
            {
                var response = await _apiService.SendPendingRodenticideConsumptionsAsync(itemsToSend);

                if (response.Success)
                {
                    await _databaseService.DeleteListAsync(itemsToSend);
                    await Shell.Current.DisplayAlert("Éxito", $"Se enviaron {itemsToSend.Count} registros.", "OK");
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        PendingConsumptions.Clear();
                        OnPropertyChanged(nameof(PendingCount));
                    });
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error de API", $"La API devolvió un error: {response.Message}", "OK");
                }
            }
            catch (Exception ex) { await Shell.Current.DisplayAlert("Error", $"Fallo al sincronizar: {ex.Message}", "OK"); }
            finally { SetBusy(false); }
        }

        [RelayCommand]
        public async Task DeleteAsync(SalidaRodenticida consumption)
        {
            if (consumption == null || !await Shell.Current.DisplayAlert("Confirmar", $"¿Eliminar registro local ID: {consumption.Id}?", "Sí", "No")) return;

            SetBusy(true);
            try
            {
                var result = await _databaseService.DeleteAsync(consumption);
                if (result > 0)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        PendingConsumptions.Remove(consumption);
                        OnPropertyChanged(nameof(PendingCount));
                    });
                }
            }
            catch (Exception ex) { await Shell.Current.DisplayAlert("Error", $"No se pudo eliminar: {ex.Message}", "OK"); }
            finally { SetBusy(false); }
        }
        [RelayCommand]
        public async Task RefreshAsync()
        {
            if (IsBusy) return;

            try
            {
                IsRefreshing = true;
                await LoadPendingAsync();
            }
            finally
            {
                IsRefreshing = false;
            }
        }
    }
}
