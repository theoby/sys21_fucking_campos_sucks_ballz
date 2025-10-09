using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class RodenticideConsumptionViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly ApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly ConnectivityService _connectivityService;
        public ConnectivityService ConnectivitySvc => _connectivityService;
        private bool isInitialized = false;

        [ObservableProperty]
        private ObservableCollection<Zafra> zafras = new();

        [ObservableProperty]
        private ObservableCollection<Campo> campos = new();

        [ObservableProperty]
        private Zafra? selectedZafra;

        [ObservableProperty]
        private Campo? selectedCampo;

        [ObservableProperty]
        private DateTime fecha = DateTime.Now;

        [ObservableProperty]
        private string cantidadComederos = "0";

        [ObservableProperty]
        private string cantidadPastillas = "0";

        [ObservableProperty]
        private string consumo = "0";

        [ObservableProperty]
        private int totalCebo;

        [ObservableProperty]
        private double porcentajeConsumo;

        [ObservableProperty]
        private bool areFieldsLocked = false;

        public RodenticideConsumptionViewModel(
            DatabaseService databaseService,
            ApiService apiService,
            SessionService sessionService,
            ConnectivityService connectivityService)
        {
            _databaseService = databaseService;
            _apiService = apiService;
            _sessionService = sessionService;
            _connectivityService = connectivityService;
            Title = "Consumo de Rodenticida";
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

                var zafraList = await _databaseService.GetAllAsync<Zafra>();
                Zafras.Clear();
                foreach (var zafra in zafraList.OrderBy(z => z.Nombre))
                    Zafras.Add(zafra);

                var allCampos = await _databaseService.GetAllAsync<Campo>();
                var filteredCampos = session.TipoUsuario == 1
                    ? allCampos
                    : allCampos.Where(c => c.IdInspector == session.IdInspector).ToList();

                Campos.Clear();
                foreach (var campo in filteredCampos.OrderBy(c => c.Nombre))
                    Campos.Add(campo);
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

        partial void OnCantidadComederosChanged(string value) => RecalculateTotals();
        partial void OnCantidadPastillasChanged(string value) => RecalculateTotals();
        partial void OnConsumoChanged(string value) => RecalculateTotals();

        private void RecalculateTotals()
        {
            int comederos = int.TryParse(CantidadComederos, out var c) ? c : 0;
            int pastillas = int.TryParse(CantidadPastillas, out var p) ? p : 0;
            int consumoActual = int.TryParse(Consumo, out var con) ? con : 0;

            TotalCebo = comederos * pastillas;

            if (TotalCebo > 0)
            {
                PorcentajeConsumo = (double)consumoActual / TotalCebo;
            }
            else
            {
                PorcentajeConsumo = 0;
            }
        }

        private void ClearForm()
        {
            SelectedZafra = null;
            SelectedCampo = null;
            Fecha = DateTime.Now;
            CantidadComederos = string.Empty;
            CantidadPastillas = string.Empty;
            Consumo = string.Empty;
        }

        [RelayCommand]
        private async Task AddConsumptionAsync()
        {
            if (SelectedZafra == null || SelectedCampo == null)
            {
                await Shell.Current.DisplayAlert("Campos Requeridos", "Por favor, seleccione Zafra y Predio.", "OK");
                return;
            }

            if (IsBusy) return;

            SetBusy(true);
            try
            {
                var newConsumption = new SalidaRodenticida
                {
                    IdTemporada = SelectedZafra.Id,
                    IdCampo = SelectedCampo.Id,
                    Fecha = this.Fecha,
                    CantidadComederos = int.TryParse(CantidadComederos, out var c) ? c : 0,
                    CantidadPastillas = int.TryParse(CantidadPastillas, out var p) ? p : 0,
                    CantidadConsumos = int.TryParse(Consumo, out var con) ? con : 0, 
                };

                try
                {
                    var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
                    if (location != null)
                    {
                        newConsumption.Lat = location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        newConsumption.Lng = location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"No se pudo obtener la geolocalización: {ex.Message}");
                }

                if (ConnectivitySvc.IsConnected)
                {
                    var apiResponse = await _apiService.SaveRodenticideConsumptionAsync(newConsumption);
                    if (apiResponse.Success)
                    {
                        await Shell.Current.DisplayAlert("Éxito", "Consumo de rodenticida enviado correctamente.", "OK");
                    }
                    else
                    {
                        await _databaseService.SaveAsync(newConsumption);
                        await Shell.Current.DisplayAlert("Guardado Localmente", "La API no respondió. El registro se guardó localmente.", "OK");
                    }
                }
                else
                {
                    await _databaseService.SaveAsync(newConsumption);
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

    } 
    
}
