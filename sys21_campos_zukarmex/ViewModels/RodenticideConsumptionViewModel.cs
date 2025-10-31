using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class RodenticideConsumptionViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly SessionService _sessionService;
        private bool isInitialized = false;

        [ObservableProperty] private ObservableCollection<Zafra> zafras = new();
        [ObservableProperty] private ObservableCollection<Campo> campos = new();
        [ObservableProperty] private Zafra? selectedZafra;
        [ObservableProperty] private Campo? selectedCampo;
        [ObservableProperty] private DateTime fecha = DateTime.Now;

        [ObservableProperty] private string cantidadComederos = string.Empty;
        [ObservableProperty] private string cantidadPastillas = string.Empty;
        [ObservableProperty] private string consumo = string.Empty;

        [ObservableProperty] private int totalCebo;
        [ObservableProperty] private double porcentajeConsumo;

        public RodenticideConsumptionViewModel(DatabaseService databaseService, SessionService sessionService)
        {
            _databaseService = databaseService;
            _sessionService = sessionService;
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

                var appPerms = await _sessionService.GetAppPermissionAsync("Consumo de Rodenticida");

                Debug.WriteLine("==================================================");
                Debug.WriteLine("PERMISOS PARA: Consumo de Rodenticida");
                Debug.WriteLine($"Mira, estos son los datos del usuario para esta pagina:");
                Debug.WriteLine($"- ¿Tiene Permiso?: {appPerms.TienePermiso}");
                Debug.WriteLine($"- TipoUsuario (específico): {appPerms.TipoUsuario}");
                Debug.WriteLine($"- IdInspector (específico): {appPerms.IdInspector}");
                Debug.WriteLine("==================================================");

                var session = await _sessionService.GetCurrentSessionAsync();
                if (session == null) { await Shell.Current.DisplayAlert("Error", "No se pudo obtener la sesión.", "OK"); return; }

                var zafraList = await _databaseService.GetAllAsync<Zafra>();
                Zafras.Clear();
                foreach (var zafra in zafraList.OrderBy(z => z.Nombre)) Zafras.Add(zafra);

                var allCampos = await _databaseService.GetAllAsync<Campo>();
                var filteredCampos = session.TipoUsuario == 1 ? allCampos : allCampos.Where(c => c.IdInspector == session.IdInspector).ToList();
                Campos.Clear();
                foreach (var campo in filteredCampos.OrderBy(c => c.Nombre)) Campos.Add(campo);
            }
            catch (Exception ex) { await Shell.Current.DisplayAlert("Error", $"No se pudieron cargar catálogos: {ex.Message}", "OK"); }
            finally { SetBusy(false); }
        }

        partial void OnCantidadComederosChanged(string value) => RecalculateTotals();
        partial void OnCantidadPastillasChanged(string value) => RecalculateTotals();
        partial void OnConsumoChanged(string newValue)
        {
            if (!int.TryParse(newValue, out var consumoActual))
            {
                RecalculateTotals();
                return;
            }

            int comederos = int.TryParse(CantidadComederos, out var c) ? c : 0;
            int pastillas = int.TryParse(CantidadPastillas, out var p) ? p : 0;
            int limiteCebo = comederos * pastillas;

            if (limiteCebo > 0 && consumoActual > limiteCebo)
            {
                Shell.Current.DisplayAlert(
                    "Límite Excedido",
                    $"El consumo no puede ser mayor al total de cebo disponible ({limiteCebo}).",
                    "OK");

                this.Consumo = limiteCebo.ToString();

                RecalculateTotals();
                return;
            }

            // Si pasa la validación, recalcula los totales
            RecalculateTotals();
        }

        private void RecalculateTotals()
        {
            int comederos = int.TryParse(CantidadComederos, out var c) ? c : 0;
            int pastillas = int.TryParse(CantidadPastillas, out var p) ? p : 0;
            int consumoActual = int.TryParse(Consumo, out var con) ? con : 0;

            TotalCebo = comederos * pastillas;

            PorcentajeConsumo = (TotalCebo > 0) ? (double)consumoActual / TotalCebo * 100 : 0;
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
                    Dispositivo = $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}"
                };

                try
                {
                    var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium));
                    if (location != null)
                    {
                        newConsumption.Lat = location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        newConsumption.Lng = location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
                catch (Exception) { /* Ignorar error de geolocalización, se guardará con 0 */ }

                await _databaseService.SaveAsync(newConsumption);
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
            SelectedZafra = null;
            SelectedCampo = null;
            Fecha = DateTime.Now;
            CantidadComederos = string.Empty;
            CantidadPastillas = string.Empty;
            Consumo = string.Empty;
        }
    }
}