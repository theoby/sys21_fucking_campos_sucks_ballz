using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class AuthorizationViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly ApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly ConnectivityService _connectivityService;
        public ConnectivityService ConnectivitySvc => _connectivityService;

        private bool isInitialized = false;

        [ObservableProperty]
        private ObservableCollection<Empresa> empresas = new();

        [ObservableProperty]
        private ObservableCollection<Pluviometro> pluviometros = new();

        [ObservableProperty]
        private Empresa? selectedEmpresa;

        [ObservableProperty]
        private Pluviometro? selectedPluviometro;

        [ObservableProperty]
        private DateTime fecha = DateTime.Now.AddDays(-1); 

        [ObservableProperty]
        private string precipitacion = string.Empty;


        public AuthorizationViewModel(DatabaseService databaseService, ApiService apiService, SessionService sessionService, ConnectivityService connectivityService)
        {
            _databaseService = databaseService;
            _apiService = apiService;
            _sessionService = sessionService;
            _connectivityService = connectivityService;
            Title = "Precipitación Pluvial";
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

                // Cargar Empresas desde la base de datos local
                var empresaList = await _databaseService.GetAllAsync<Empresa>();
                Empresas.Clear();
                foreach (var empresa in empresaList) Empresas.Add(empresa);

                // NUEVO: Cargar Pluviómetros desde la API
                if (ConnectivitySvc.IsConnected)
                {
                    var pluviometrosFromApi = await _apiService.GetPluviometrosAsync();
                    Pluviometros.Clear();
                    foreach (var pluviometro in pluviometrosFromApi) Pluviometros.Add(pluviometro);
                }
                else
                {
                    await Shell.Current.DisplayAlert("Sin Conexión", "Se necesita conexión a internet para cargar el catálogo de pluviómetros.", "OK");
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
        private async Task AddRainfallAsync()
        {
            if (SelectedEmpresa == null || SelectedPluviometro == null || string.IsNullOrWhiteSpace(Precipitacion))
            {
                await Shell.Current.DisplayAlert("Campos Requeridos", "Por favor, complete todos los campos.", "OK");
                return;
            }

            if (IsBusy) return;
            SetBusy(true);

            try
            {
                // Aquí necesitarás un modelo local, por ejemplo 'SalidaPluvial'
                var newRainfall = new SalidaPluvial
                {
                    IdEmpresa = SelectedEmpresa.Id,
                    IdPluviometro = SelectedPluviometro.Id,
                    Fecha = this.Fecha,
                    Precipitacion = decimal.TryParse(Precipitacion, out var p) ? p : 0,
                    FechaCreacion = DateTime.Now
                };

                var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium));
                if (location != null)
                {
                    newRainfall.Lat = location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    newRainfall.Lng = location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                if (ConnectivitySvc.IsConnected)
                {
                    var apiResponse = await _apiService.SaveRainfallAsync(newRainfall);
                    if (apiResponse.Success)
                    {
                        await Shell.Current.DisplayAlert("Éxito", "Registro de precipitación enviado.", "OK");
                    }
                    else
                    {
                        await _databaseService.SaveAsync(newRainfall);
                        await Shell.Current.DisplayAlert("Guardado Localmente", "La API no respondió. Se guardó localmente.", "OK");
                    }
                }
                else
                {
                    await _databaseService.SaveAsync(newRainfall);
                    await Shell.Current.DisplayAlert("Guardado Localmente", "Sin conexión. Se guardó localmente.", "OK");
                }
                ClearForm();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudo guardar: {ex.Message}", "OK");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void ClearForm()
        {
            SelectedEmpresa = null;
            SelectedPluviometro = null;
            Precipitacion = string.Empty;
            Fecha = DateTime.Now.AddDays(-1);
        }
    }
}