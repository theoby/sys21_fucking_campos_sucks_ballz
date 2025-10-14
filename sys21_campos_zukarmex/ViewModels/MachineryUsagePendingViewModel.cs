using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class MachineryUsagePendingViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly ApiService _apiService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPendingItems))]
        [NotifyPropertyChangedFor(nameof(PendingCount))]
        private ObservableCollection<SalidaMaquinaria> pendingMachineryUsages;

        public int PendingCount => PendingMachineryUsages?.Count ?? 0;
        public bool HasPendingItems => PendingMachineryUsages?.Any() ?? false;


        public MachineryUsagePendingViewModel(DatabaseService databaseService, ApiService apiService)
        {
            _databaseService = databaseService;
            _apiService = apiService;
            PendingMachineryUsages = new ObservableCollection<SalidaMaquinaria>();
            Title = "Usos Pendientes";
            // La corrección: Llamar a la carga al iniciar el ViewModel
            _ = LoadPendingMachineryUsagesAsync();
        }

        // --- Comando para cargar los datos de la DB local ---
        [RelayCommand]
        public async Task LoadPendingMachineryUsagesAsync()
        {
            if (IsBusy) return;
            SetBusy(true);

            try
            {
                var list = await _databaseService.GetAllAsync<SalidaMaquinaria>();
                PendingMachineryUsages.Clear();

                foreach (var item in list)
                {
                    PendingMachineryUsages.Add(item);
                }
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


        // --- Comando para Enviar y Eliminar (Actualizado) ---
        [RelayCommand]
        public async Task SendAllPendingAsync()
        {
            if (!HasPendingItems)
            {
                await Shell.Current.DisplayAlert("Sincronización", "No hay muestreos pendientes para enviar.", "OK");
                return;
            }

            if (IsBusy) return;
            SetBusy(true); // Bloquea la UI

            // Confirmación antes de enviar
            bool confirmed = await Shell.Current.DisplayAlert("Confirmar Sincronización",
                                                              $"Se enviarán {PendingMachineryUsages.Count} muestreos pendientes. ¿Desea continuar?",
                                                              "Sí, Enviar", "Cancelar");
            if (!confirmed)
            {
                SetBusy(false);
                return;
            }

            // Crear una copia de la lista para enviarla.
            var itemsToSend = PendingMachineryUsages.ToList();

            try
            {
                // 1. Enviar la lista completa a la API

                var response = await _apiService.SendPendingMachineryUsageAsync(itemsToSend);

                if (response.Success)
                {
                    // 2. ÉXITO: Eliminar los registros de la DB local
                    await _databaseService.DeleteListAsync(itemsToSend);

                    await Shell.Current.DisplayAlert("Éxito de Sincronización",
                                                     $"Se enviaron {itemsToSend.Count} muestreos y se eliminaron de la base de datos local.",
                                                     "OK");

                    // 3. Recargar la lista (debería quedar vacía)
                    await LoadPendingMachineryUsagesAsync();
                }
                else
                {
                    // Fallo confirmado por la API. No eliminamos los datos locales.
                    string errorMessage = response.Message ?? "Error desconocido devuelto por la API.";
                    await Shell.Current.DisplayAlert("Error de Sincronización",
                                                     $"Error al enviar los datos. La API devolvió un error: {errorMessage}",
                                                     "OK");
                }
            }
            catch (Exception ex)
            {
                // Fallo de conexión, timeout o servicio (no hay respuesta)
                await Shell.Current.DisplayAlert("Fallo de Conexión",
                                                 $"Fallo al intentar sincronizar: {ex.Message}. Verifique su conexión e intente de nuevo.",
                                                 "OK");
            }
            finally
            {
                SetBusy(false); // Desbloquea la UI
            }
        }

        [RelayCommand]
        public async Task DeleteMachineryUsagesAsync(SalidaMaquinaria Use)
        {
            if (Use == null) return;

            bool confirmed = await Shell.Current.DisplayAlert("Confirmar Eliminación",
                                                             $"¿Está seguro de que desea eliminar el muestreo Id: {Use.Id} localmente?",
                                                             "Sí, Eliminar", "Cancelar");

            if (!confirmed) return;

            SetBusy(true);

            try
            {
                // 1. Eliminar de la base de datos local (usando el método que acepta un solo objeto)
                await _databaseService.DeleteAsync(Use);

                // 2. Eliminar de la colección Observable para actualizar la UI
                PendingMachineryUsages.Remove(Use);

                // Opcional: Notificar que el conteo ha cambiado
                OnPropertyChanged(nameof(PendingCount));
                OnPropertyChanged(nameof(HasPendingItems));

                await Shell.Current.DisplayAlert("Éxito", $"Muestreo Id: {Use.Id} eliminado localmente.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error de Eliminación",
                                                 $"No se pudo eliminar el muestreo: {ex.Message}", "OK");
            }
            finally
            {
                SetBusy(false);
            }
        }
    }
}
