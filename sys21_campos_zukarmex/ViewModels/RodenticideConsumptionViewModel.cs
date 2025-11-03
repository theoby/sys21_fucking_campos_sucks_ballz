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

        private int _previousConsumo = 0;
        private bool _isUpdatingConsumo = false;

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

                if (!appPerms.TienePermiso)
                {
                    await Shell.Current.DisplayAlert("Acceso Denegado", "No tiene permiso para este módulo.", "OK");
                    SetBusy(false);
                    return;
                }

                var tipoUsuario = appPerms.TipoUsuario;
                var inspectorId = appPerms.IdInspector;



                var zafraList = await _databaseService.GetAllAsync<Zafra>();
                Zafras.Clear();
                foreach (var zafra in zafraList.OrderBy(z => z.Nombre)) Zafras.Add(zafra);

                var allCampos = await _databaseService.GetAllAsync<Campo>();
                var filteredCampos = (tipoUsuario == 1) 
                    ? allCampos
                    : allCampos.Where(c => c.IdInspector == inspectorId).ToList();
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
            // Intentamos parsear; si no se puede, recalculamos y salimos.
            if (!int.TryParse(newValue, out var consumoActual))
            {
                _previousConsumo = 0;
                RecalculateTotals();
                return;
            }
            if (consumoActual < 0)
            {
                _ = Shell.Current.DisplayAlert("Valor inválido", "El consumo no puede ser negativo.", "OK");
                // Revertir al anterior valor válido
                _isUpdatingConsumo = true;
                Consumo = _previousConsumo.ToString();
                _isUpdatingConsumo = false;
                RecalculateTotals();
                return;
            }

            int comederos = int.TryParse(CantidadComederos, out var c) ? c : 0;
            int pastillas = int.TryParse(CantidadPastillas, out var p) ? p : 0;
            int limiteCebo = comederos * pastillas;

            // No permitimos valores negativos
            if (consumoActual < 0)
            {
                Shell.Current.DisplayAlert("Valor inválido", "El consumo no puede ser negativo.", "OK");
                this.Consumo = "0";
                RecalculateTotals();
                return;
            }

            if (limiteCebo > 0 && consumoActual > limiteCebo)
            {
                _ = Shell.Current.DisplayAlert(
                    "Límite Excedido",
                    $"No puedes aumentar el consumo por encima del total disponible ({limiteCebo}). El valor volverá al anterior.",
                    "OK");

                // Revertimos al último consumo válido
                _isUpdatingConsumo = true;
                Consumo = _previousConsumo.ToString();
                _isUpdatingConsumo = false;

                // No aplicamos RecalculateTotals() con el intento inválido; ya se restauró el valor
                RecalculateTotals();
                return;
            }

            // Si llegó aquí, el valor es válido -> lo guardamos como previo y recalculamos
            _previousConsumo = consumoActual;
            RecalculateTotals();
        }

        private void RecalculateTotals()
        {
            int comederos = int.TryParse(CantidadComederos, out var c) ? c : 0;
            int pastillas = int.TryParse(CantidadPastillas, out var p) ? p : 0;
            int consumoActual = int.TryParse(Consumo, out var con) ? con : 0;

            // Calcula total de cebo
            TotalCebo = comederos * pastillas;

            // Si por alguna razón (reducción de comederos/pastillas) el consumo actual queda > total, ajustamos.
            if (TotalCebo >= 0 && consumoActual > TotalCebo)
            {
                // Ajuste automático: no permitimos que el consumo quede > total disponible.
                // Mostramos alerta informativa y corregimos el Entry al tope.
                _ = Shell.Current.DisplayAlert("Ajuste automático", $"El consumo actual ({consumoActual}) excede el nuevo total de cebo ({TotalCebo}). Se ajustará al máximo disponible.", "OK");

                consumoActual = TotalCebo;
                _isUpdatingConsumo = true;
                Consumo = consumoActual.ToString();
                _isUpdatingConsumo = false;

                // Actualizamos el previous al nuevo valor ajustado
                _previousConsumo = consumoActual;
            }

            // Calcula porcentaje, lo limitamos a 100 y lo redondeamos a 2 decimales
            double fraction = (TotalCebo > 0) ? (double)consumoActual / TotalCebo : 0.0;
            fraction = Math.Clamp(fraction, 0.0, 1.0);
            PorcentajeConsumo = Math.Round(fraction, 4); 

            Debug.WriteLine($"porcentaje (fracción): {fraction}");
            Debug.WriteLine($"PorcentajeConsumo (propiedad): {PorcentajeConsumo}");
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