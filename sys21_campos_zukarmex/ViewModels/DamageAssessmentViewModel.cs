using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class DamageAssessmentViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly ApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly ConnectivityService _connectivityService;
        public ConnectivityService ConnectivitySvc => _connectivityService;
        private bool isInitialized = false;

        [ObservableProperty] private ObservableCollection<Campo> campos = new();
        [ObservableProperty] private ObservableCollection<Zafra> zafras = new();
        [ObservableProperty] private ObservableCollection<Ciclo> ciclos = new();

        [ObservableProperty] private Campo? selectedCampo;
        [ObservableProperty] private Zafra? selectedZafra;
        [ObservableProperty] private Ciclo? selectedCiclo;

        [ObservableProperty] private DateTime fecha = DateTime.Now;
        [ObservableProperty] private string superficie = string.Empty;
        [ObservableProperty] private string numeroTallos = string.Empty;
        [ObservableProperty] private string danoViejo = string.Empty;
        [ObservableProperty] private string danoNuevo = string.Empty;

        [ObservableProperty] private int totalDano;
        [ObservableProperty] private double porcentajeDano;

        public DamageAssessmentViewModel(DatabaseService databaseService, ApiService apiService, SessionService sessionService, ConnectivityService connectivityService)
        {
            _databaseService = databaseService;
            _apiService = apiService;
            _sessionService = sessionService;
            _connectivityService = connectivityService;
            Title = "Muestreo de Daño";
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
                if (session == null) { /* ... manejo de error ... */ return; }

                var allCampos = await _databaseService.GetAllAsync<Campo>();
                var filteredCampos = session.TipoUsuario == 1 ? allCampos : allCampos.Where(c => c.IdInspector == session.IdInspector).ToList();
                Campos.Clear();
                foreach (var campo in filteredCampos.OrderBy(c => c.Nombre)) Campos.Add(campo);

                var zafraList = await _databaseService.GetAllAsync<Zafra>();
                Zafras.Clear();
                foreach (var zafra in zafraList.OrderBy(z => z.Nombre)) Zafras.Add(zafra);

                var cicloList = await _databaseService.GetAllAsync<Ciclo>();
                Ciclos.Clear();
                foreach (var ciclo in cicloList.OrderBy(c => c.Nombre)) Ciclos.Add(ciclo);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudieron cargar catálogos: {ex.Message}", "OK");
            }
            finally
            {
                SetBusy(false);
            }
        }

        partial void OnNumeroTallosChanged(string value) => RecalculateTotals();
        partial void OnDanoViejoChanged(string value) => RecalculateTotals();
        partial void OnDanoNuevoChanged(string value) => RecalculateTotals();

        private void RecalculateTotals()
        {
            int tallos = int.TryParse(NumeroTallos, out var t) ? t : 0;
            int viejo = int.TryParse(DanoViejo, out var v) ? v : 0;
            int nuevo = int.TryParse(DanoNuevo, out var n) ? n : 0;

            TotalDano = viejo + nuevo;

            if (tallos > 0)
            {
                PorcentajeDano = (double)TotalDano / tallos;
            }
            else
            {
                PorcentajeDano = 0;
            }
        }

        [RelayCommand]
        private async Task AddAssessmentAsync()
        {
            if (SelectedCampo == null || SelectedZafra == null || SelectedCiclo == null)
            {
                await Shell.Current.DisplayAlert("Campos Requeridos", "Por favor, seleccione Predio, Zafra y Ciclo.", "OK");
                return;
            }

            if (IsBusy) return;
            SetBusy(true);
            try
            {
                var newAssessment = new SalidaMuestroDaños
                {
                    IdTemporada = SelectedZafra.Id,
                    IdCampo = SelectedCampo.Id,
                    IdCiclo = SelectedCiclo.Id,
                    Fecha = this.Fecha,
                    NumeroTallos = int.TryParse(NumeroTallos, out var t) ? t : 0,
                    DañoViejo = int.TryParse(DanoViejo, out var v) ? v : 0,
                    DañoNuevo = int.TryParse(DanoNuevo, out var n) ? n : 0,
                    // El campo Id es 0 aquí.
                    Dispositivo = $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}"
                };

                var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium));
                if (location != null)
                {
                    newAssessment.Lat = location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    newAssessment.Lng = location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                // 1. Guardar en la DB. 
                // Aunque devuelva 1 (filas afectadas), el Id del objeto newAssessment debería actualizarse aquí.
                var rowsAffected = await _databaseService.SaveAsync(newAssessment);

                // 2. Obtener el ID asignado. 
                // Usamos el ID que SQLite debió haber puesto en el objeto.
                var realAssignedId = newAssessment.Id;

                System.Diagnostics.Debug.WriteLine($"[DB SAVE]: Muestreo de Daño guardado. Filas afectadas: {rowsAffected}. ID asignado (desde objeto): {realAssignedId}");

                SalidaMuestroDaños? savedAssessment = null;

                // Si el ID es mayor que 0 (lo cual es cierto en tus últimas pruebas), intentamos recuperarlo
                if (realAssignedId > 0)
                {
                    // 3. Recuperar el objeto usando el ID REAL para verificación.
                    savedAssessment = await _databaseService.GetByIdAsync<SalidaMuestroDaños>(realAssignedId);
                }

                // 4. Lógica de verificación y mensaje al usuario
                if (savedAssessment != null)
                { // Imprimir los detalles del objeto recuperado en el Debug.
                    System.Diagnostics.Debug.WriteLine("-------------------------------------------");
                    System.Diagnostics.Debug.WriteLine($"[DB READ SUCCESS]: Muestreo de Daño recuperado con ID: {savedAssessment.Id}");

                    // AÑADIDO: Claves foráneas y fecha
                    System.Diagnostics.Debug.WriteLine($"- Temporada ID: {savedAssessment.IdTemporada}");
                    System.Diagnostics.Debug.WriteLine($"- Campo ID: {savedAssessment.IdCampo}");
                    System.Diagnostics.Debug.WriteLine($"- Ciclo ID: {savedAssessment.IdCiclo}");
                    System.Diagnostics.Debug.WriteLine($"- Fecha: {savedAssessment.Fecha:yyyy-MM-dd HH:mm:ss}"); // Formato completo

                    // DATOS DEL MUESTREO
                    System.Diagnostics.Debug.WriteLine($"- Tallos: {savedAssessment.NumeroTallos}");
                    System.Diagnostics.Debug.WriteLine($"- Daño (V/N): {savedAssessment.DañoViejo} / {savedAssessment.DañoNuevo}");

                    // DATOS GEOGRÁFICOS Y DISPOSITIVO
                    System.Diagnostics.Debug.WriteLine($"- Dispositivo: {savedAssessment.Dispositivo}");
                    System.Diagnostics.Debug.WriteLine($"- Lat/Lng: {savedAssessment.Lat}, {savedAssessment.Lng}");
                    System.Diagnostics.Debug.WriteLine("-------------------------------------------");

                    await Shell.Current.DisplayAlert("Guardado Localmente", "El registro se guardó en el dispositivo.", "OK");
                }
                else
                {
                    // Si falla la recuperación, pero sabemos que la inserción (rowsAffected=1) fue exitosa
                    // y que el ID (realAssignedId) fue asignado, mostramos el ID asignado para referencia.
                    System.Diagnostics.Debug.WriteLine($"[DB VERIFICATION FAILED]: Falló la recuperación por ID {realAssignedId}.");                }

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
            SelectedCampo = null;
            SelectedZafra = null;
            SelectedCiclo = null;
            Fecha = DateTime.Now;
            NumeroTallos = string.Empty;
            DanoViejo = string.Empty;
            DanoNuevo = string.Empty;
        }
    }
}