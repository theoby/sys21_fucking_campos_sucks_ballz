using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class IrrigationLineViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly SessionService _sessionService;
        private bool isInitialized = false;

        [ObservableProperty] private ObservableCollection<Campo> predios = new();
        [ObservableProperty] private ObservableCollection<LineaDeRiego> lineasDeRiego = new();
        [ObservableProperty] private Campo? selectedPredio;
        [ObservableProperty] private LineaDeRiego? selectedLineaDeRiego;
        [ObservableProperty] private DateTime fecha = DateTime.Now;
        [ObservableProperty] private string equipoDeBombeo = string.Empty;
        [ObservableProperty] private string equiposDeBombeoOperando = string.Empty;
        [ObservableProperty] private string observacion = string.Empty;

        public IrrigationLineViewModel(DatabaseService databaseService, SessionService sessionService)
        {
            _databaseService = databaseService;
            _sessionService = sessionService;
            Title = "Línea de Riego";
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
                if (session == null) { await Shell.Current.DisplayAlert("Error", "No se pudo obtener la sesión.", "OK"); return; }

                var allCamposFromDb = await _databaseService.GetAllAsync<Campo>();
                var filteredCampos = session.TipoUsuario == 1 ? allCamposFromDb : allCamposFromDb.Where(c => c.IdInspector == session.IdInspector).ToList();
                Predios.Clear();
                foreach (var campo in filteredCampos.OrderBy(c => c.Nombre)) Predios.Add(campo);

                var lineasPredefinidas = new List<LineaDeRiego>
                {
                    new LineaDeRiego { Id = 1, Nombre = "Principal Norte" },
                    new LineaDeRiego { Id = 2, Nombre = "Secundaria A-1" }
                   
                };
                LineasDeRiego.Clear();
                foreach (var linea in lineasPredefinidas.OrderBy(l => l.Nombre)) LineasDeRiego.Add(linea);
            }
            catch (Exception ex) { await Shell.Current.DisplayAlert("Error", $"No se pudieron cargar catálogos: {ex.Message}", "OK"); }
            finally { SetBusy(false); }
        }

        [RelayCommand]
        private async Task AddIrrigationEntryAsync()
        {
            if (SelectedPredio == null || SelectedLineaDeRiego == null)
            {
                await Shell.Current.DisplayAlert("Campos Requeridos", "Por favor, seleccione un Predio y una Línea de Riego.", "OK");
                return;
            }

            if (IsBusy) return;
            SetBusy(true);

            try
            {
                var newEntry = new SalidaLineaDeRiego
                {
                    IdCampo = SelectedPredio.Id,
                    IdLineaRiego = SelectedLineaDeRiego.Id,
                    Fecha = this.Fecha,
                    Observaciones = this.Observacion,
                    EquipoBombeo = int.TryParse(EquipoDeBombeo, out var eb) ? eb : 0,
                    EquiposBombeoOperando = int.TryParse(EquiposDeBombeoOperando, out var eo) ? eo : 0,
                    Dispositivo = $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}"
                };

                try
                {
                    var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium));
                    if (location != null)
                    {
                        newEntry.Lat = location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        newEntry.Lng = location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
                catch (Exception) { /* Ignorar error de geolocalización */ }

                await _databaseService.SaveAsync(newEntry);
                await Shell.Current.DisplayAlert("Guardado Localmente", "El registro se guardó en el dispositivo.", "OK");
                ClearForm();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudo guardar localmente: {ex.Message}", "OK");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void ClearForm()
        {
            SelectedLineaDeRiego = null;
            Fecha = DateTime.Now;
            EquipoDeBombeo = string.Empty;
            EquiposDeBombeoOperando = string.Empty;
            Observacion = string.Empty;
        }
    }
}