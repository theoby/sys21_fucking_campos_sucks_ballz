using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class RatTrappingViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly SessionService _sessionService;
        private bool isInitialized = false;

        [ObservableProperty] private ObservableCollection<Zafra> zafras = new();
        [ObservableProperty] private ObservableCollection<Campo> campos = new();

        [ObservableProperty] private Zafra? selectedZafra;
        [ObservableProperty] private Campo? selectedCampo;
        [ObservableProperty] private DateTime fecha = DateTime.Now;
        [ObservableProperty] private string numeroDeTrampas = string.Empty;
        [ObservableProperty] private string machosCapturados = string.Empty;
        [ObservableProperty] private string hembrasCapturadas = string.Empty;

        public RatTrappingViewModel(DatabaseService databaseService, SessionService sessionService)
        {
            _databaseService = databaseService;
            _sessionService = sessionService;
            Title = "Trampeo de Ratas";
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

                // Cargar catálogos desde la base de datos local
                var zafraList = await _databaseService.GetAllAsync<Zafra>();
                Zafras.Clear();
                foreach (var zafra in zafraList.OrderBy(z => z.Nombre)) Zafras.Add(zafra);

                var allCamposFromDb = await _databaseService.GetAllAsync<Campo>();
                var filteredCampos = session.TipoUsuario == 1 ? allCamposFromDb : allCamposFromDb.Where(c => c.IdInspector == session.IdInspector).ToList();
                Campos.Clear();
                foreach (var campo in filteredCampos.OrderBy(c => c.Nombre)) Campos.Add(campo);
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
                    var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium));
                    if (location != null)
                    {
                        newCapture.Lat = location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        newCapture.Lng = location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"No se pudo obtener la geolocalización: {ex.Message}");
                    newCapture.Lat = "0";
                    newCapture.Lng = "0";
                }

                // Guardar únicamente en la base de datos local
                await _databaseService.SaveAsync(newCapture);

                await Shell.Current.DisplayAlert("Guardado Localmente", "La captura de trampeo se guardó en el dispositivo.", "OK");

                ClearForm();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudo guardar la captura localmente: {ex.Message}", "OK");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void ClearForm()
        {
            SelectedCampo = null;
            Fecha = DateTime.Now;
            NumeroDeTrampas = string.Empty;
            MachosCapturados = string.Empty;
            HembrasCapturadas = string.Empty;
        }
    }
}