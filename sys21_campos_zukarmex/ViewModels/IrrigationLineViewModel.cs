using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class IrrigationLineViewModel : BaseViewModel
    {
        // Servicios Inyectados
        private readonly DatabaseService _databaseService;
        private readonly ApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly ConnectivityService _connectivityService;
        public ConnectivityService ConnectivitySvc => _connectivityService;

        private bool isInitialized = false;

        [ObservableProperty]
        private ObservableCollection<Campo> predios;

        [ObservableProperty]
        private ObservableCollection<LineaDeRiego> lineasDeRiego;

        [ObservableProperty]
        private Campo? selectedPredio;

        [ObservableProperty]
        private LineaDeRiego? selectedLineaDeRiego;

        [ObservableProperty]
        private DateTime fecha = DateTime.Now;

        [ObservableProperty]
        private string equipoDeBombeo = string.Empty;

        [ObservableProperty]
        private string equiposDeBombeoOperando = string.Empty;

        [ObservableProperty]
        private string observacion = string.Empty;


        public IrrigationLineViewModel(DatabaseService databaseService, ApiService apiService, SessionService sessionService, ConnectivityService connectivityService)
        {
            _databaseService = databaseService;
            _apiService = apiService;
            _sessionService = sessionService;
            _connectivityService = connectivityService;
            Title = "Línea de Riego";

            predios = new ObservableCollection<Campo>();
            lineasDeRiego = new ObservableCollection<LineaDeRiego>();
        }

        public async Task InitializeAsync()
        {
            if (isInitialized) return;
            await LoadCatalogsAsync();
            isInitialized = true;
        }

        private async Task LoadCatalogsAsync()
        {
            if (IsBusy) return;
            try
            {
                SetBusy(true);
                var session = await _sessionService.GetCurrentSessionAsync();
                if (session == null)
                {
                    await Shell.Current.DisplayAlert("Error de Sesión", "No se pudo obtener la sesión del usuario.", "OK");
                    return;
                }

                var allCamposFromDb = await _databaseService.GetAllAsync<Campo>();
                List<Campo> filteredCampos;
                if (session.TipoUsuario == 1) 
                {
                    filteredCampos = allCamposFromDb;
                }
                else
                {
                    filteredCampos = allCamposFromDb.Where(c => c.IdInspector == session.IdInspector).ToList();
                }

                Predios.Clear();
                foreach (var campo in filteredCampos.OrderBy(c => c.Nombre))
                {
                    Predios.Add(campo);
                }

                var lineasPredefinidas = new List<LineaDeRiego>
                {
                    new LineaDeRiego { Id = 1, Nombre = "Principal Norte" },
                    new LineaDeRiego { Id = 2, Nombre = "Secundaria A-1" },
                    new LineaDeRiego { Id = 3, Nombre = "Secundaria A-2" },
                    new LineaDeRiego { Id = 4, Nombre = "Principal Sur" },
                    new LineaDeRiego { Id = 5, Nombre = "Terciaria B-3 (Goteo)" }
                };

                LineasDeRiego.Clear();//Modificar cuando tengamos el catalogo on
                foreach (var linea in lineasPredefinidas.OrderBy(l => l.Nombre))
                {
                    LineasDeRiego.Add(linea);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudieron cargar los catálogos: {ex.Message}", "OK");
            }
            finally
            {
                SetBusy(false);
            }
        }


        [RelayCommand]
        private async Task AddIrrigationEntryAsync()
        {
            // Validación de campos
            if (SelectedPredio == null || SelectedLineaDeRiego == null)
            {
                await Shell.Current.DisplayAlert("Campos Requeridos", "Por favor, seleccione un Predio y una Línea de Riego.", "OK");
                return;
            }

            if (IsBusy) return;

            SetBusy(true);
            try
            {
                // 1. Crear el objeto de salida
                var newEntry = new SalidaLineaDeRiego
                {
                    IdCampo = SelectedPredio.Id,
                    IdLineaRiego = SelectedLineaDeRiego.Id,
                    Fecha = this.Fecha,
                    Observaciones = this.Observacion,

                    // Manejo seguro de la conversión de string a int
                    EquipoBombeo = int.TryParse(EquipoDeBombeo, out var eb) ? eb : 0,
                    EquiposBombeoOperando = int.TryParse(EquiposDeBombeoOperando, out var eo) ? eo : 0,

                    Dispositivo = $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}"
                };

                // 2. Geolocalización (Latitud y Longitud)
                try
                {
                    var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
                    if (location != null)
                    {
                        // Usamos InvariantCulture para evitar problemas con comas/puntos decimales
                        newEntry.Lat = location.Latitude.ToString(CultureInfo.InvariantCulture);
                        newEntry.Lng = location.Longitude.ToString(CultureInfo.InvariantCulture);
                    }
                }
                catch (Exception ex)
                {
                    // No es un error crítico si falla el GPS, solo se registra
                    System.Diagnostics.Debug.WriteLine($"No se pudo obtener la geolocalización: {ex.Message}");
                }


                // 3. Guardar en la DB LOCAL (similar al AddAssessmentAsync)
                var rowsAffected = await _databaseService.SaveAsync(newEntry);
                var realAssignedId = newEntry.Id; // El ID se asigna en el objeto después del SaveAsync (si el servicio es correcto)

                System.Diagnostics.Debug.WriteLine($"[DB SAVE]: Registro de Riego guardado. Filas afectadas: {rowsAffected}. ID asignado (desde objeto): {realAssignedId}");

                SalidaLineaDeRiego? savedEntry = null;

                if (realAssignedId > 0)
                {
                    // Recuperar el objeto usando el ID REAL para verificación.
                    savedEntry = await _databaseService.GetByIdAsync<SalidaLineaDeRiego>(realAssignedId);
                }

                // 4. Lógica de verificación y mensaje al usuario
                if (savedEntry != null)
                {
                    System.Diagnostics.Debug.WriteLine("-------------------------------------------");
                    System.Diagnostics.Debug.WriteLine($"[DB READ SUCCESS]: Entrada de Riego recuperada con ID: {savedEntry.Id}");
                    System.Diagnostics.Debug.WriteLine($"- Campo ID: {savedEntry.IdCampo}, Línea ID: {savedEntry.IdLineaRiego}");
                    System.Diagnostics.Debug.WriteLine($"- Equipos Operando: {savedEntry.EquiposBombeoOperando}");
                    System.Diagnostics.Debug.WriteLine($"-------------------------------------------");

                    await Shell.Current.DisplayAlert("Guardado Localmente", $"El registro se guardó exitosamente en el dispositivo con ID: {savedEntry.Id}. Se sincronizará más tarde.", "OK");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DB VERIFICATION FAILED]: Falló la verificación para ID {realAssignedId}.");
                    await Shell.Current.DisplayAlert("Guardado Localmente", $"El registro se insertó, pero falló la verificación inmediata. ID asignado: {realAssignedId}", "OK");
                }

                ClearForm();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudo guardar el registro localmente: {ex.Message}", "OK");
            }
            finally
            {
                SetBusy(false);
            }
        }


        private void ClearForm()
        {
            // Mantenemos el Predio seleccionado para facilitar múltiples registros
            SelectedLineaDeRiego = null;
            Fecha = DateTime.Now;
            EquipoDeBombeo = string.Empty;
            EquiposDeBombeoOperando = string.Empty;
            Observacion = string.Empty;
        }
    }
}