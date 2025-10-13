using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Collections.ObjectModel;
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

        [ObservableProperty] private ObservableCollection<Empresa> empresas = new();
        [ObservableProperty] private ObservableCollection<Campo> campos = new();
        [ObservableProperty] private ObservableCollection<Maquinaria> equipos = new();

        [ObservableProperty] private Empresa? selectedEmpresa;
        [ObservableProperty] private Campo? selectedCampo;
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
                if (session == null) { /* ... error ... */ return; }

                var empresaList = await _databaseService.GetAllAsync<Empresa>();
                Empresas.Clear();
                foreach (var item in empresaList.OrderBy(e => e.Nombre)) Empresas.Add(item);

                var equipoList = await _databaseService.GetAllAsync<Maquinaria>();
                Equipos.Clear();
                foreach (var item in equipoList.OrderBy(e => e.Nombre)) Equipos.Add(item);

                var allCampos = await _databaseService.GetAllAsync<Campo>();
                var filteredCampos = session.TipoUsuario == 1 ? allCampos : allCampos.Where(c => c.IdInspector == session.IdInspector).ToList();
                Campos.Clear();
                foreach (var item in filteredCampos.OrderBy(c => c.Nombre)) Campos.Add(item);
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
            if (SelectedEmpresa == null || SelectedCampo == null || SelectedEquipo == null ||
                (string.IsNullOrWhiteSpace(HorasTrabajadas) && string.IsNullOrWhiteSpace(KilometrajeOdometro)))
            {
                await Shell.Current.DisplayAlert("Campos Requeridos", "Por favor, complete todos los campos y capture Horas o Kilometraje.", "OK");
                return;
            }

            if (IsBusy) return;
            SetBusy(true);
            try
            {
                var newUsage = new SalidaMaquinaria
                {
                    IdMaquinaria = SelectedEquipo.IdPk,
                    IdCampo = SelectedCampo.Id,
                    Fecha = this.Fecha,
                    HorasTrabajadas = int.TryParse(HorasTrabajadas, out var h) ? h : 0,
                    KilometrajeOdometro = int.TryParse(KilometrajeOdometro, out var k) ? k : 0,
                };

                var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium));
                if (location != null)
                {
                    newUsage.Lat = location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    newUsage.Lng = location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                // Lógica Online/Offline
                if (ConnectivitySvc.IsConnected)
                {
                    var apiResponse = await _apiService.SaveMachineryUsageAsync(newUsage);
                    if (apiResponse.Success)
                    {
                        await Shell.Current.DisplayAlert("Éxito", "Registro enviado correctamente.", "OK");
                    }
                    else
                    {
                        await _databaseService.SaveAsync(newUsage);
                        await Shell.Current.DisplayAlert("Guardado Localmente", "La API no respondió. Se guardó localmente.", "OK");
                    }
                }
                else
                {
                    await _databaseService.SaveAsync(newUsage);
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
            SelectedCampo = null;
            SelectedEquipo = null;
            Fecha = DateTime.Now;
            HorasTrabajadas = string.Empty;
            KilometrajeOdometro = string.Empty;
        }
    }
}