using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Linq;
using System.Threading.Tasks;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class MachineryUsagePendingViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly ApiService _apiService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPendingItems), nameof(PendingCount))]
        private ObservableCollection<SalidaMaquinaria> pendingMachineryUsages = new();

        public int PendingCount => PendingMachineryUsages.Count;
        public bool HasPendingItems => PendingMachineryUsages.Any();

        public MachineryUsagePendingViewModel(DatabaseService databaseService, ApiService apiService)
        {
            _databaseService = databaseService;
            _apiService = apiService;
            Title = "Usos Pendientes";
        }

        [RelayCommand]
        public async Task LoadPendingMachineryUsagesAsync()
        {
            if (IsBusy) return;
            SetBusy(true);
            try
            {
                var list = await _databaseService.GetAllAsync<SalidaMaquinaria>();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    PendingMachineryUsages.Clear();
                    foreach (var item in list.OrderByDescending(i => i.Fecha))
                    {
                        PendingMachineryUsages.Add(item);
                    }
                    OnPropertyChanged(nameof(PendingCount));
                    OnPropertyChanged(nameof(HasPendingItems));
                });
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudieron cargar los registros: {ex.Message}", "OK");
            }
            finally
            {
                SetBusy(false);
            }
        }

        [RelayCommand]
        public async Task SendAllPendingAsync()
        {
            if (!HasPendingItems || !await Shell.Current.DisplayAlert("Confirmar", $"Se enviarán {PendingCount} registros. ¿Desea continuar?", "Sí, Enviar", "No")) return;

            if (IsBusy) return;
            SetBusy(true);

            var itemsToSend = PendingMachineryUsages.ToList();
            try
            {
                var response = await _apiService.SendPendingMachineryUsageAsync(itemsToSend);

                if (response.Success)
                {
                    await _databaseService.DeleteListAsync(itemsToSend);
                    await Shell.Current.DisplayAlert("Éxito", $"Se enviaron {itemsToSend.Count} registros correctamente.", "OK");

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        PendingMachineryUsages.Clear();
                        OnPropertyChanged(nameof(PendingCount));
                        OnPropertyChanged(nameof(HasPendingItems));
                    });
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error de API", $"La API devolvió un error: {response.Message}", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fallo de Conexión", $"Error al sincronizar: {ex.Message}", "OK");
            }
            finally
            {
                SetBusy(false);
            }
        }

        [RelayCommand]
        public async Task DeleteMachineryUsagesAsync(SalidaMaquinaria use)
        {
            if (use == null || !await Shell.Current.DisplayAlert("Confirmar", $"¿Eliminar registro local ID: {use.Id}?", "Sí, Eliminar", "No")) return;

            SetBusy(true);
            try
            {
                var result = await _databaseService.DeleteAsync(use);
                if (result > 0)
                {
                    // Actualizar la UI en el hilo principal
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        PendingMachineryUsages.Remove(use);
                        OnPropertyChanged(nameof(PendingCount));
                        OnPropertyChanged(nameof(HasPendingItems));
                    });
                    
                    await Shell.Current.DisplayAlert("Éxito", $"Registro ID: {use.Id} eliminado.", "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", "No se pudo eliminar el registro de la base de datos.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudo eliminar: {ex.Message}", "OK");
            }
            finally
            {
                SetBusy(false);
            }
        }
    }
}