using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class IrrigationLinePendingViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly ApiService _apiService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPendingItems), nameof(PendingCount))]
        private ObservableCollection<SalidaLineaDeRiego> pendingEntries = new();

        public int PendingCount => PendingEntries.Count;
        public bool HasPendingItems => PendingEntries.Any();

        public IrrigationLinePendingViewModel(DatabaseService databaseService, ApiService apiService)
        {
            _databaseService = databaseService;
            _apiService = apiService;
            Title = "Riegos Pendientes";
        }

        [RelayCommand]
        public async Task LoadPendingAsync()
        {
            if (IsBusy) return;
            SetBusy(true);
            try
            {
                var list = await _databaseService.GetAllAsync<SalidaLineaDeRiego>();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    PendingEntries.Clear();
                    foreach (var item in list.OrderByDescending(i => i.Fecha)) PendingEntries.Add(item);
                    OnPropertyChanged(nameof(PendingCount));
                });
            }
            catch (Exception ex) { await Shell.Current.DisplayAlert("Error", $"Error al cargar: {ex.Message}", "OK"); }
            finally { SetBusy(false); }
        }

        [RelayCommand]
        public async Task SendAllPendingAsync()
        {
            if (!HasPendingItems || !await Shell.Current.DisplayAlert("Confirmar", $"Enviar {PendingCount} registros?", "Sí", "No")) return;
            if (IsBusy) return;
            SetBusy(true);

            var itemsToSend = PendingEntries.ToList();
            try
            {
                var response = await _apiService.SendPendingIrrigationEntriesAsync(itemsToSend);
                if (response.Success)
                {
                    await _databaseService.DeleteListAsync(itemsToSend);
                    await Shell.Current.DisplayAlert("Éxito", $"Se enviaron {itemsToSend.Count} registros.", "OK");
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        PendingEntries.Clear();
                        OnPropertyChanged(nameof(PendingCount));
                    });
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error de API", response.Message, "OK");
                }
            }
            catch (Exception ex) { await Shell.Current.DisplayAlert("Error", $"Fallo al sincronizar: {ex.Message}", "OK"); }
            finally { SetBusy(false); }
        }

        [RelayCommand]
        public async Task DeleteAsync(SalidaLineaDeRiego entry)
        {
            if (entry == null || !await Shell.Current.DisplayAlert("Confirmar", $"¿Eliminar registro ID: {entry.Id}?", "Sí", "No")) return;
            SetBusy(true);
            try
            {
                if (await _databaseService.DeleteAsync(entry) > 0)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        PendingEntries.Remove(entry);
                        OnPropertyChanged(nameof(PendingCount));
                    });
                }
            }
            catch (Exception ex) { await Shell.Current.DisplayAlert("Error", $"No se pudo eliminar: {ex.Message}", "OK"); }
            finally { SetBusy(false); }
        }
    }
}