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
    public partial class RatTrappingViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly ApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly ConnectivityService _connectivityService;
        public ConnectivityService ConnectivitySvc => _connectivityService;

        private bool isInitialized = false;

        [ObservableProperty]
        private ObservableCollection<Zafra> zafras;

        [ObservableProperty]
        private ObservableCollection<Campo> campos;

        [ObservableProperty]
        private Zafra? selectedZafra;

        [ObservableProperty]
        private Campo? selectedCampo;

        [ObservableProperty]
        private DateTime fecha = DateTime.Now;

        [ObservableProperty]
        private string numeroDeTrampas = string.Empty;

        [ObservableProperty]
        private string machosCapturados = string.Empty;

        [ObservableProperty]
        private string hembrasCapturadas = string.Empty;

        public RatTrappingViewModel(DatabaseService databaseService, ApiService apiService, SessionService sessionService, ConnectivityService connectivityService)
        {
            _databaseService = databaseService;
            _apiService = apiService;
            _sessionService = sessionService;
            _connectivityService = connectivityService;
            Title = "Trampeo de Ratas";

            zafras = new ObservableCollection<Zafra>();
            campos = new ObservableCollection<Campo>();
            _ = LoadCatalogsAsync();
        }



        private async Task LoadCatalogsAsync()
        {
            Debug.WriteLine("Inicio LoadCatalogs");
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

                // Cargar todas las zafras
                var zafraList = await _databaseService.GetAllAsync<Zafra>();
                Zafras.Clear();
                Debug.WriteLine("Zafras a cargar");
                foreach (var zafra in zafraList.OrderBy(z => z.Nombre))
                {
                    Debug.WriteLine(zafra);
                    Zafras.Add(zafra);
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

                // Poblar la lista de Campos en la UI
                Campos.Clear();
                foreach (var campo in filteredCampos.OrderBy(c => c.Nombre))
                {
                    Campos.Add(campo);
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
        private async Task AddCaptureAsync()
        {
            if (SelectedZafra == null || SelectedCampo == null || string.IsNullOrWhiteSpace(NumeroDeTrampas))
            {
                await Shell.Current.DisplayAlert("Campos Requeridos", "Por favor, complete Zafra, Predio y Número de trampas.", "OK");
                return;
            }

            if (IsBusy) return;

            SetBusy(true);
            try
            {
                var newCapture = new SalidaTrampeoRatas
                {
                    IdTemporada = SelectedZafra.Id, 
                    IdCampo = SelectedCampo.Id,
                    Fecha = this.Fecha,
                    CantidadTrampas = int.TryParse(NumeroDeTrampas, out var nt) ? nt : 0,
                    CantidadMachos = int.TryParse(MachosCapturados, out var m) ? m : 0,
                    CantidadHembras = int.TryParse(HembrasCapturadas, out var h) ? h : 0,

                    Dispositivo = $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}"
                };

                try
                {
                    var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
                    if (location != null)
                    {
                        newCapture.Lat = (int)(location.Latitude * 1000000); 
                        newCapture.Lng = (int)(location.Longitude * 1000000);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"No se pudo obtener la geolocalización: {ex.Message}");
                }


                if (ConnectivitySvc.IsConnected)
                {

                    var apiResponse = await _apiService.SaveRatCaptureAsync(newCapture);
                    if (apiResponse.Success)
                    {
                        await Shell.Current.DisplayAlert("Éxito", "Captura enviada correctamente.", "OK");
                    }
                    else
                    {
                        await _databaseService.SaveAsync(newCapture);
                        await Shell.Current.DisplayAlert("Guardado Localmente", "La API no respondió. La captura se guardó localmente.", "OK");
                    }
                }
                else
                {
                    await _databaseService.SaveAsync(newCapture);
                    await Shell.Current.DisplayAlert("Guardado Localmente", "Sin conexión. La captura se guardó localmente.", "OK");
                }

                ClearForm();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudo guardar la captura: {ex.Message}", "OK");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void ClearForm()
        {
            SelectedCampo = null; // CAMBIO
            Fecha = DateTime.Now;
            NumeroDeTrampas = string.Empty;
            MachosCapturados = string.Empty;
            HembrasCapturadas = string.Empty;
        }
    }
}