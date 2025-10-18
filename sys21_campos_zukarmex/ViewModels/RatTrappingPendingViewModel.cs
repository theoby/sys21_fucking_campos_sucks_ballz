using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class RatTrappingPendingViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly ApiService _apiService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPendingItems))]
        [NotifyPropertyChangedFor(nameof(PendingCount))]
        private ObservableCollection<SalidaTrampeoRatas> pendingCaptures;

        [ObservableProperty]
        private bool isRefreshing;
        public int PendingCount => PendingCaptures?.Count ?? 0;
        public bool HasPendingItems => PendingCaptures?.Any() ?? false;

        public RatTrappingPendingViewModel(DatabaseService databaseService, ApiService apiService)
        {
            _databaseService = databaseService;
            _apiService = apiService;
            PendingCaptures = new ObservableCollection<SalidaTrampeoRatas>();
            Title = "Trampeos Pendientes";
        }

        [RelayCommand]
        public async Task LoadPendingCapturesAsync()
        {
            if (IsBusy) return;
            SetBusy(true);
            try
            {
                var list = await _databaseService.GetAllAsync<SalidaTrampeoRatas>();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    PendingCaptures.Clear();
                    foreach (var item in list.OrderByDescending(i => i.Fecha))
                    {
                        PendingCaptures.Add(item);
                    }

                    // También notificamos explícitamente a las propiedades dependientes
                    OnPropertyChanged(nameof(PendingCount));
                    OnPropertyChanged(nameof(HasPendingItems));
                });
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error de Carga", $"No se pudieron cargar los registros: {ex.Message}", "OK");
            }
            finally
            {
                SetBusy(false);
            }
        }

        [RelayCommand]
        public async Task SendAllPendingAsync()
        {
            if (!HasPendingItems)
            {
                await Shell.Current.DisplayAlert("Sincronización", "No hay trampeos pendientes para enviar.", "OK");
                return;
            }

            if (!await Shell.Current.DisplayAlert("Confirmar Sincronización", $"Se enviarán {PendingCount} registros. ¿Desea continuar?", "Sí, Enviar", "Cancelar"))
            {
                return;
            }

            if (IsBusy) return;
            SetBusy(true);

            var itemsToSend = PendingCaptures.ToList();
            try
            {
                var response = await _apiService.SendPendingRatCapturesAsync(itemsToSend);

                if (response.Success)
                {
                    await _databaseService.DeleteListAsync(itemsToSend);

                    await Shell.Current.DisplayAlert("Éxito", $"Se enviaron {itemsToSend.Count} registros correctamente.", "OK");

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        PendingCaptures.Clear();
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
        public async Task DeleteCaptureAsync(SalidaTrampeoRatas capture)
        {
            if (capture == null) return;
            if (!await Shell.Current.DisplayAlert("Confirmar Eliminación", $"¿Seguro que desea eliminar el registro local ID: {capture.Id}?", "Sí, Eliminar", "Cancelar"))
            {
                return;
            }

            SetBusy(true);
            try
            {
                var result = await _databaseService.DeleteAsync(capture);

                if (result > 0)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        PendingCaptures.Remove(capture);
                        // Notificar que el contador ha cambiado
                        OnPropertyChanged(nameof(PendingCount));
                        OnPropertyChanged(nameof(HasPendingItems));
                    });
                    await Shell.Current.DisplayAlert("Éxito", $"Registro ID: {capture.Id} eliminado localmente.", "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", "No se pudo eliminar el registro de la base de datos.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudo eliminar el registro: {ex.Message}", "OK");
            }
            finally
            {
                SetBusy(false);
            }
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            if (IsBusy) return;

            try
            {
                IsRefreshing = true;
                await LoadPendingCapturesAsync();
            }
            finally
            {
                IsRefreshing = false;
            }
        }
    }
}