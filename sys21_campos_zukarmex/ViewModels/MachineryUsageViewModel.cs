using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class MachineryUsageViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly ApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly ConnectivityService _connectivityService;
        public ConnectivityService ConnectivitySvc => _connectivityService;
        private bool isInitialized = false;

        [ObservableProperty] private ObservableCollection<Campo> campos;
        [ObservableProperty] private ObservableCollection<Lote> lotes;
        [ObservableProperty] private ObservableCollection<Maquinaria> equipos = new();

        [ObservableProperty] private Lote? selectedLote;
        [ObservableProperty] private Maquinaria? selectedEquipo;
        [ObservableProperty] private DateTime fecha = DateTime.Now;

        [ObservableProperty] private string horasTrabajadas = string.Empty;
        [ObservableProperty] private string kilometrajeOdometro = string.Empty;


        public MachineryUsageViewModel(DatabaseService databaseService, ApiService apiService, SessionService sessionService, ConnectivityService connectivityService)
        {
            _databaseService = databaseService;
            _apiService = apiService;
            _sessionService = sessionService;
            _connectivityService = connectivityService;
            Title = "Uso de Maquinaria";
            campos = new ObservableCollection<Campo>();
            lotes = new ObservableCollection<Lote>();
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

                var appPerms = await _sessionService.GetAppPermissionAsync("Uso De Maquinaria");

                // --- BLOQUE DE DEBUG ---
                Debug.WriteLine("==================================================");
                Debug.WriteLine("PERMISOS PARA: Uso De Maquinaria");
                Debug.WriteLine($"Mira, estos son los datos del usuario para esta pagina:");
                Debug.WriteLine($"- ¿Tiene Permiso?: {appPerms.TienePermiso}");
                Debug.WriteLine($"- TipoUsuario (específico): {appPerms.TipoUsuario}");
                Debug.WriteLine($"- IdInspector (específico): {appPerms.IdInspector}");
                Debug.WriteLine("==================================================");


                var session = await _sessionService.GetCurrentSessionAsync();
                if (session == null) { /* ... error ... */ return; }

                var equipoList = await _databaseService.GetAllAsync<Maquinaria>();
                Equipos.Clear();
                foreach (var item in equipoList.OrderBy(e => e.Nombre)) Equipos.Add(item);

                var allLotesFromDb = await _databaseService.GetAllAsync<Lote>();
                var allCamposFromDb = await _databaseService.GetAllAsync<Campo>();

                List<Lote> filteredLotes;

                if (session.TipoUsuario == 1) 
                {
                    // 2. El Admin ve TODOS los lotes
                    filteredLotes = allLotesFromDb;
                    System.Diagnostics.Debug.WriteLine($"Usuario Admin: Cargando {filteredLotes.Count} lotes totales.");
                }

                else 
                {
                    var inspectorId = session.IdInspector;

                    var misCamposIds = allCamposFromDb
                        .Where(c => c.IdInspector == inspectorId)
                        .Select(c => c.Id)
                        .ToHashSet();

                    filteredLotes = allLotesFromDb
                        .Where(lote => misCamposIds.Contains(lote.IdCampo))
                        .ToList();

                    System.Diagnostics.Debug.WriteLine($"Usuario Inspector ({inspectorId}): Encontró {misCamposIds.Count} campos. Cargando {filteredLotes.Count} lotes filtrados.");
                }

                Lotes.Clear();
                foreach (var lote in filteredLotes.OrderBy(l => l.Nombre))
                {
                    Lotes.Add(lote);
                }
            }
            catch (Exception ex) { /* ... error ... */ }
            finally { SetBusy(false); }
        }

        partial void OnHorasTrabajadasChanged(string value)
        {
            if (int.TryParse(value, out var horas) && horas > 0)
            {
                if (!string.IsNullOrEmpty(KilometrajeOdometro) && KilometrajeOdometro != "0")
                {
                    KilometrajeOdometro = "0";
                }
            }
        }

        partial void OnKilometrajeOdometroChanged(string value)
        {
            if (int.TryParse(value, out var km) && km > 0)
            {
                if (!string.IsNullOrEmpty(HorasTrabajadas) && HorasTrabajadas != "0")
                {
                    HorasTrabajadas = "0";
                }
            }
        }

        [RelayCommand]

    
        private async Task SaveAsync()
        {
            // Validaciones
            if (selectedLote == null || SelectedEquipo == null ||
                (string.IsNullOrWhiteSpace(HorasTrabajadas) && string.IsNullOrWhiteSpace(KilometrajeOdometro)) ||
                (!decimal.TryParse(HorasTrabajadas, out var h) && !decimal.TryParse(KilometrajeOdometro, out var k)))
            {
                await Shell.Current.DisplayAlert("Campos Requeridos", "Por favor, complete Predio, Equipo, y capture un valor numérico válido en Horas o Kilometraje.", "OK");
                return;
            }

            if (IsBusy) return;
            SetBusy(true);

            try
            {
                // 1. Crear el objeto de registro con datos de la UI
                var newUsage = new SalidaMaquinaria
                {
                    IdMaquinaria = SelectedEquipo.IdPk,
                    IdCampo = selectedLote.Id,
                    Fecha = this.Fecha,
                    HorasTrabajadas = (int)(decimal.TryParse(HorasTrabajadas, out var hDecimal) ? hDecimal : 0m),
                    KilometrajeOdometro = (int)(decimal.TryParse(KilometrajeOdometro, out var kDecimal) ? kDecimal : 0m),
                    // Id, Lat, Lng se asignan a continuación
                };

                // 2. Obtener la ubicación GPS
                var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium));
                if (location != null)
                {
                    newUsage.Lat = location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    newUsage.Lng = location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                // 3. Guardar en la DB local. El ID del objeto 'newUsage' se actualizará internamente.
                var rowsAffected = await _databaseService.SaveAsync(newUsage);
                var realAssignedId = newUsage.Id; // Obtener el ID asignado por SQLite

                System.Diagnostics.Debug.WriteLine($"[DB SAVE]: Uso de Maquinaria guardado. Filas afectadas: {rowsAffected}. ID asignado (desde objeto): {realAssignedId}");

                SalidaMaquinaria? savedUsage = null;

                // 4. Recuperar y verificar (solo si se asignó un ID)
                if (realAssignedId > 0)
                {
                    savedUsage = await _databaseService.GetByIdAsync<SalidaMaquinaria>(realAssignedId);
                }

                // 5. Lógica de verificación y mensaje al usuario
                if (savedUsage != null)
                {
                    System.Diagnostics.Debug.WriteLine("-------------------------------------------");
                    System.Diagnostics.Debug.WriteLine($"[DB READ SUCCESS]: Uso de Maquinaria recuperado con ID: {savedUsage.Id}");
                    System.Diagnostics.Debug.WriteLine($"- Máquina ID: {savedUsage.IdMaquinaria}");
                    System.Diagnostics.Debug.WriteLine($"- Campo ID: {savedUsage.IdCampo}");
                    System.Diagnostics.Debug.WriteLine($"- Horas/Km: {savedUsage.HorasTrabajadas} / {savedUsage.KilometrajeOdometro}");
                    System.Diagnostics.Debug.WriteLine($"- Fecha: {savedUsage.Fecha:yyyy-MM-dd HH:mm:ss}");
                    System.Diagnostics.Debug.WriteLine($"- Lat/Lng: {savedUsage.Lat}, {savedUsage.Lng}");
                    System.Diagnostics.Debug.WriteLine("-------------------------------------------");

                    await Shell.Current.DisplayAlert("Guardado Localmente", "El registro se guardó en el dispositivo.", "OK");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DB VERIFICATION FAILED]: Falló la recuperación por ID {realAssignedId}.");
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
            selectedLote = null;
            SelectedEquipo = null;
            Fecha = DateTime.Now;
            HorasTrabajadas = string.Empty;
            KilometrajeOdometro = string.Empty;
        }
    }
}