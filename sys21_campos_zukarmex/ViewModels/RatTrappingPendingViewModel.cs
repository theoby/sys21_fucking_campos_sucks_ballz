using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.Mvvm.Messaging;
using System.Diagnostics;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class RatTrappingPendingViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly ApiService _apiService;
        private readonly SessionService _sessionService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPendingItems))]
        [NotifyPropertyChangedFor(nameof(PendingCount))]
        private ObservableCollection<SalidaTrampeoRatas> pendingCaptures = new();

        [ObservableProperty]
        private bool isRefreshing;
        public int PendingCount => PendingCaptures?.Count ?? 0;
        public bool HasPendingItems => PendingCaptures?.Any() ?? false;



        public RatTrappingPendingViewModel(DatabaseService databaseService, ApiService apiService, SessionService sessionService)
        {
            _databaseService = databaseService;
            _apiService = apiService;
            _sessionService = sessionService;
            PendingCaptures = new ObservableCollection<SalidaTrampeoRatas>();
            Title = "Trampeos Pendientes";

            if (!WeakReferenceMessenger.Default.IsRegistered<ValueChangedMessage<int>>(this))
            {
                WeakReferenceMessenger.Default.Register<ValueChangedMessage<int>>(this, (r, m) =>
                {
                    // m.Value contiene el Id enviado
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await LoadPendingCapturesAsync();
                    });
                });
            }
        }

        [RelayCommand]
        public async Task LoadPendingCapturesAsync()
        {
            if (IsBusy) return;
            SetBusy(true);
            try
            {
                var session = await _sessionService.GetCurrentSessionAsync();

                var zafraList = await _databaseService.GetAllAsync<Zafra>();
                var allCampos = await _databaseService.GetAllAsync<Campo>();
                var filteredCampos = session.TipoUsuario == 1 ? allCampos : allCampos.Where(c => c.IdInspector == session.IdInspector).ToList();

                var list = await _databaseService.GetAllAsync<SalidaTrampeoRatas>();

                foreach (var item in list)
                {
                    item.ZafraNombre = zafraList.FirstOrDefault(z => z.Id == item.IdTemporada)?.Nombre ?? "Zafra N/D";
                    item.CampoNombre = filteredCampos.FirstOrDefault(c => c.Id == item.IdCampo)?.Nombre ?? "Predio N/D";
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    PendingCaptures.Clear();
                    foreach (var item in list.OrderByDescending(i => i.Fecha))
                    {
                        PendingCaptures.Add(item);
                    }
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
        public async Task EditCaptureAsync(SalidaTrampeoRatas capture)
        {
            if (capture == null)
            {
                Debug.WriteLine("[Pending] EditCaptureAsync called with null capture!");
                return;
            }

            Debug.WriteLine($"[Pending] EditCaptureAsync called with capture.Id = {capture.Id}");

            var route = $"RatTrappingPage?recordId={capture.Id}";
            Debug.WriteLine($"[Pending] Navigating to route: {route}");
            await Shell.Current.GoToAsync(route);
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