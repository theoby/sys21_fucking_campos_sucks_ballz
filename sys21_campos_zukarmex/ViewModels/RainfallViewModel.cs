using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class RainfallViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly ApiService _apiService;
        private readonly ConnectivityService _connectivityService;
        private readonly SessionService _sessionService;
        public ConnectivityService ConnectivitySvc => _connectivityService;

        private bool isInitialized = false;

        [ObservableProperty] private ObservableCollection<Pluviometro> pluviometros = new();
        [ObservableProperty]
        private Pluviometro? selectedPluviometro;
        [ObservableProperty] private bool hasPluviometros = false;
        [ObservableProperty] private DateTime fecha = DateTime.Now.AddDays(-1);
        [ObservableProperty] private string precipitacion = string.Empty;
        [ObservableProperty]
        private DateTime minDate = DateTime.Now.AddYears(-5);
        [ObservableProperty]
        private DateTime maxDate = DateTime.Today;


        public RainfallViewModel(DatabaseService databaseService, ApiService apiService, ConnectivityService connectivityService, SessionService sessionService)
        {
            _databaseService = databaseService;
            _apiService = apiService;
            _connectivityService = connectivityService;
            _sessionService = sessionService;
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
                var session = await _sessionService.GetCurrentSessionAsync();
                if (session == null) { await Shell.Current.DisplayAlert("Error", "No se pudo obtener la sesión.", "OK"); return; }

                var appPerms = await _sessionService.GetAppPermissionAsync("Precipitación Pluvial");

                // --- BLOQUE DE DEBUG ---
                Debug.WriteLine("==================================================");
                Debug.WriteLine("PERMISOS PARA: Precipitación Pluvial");
                Debug.WriteLine($"Mira, estos son los datos del usuario para esta pagina:");
                Debug.WriteLine($"- ¿Tiene Permiso?: {appPerms.TienePermiso}");
                Debug.WriteLine($"- TipoUsuario (específico): {appPerms.TipoUsuario}");
                Debug.WriteLine($"- IdInspector (específico): {appPerms.IdInspector}");
                Debug.WriteLine("==================================================");

                var hoy = DateTime.Today;

                var pluviometrosFromApi = await _apiService.GetPluviometrosAsync();
                Pluviometros.Clear();

                foreach (var pluviometro in pluviometrosFromApi.OrderBy(p => p.Nombre))
                {
                    Pluviometros.Add(pluviometro);
                }
                if (Pluviometros.Count == 0)
                {
                    var placeholder = new Pluviometro
                    {
                        Id = -1,
                        Nombre = "No hay pluviómetros disponibles"
                    };

                    Pluviometros.Add(placeholder);

                    HasPluviometros = false;

                    SelectedPluviometro = placeholder;
                }
                else
                {
                    HasPluviometros = true;
                    SelectedPluviometro = null;
                }
            }
            catch (Exception ex) { await Shell.Current.DisplayAlert("Error", $"No se pudieron cargar catálogos: {ex.Message}", "OK"); }
            finally { SetBusy(false); }
        }

        partial void OnSelectedPluviometroChanged(Pluviometro? value)
        {
            if (value != null && value.Id != -1)
            {
           
                MinDate = value.FechaInicio;
                MaxDate = value.FechaBaja ?? DateTime.Today; 

                if (Fecha > MaxDate) Fecha = MaxDate;
                if (Fecha < MinDate) Fecha = MinDate;
            }
            else
            {
                MinDate = DateTime.Now.AddYears(-5);
                MaxDate = DateTime.Today;
            }
        }

        [RelayCommand]
        private async Task AddRainfallAsync()
        {
            if (SelectedPluviometro == null || string.IsNullOrWhiteSpace(Precipitacion))
            {
                await Shell.Current.DisplayAlert("Campos Requeridos", "Por favor, complete todos los campos.", "OK");
                return;
            }

            if (Fecha.Date < SelectedPluviometro.FechaInicio.Date)
            {
                await Shell.Current.DisplayAlert("Fecha Inválida", $"La fecha de captura ({Fecha:dd/MM/yy}) no puede ser anterior a la fecha de inicio del pluviómetro ({SelectedPluviometro.FechaInicio:dd/MM/yy}).", "OK");
                return;
            }
            if (SelectedPluviometro.FechaBaja != null && Fecha.Date > SelectedPluviometro.FechaBaja.Value.Date)
            {
                await Shell.Current.DisplayAlert("Fecha Inválida", $"La fecha de captura ({Fecha:dd/MM/yy}) no puede ser posterior a la fecha de baja del pluviómetro ({SelectedPluviometro.FechaBaja:dd/MM/yy}).", "OK");
                return;
            }

            if (IsBusy) return;
            SetBusy(true);

            try
            {
                var newRainfall = new SalidaPrecipitacion
                {
                    IdPluviometro = SelectedPluviometro.Id,
                    Fecha = this.Fecha,
                    Precipitacion = decimal.TryParse(Precipitacion, out var p) ? p : 0
                };

                try
                {
                    var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium));
                    if (location != null)
                    {
                        newRainfall.Lat = location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        newRainfall.Lng = location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
                catch (Exception) { /* Ignorar error */ }

                await _databaseService.SaveAsync(newRainfall);
                await Shell.Current.DisplayAlert("Guardado Localmente", "El registro de precipitación se guardó en el dispositivo.", "OK");
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
            SelectedPluviometro = null;
            Precipitacion = string.Empty;
            Fecha = DateTime.Now.AddDays(-1);
        }
    }
}