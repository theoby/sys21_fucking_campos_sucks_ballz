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
    public partial class DamageAssessmentPendingViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly ApiService _apiService;
        private readonly SessionService _sessionService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPendingItems))]
        [NotifyPropertyChangedFor(nameof(PendingCount))]
        private ObservableCollection<SalidaMuestroDaños> pendingAssessments;

        [ObservableProperty]
        private bool isRefreshing;
        public int PendingCount => PendingAssessments?.Count ?? 0;
        public bool HasPendingItems => PendingAssessments?.Any() ?? false;

        public DamageAssessmentPendingViewModel(DatabaseService databaseService, ApiService apiService, SessionService sessionService)
        {
            _databaseService = databaseService;
            _apiService = apiService;
            _sessionService = sessionService;
            PendingAssessments = new ObservableCollection<SalidaMuestroDaños>();
            Title = "Muestreos Pendientes";
            // La corrección: Llamar a la carga al iniciar el ViewModel
            _ = LoadPendingAssessmentsAsync();
        }

        // --- Comando para cargar los datos de la DB local ---
        [RelayCommand]
        public async Task LoadPendingAssessmentsAsync()
        {
            if (IsBusy) return;
            SetBusy(true);

            try
            {
                var session = await _sessionService.GetCurrentSessionAsync();

                var zafraList = await _databaseService.GetAllAsync<Zafra>();
                var cicloList = await _databaseService.GetAllAsync<Ciclo>();
                var allCampos = await _databaseService.GetAllAsync<Campo>();

                var filteredCampos = session.TipoUsuario == 1
                    ? allCampos
                    : allCampos.Where(c => c.IdInspector == session.IdInspector).ToList();

                var list = await _databaseService.GetAllAsync<SalidaMuestroDaños>();
                PendingAssessments.Clear();

                foreach (var item in list.OrderByDescending(i => i.Fecha))
                {
                    item.ZafraNombre = zafraList.FirstOrDefault(z => z.Id == item.IdTemporada)?.Nombre ?? "Zafra N/D";
                    item.CampoNombre = filteredCampos.FirstOrDefault(c => c.Id == item.IdCampo)?.Nombre ?? "Predio N/D";
                    item.CicloNombre = cicloList.FirstOrDefault(c => c.Id == item.IdCiclo)?.Nombre ?? "Ciclo N/D";

                    PendingAssessments.Add(item);
                }
                OnPropertyChanged(nameof(PendingCount)); 
                OnPropertyChanged(nameof(HasPendingItems));
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
                                                              $"Se enviarán {PendingAssessments.Count} muestreos pendientes. ¿Desea continuar?",
                                                              "Sí, Enviar", "Cancelar");
            if (!confirmed)
            {
                SetBusy(false);
                return;
            }

            // Crear una copia de la lista para enviarla.
            var itemsToSend = PendingAssessments.ToList();

            try
            {
                // 1. Enviar la lista completa a la API
                // NOTA: Se asume que _apiService.SendPendingDamageAssessmentsAsync existe y devuelve un objeto con 'Success' y 'Message'.
                var response = await _apiService.SendPendingDamageAssessmentsAsync(itemsToSend);

                if (response.Success)
                {
                    // 2. ÉXITO: Eliminar los registros de la DB local
                    await _databaseService.DeleteListAsync(itemsToSend);

                    await Shell.Current.DisplayAlert("Éxito de Sincronización",
                                                     $"Se enviaron {itemsToSend.Count} muestreos y se eliminaron de la base de datos local.",
                                                     "OK");

                    // 3. Recargar la lista (debería quedar vacía)
                    await LoadPendingAssessmentsAsync();
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
        public async Task DeleteAssessmentAsync(SalidaMuestroDaños assessment)
        {
            if (assessment == null) return;

            bool confirmed = await Shell.Current.DisplayAlert("Confirmar Eliminación",
                                                             $"¿Está seguro de que desea eliminar el muestreo Id: {assessment.Id} localmente?",
                                                             "Sí, Eliminar", "Cancelar");

            if (!confirmed) return;

            SetBusy(true);

            try
            {
                // 1. Eliminar de la base de datos local (usando el método que acepta un solo objeto)
                await _databaseService.DeleteAsync(assessment);

                // 2. Eliminar de la colección Observable para actualizar la UI
                PendingAssessments.Remove(assessment);


                // Opcional: Notificar que el conteo ha cambiado
                OnPropertyChanged(nameof(PendingCount));
                OnPropertyChanged(nameof(HasPendingItems));

                await Shell.Current.DisplayAlert("Éxito", $"Muestreo Id: {assessment.Id} eliminado localmente.", "OK");
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

        [RelayCommand]
        public async Task RefreshAsync()
        {
            if (IsBusy) return;

            try
            {
                IsRefreshing = true;
                await LoadPendingAssessmentsAsync();
            }
            finally
            {
                IsRefreshing = false;
            }
        }
    }
}
