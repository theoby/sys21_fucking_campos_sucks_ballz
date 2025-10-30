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
        [ObservableProperty] private DateTime fecha = DateTime.Today;
        [ObservableProperty] private string equipoDeBombeo = string.Empty;
        [ObservableProperty] private string equiposDeBombeoOperando = string.Empty;
        [ObservableProperty] private string observacion = string.Empty;
        public DateTime MinDate { get; } = DateTime.Today.AddDays(-1);

        private int maxEquiposOperando = 0;

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

                var lineasFromDb = await _databaseService.GetAllAsync<LineaDeRiego>();

                LineasDeRiego.Clear();
                foreach (var linea in lineasFromDb.OrderBy(l => l.Nombre)) LineasDeRiego.Add(linea);
            }
            catch (Exception ex) { await Shell.Current.DisplayAlert("Error", $"No se pudieron cargar catálogos: {ex.Message}", "OK"); }
            finally { SetBusy(false); }
        }

        partial void OnSelectedLineaDeRiegoChanged(LineaDeRiego? value)
        {
            if (value != null)
            {
                EquipoDeBombeo = value.CantidadEquiposBombeo.ToString();
                maxEquiposOperando = value.CantidadEquiposBombeo;
            }
            else
            {
                EquipoDeBombeo = string.Empty;
                maxEquiposOperando = 0;
            }
            EquiposDeBombeoOperando = string.Empty;
        }

        [RelayCommand]
        private async Task AddIrrigationEntryAsync()
        {
            if (SelectedPredio == null || SelectedLineaDeRiego == null)
            {
                await Shell.Current.DisplayAlert("Campos Requeridos", "Por favor, seleccione un Predio y una Línea de Riego.", "OK");
                return;
            }

            if (!int.TryParse(EquiposDeBombeoOperando, out int operando) || operando <= 0)
            {
                await Shell.Current.DisplayAlert("Dato Inválido", "Debe ingresar un número válido y mayor a 0 para los 'Equipos de bombeo operando'.", "OK");
                return;
            }

            if (operando > maxEquiposOperando)
            {
                await Shell.Current.DisplayAlert("Dato Inválido", $"El número de equipos operando ({operando}) no puede ser mayor al total de equipos de bombeo ({maxEquiposOperando}).", "OK");
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
            SelectedPredio = null; 
            SelectedLineaDeRiego = null;
            Fecha = DateTime.Today;
            EquipoDeBombeo = string.Empty;
            EquiposDeBombeoOperando = string.Empty;
            Observacion = string.Empty;
            maxEquiposOperando = 0; 
        }
    }
}