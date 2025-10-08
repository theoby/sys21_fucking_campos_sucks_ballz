using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Collections.ObjectModel;
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

                //Geolocalización (Latitud y Longitud)
                try
                {
                    var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
                    if (location != null)
                    {
                        // El modelo espera string para Lat/Lng, lo cual es ideal
                        newEntry.Lat = location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        newEntry.Lng = location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"No se pudo obtener la geolocalización: {ex.Message}");
                }


                if (ConnectivitySvc.IsConnected)
                {
                    var apiResponse = await _apiService.SaveIrrigationEntryAsync(newEntry);
                    if (apiResponse.Success)
                    {
                        await Shell.Current.DisplayAlert("Éxito", "Registro de riego enviado correctamente.", "OK");
                    }
                    else
                    {
                        await _databaseService.SaveAsync(newEntry);
                        await Shell.Current.DisplayAlert("Guardado Localmente", "La API no respondió. El registro se guardó localmente.", "OK");
                    }
                }
                else
                {
                    await _databaseService.SaveAsync(newEntry);
                    await Shell.Current.DisplayAlert("Guardado Localmente", "Sin conexión. El registro se guardó localmente.", "OK");
                }

                ClearForm();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudo guardar el registro: {ex.Message}", "OK");
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