using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging; 
using CommunityToolkit.Mvvm.Messaging.Messages; 

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class RatTrappingViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly SessionService _sessionService;
        private bool isInitialized = false;
        [ObservableProperty]
        private bool isEditMode = false;
        [ObservableProperty]
        private int editingRecordId = 0;

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

                var appPerms = await _sessionService.GetAppPermissionAsync("Trampeo de Rata");

                // --- PEGA EL BLOQUE DE DEBUG AQUÍ ---
                Debug.WriteLine("==================================================");
                Debug.WriteLine("PERMISOS PARA: Trampeo de Rata");
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

                var allCamposFromDb = await _databaseService.GetAllAsync<Campo>();
                var filteredCampos = (tipoUsuario == 1) // 1 = Admin
                    ? allCamposFromDb
                    : allCamposFromDb.Where(c => c.IdInspector == inspectorId).ToList();

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
        public async Task LoadCaptureForEditAsync(int recordId)
        {
            Debug.WriteLine($"---------------------------------------------------------------------");
            Debug.WriteLine($"[VM] LoadCaptureForEditAsync called with recordId = {recordId}");
            Debug.WriteLine($"---------------------------------------------------------------------");
            try
            {
                // Asegurarse de tener catálogos
                await InitializeAsync();

                // Traer el registro de la BD
                var record = await _databaseService.GetByIdAsync<SalidaTrampeoRatas>(recordId);
                if (record == null)
                {
                    Debug.WriteLine($"[VM] No se encontró el registro con Id = {recordId}");
                    await Shell.Current.DisplayAlert("Error", "Registro no encontrado.", "OK");
                    return;
                }
                else
                {
                    Debug.WriteLine($"[VM] Registro encontrado: IdCampo={record.IdCampo}, IdTemporada={record.IdTemporada}");
                }

                    // Mapear campos del registro a las propiedades del ViewModel
                    EditingRecordId = record.Id;
                IsEditMode = true;

                Fecha = record.Fecha;
                NumeroDeTrampas = record.CantidadTrampas.ToString();
                MachosCapturados = record.CantidadMachos.ToString();
                HembrasCapturadas = record.CantidadHembras.ToString();

                // Seleccionar Zafra y Campo (ya cargados en Zafras/Campos por InitializeAsync)
                SelectedZafra = Zafras.FirstOrDefault(z => z.Id == record.IdTemporada);
                SelectedCampo = Campos.FirstOrDefault(c => c.Id == record.IdCampo);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudo cargar el registro: {ex.Message}", "OK");
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
                if (IsEditMode)
                {
                    // Actualizar registro existente
                    var recordToUpdate = await _databaseService.GetByIdAsync<SalidaTrampeoRatas>(EditingRecordId);
                    if (recordToUpdate == null)
                    {
                        await Shell.Current.DisplayAlert("Error", "Registro para actualizar no encontrado.", "OK");
                        return;
                    }

                    recordToUpdate.IdTemporada = SelectedZafra.Id;
                    recordToUpdate.IdCampo = SelectedCampo.Id;
                    recordToUpdate.Fecha = this.Fecha;
                    recordToUpdate.CantidadTrampas = int.TryParse(NumeroDeTrampas, out var nt) ? nt : 0;
                    recordToUpdate.CantidadMachos = int.TryParse(MachosCapturados, out var m) ? m : 0;
                    recordToUpdate.CantidadHembras = int.TryParse(HembrasCapturadas, out var h) ? h : 0;
                    // No sobrescribas Dispositivo/LatLng a menos que quieras

                    await _databaseService.SaveAsync(recordToUpdate); // asume Update si Id != 0

                    // Notificar a pendientes para que recargue
                    WeakReferenceMessenger.Default.Send(new ValueChangedMessage<int>(recordToUpdate.Id));

                    await Shell.Current.DisplayAlert("Actualizado", "Registro actualizado localmente.", "OK");

                    // Resetear modo edición
                    IsEditMode = false;
                    EditingRecordId = 0;
                    ClearForm();
                    await Shell.Current.GoToAsync(".."); // vuelve a la vista anterior (pendientes)
                }
                else
                {
                    // Comportamiento original: crear nuevo
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

                    await _databaseService.SaveAsync(newCapture);
                    await Shell.Current.DisplayAlert("Guardado Localmente", "La captura de trampeo se guardó en el dispositivo.", "OK");
                    ClearForm();
                }
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

        partial void OnMachosCapturadosChanged(string value)
        {
            // Si el usuario está escribiendo machos, limpiamos hembras
            if (!string.IsNullOrWhiteSpace(value))
            {
                HembrasCapturadas = string.Empty;
            }
        }

        partial void OnHembrasCapturadasChanged(string value)
        {
            // Si el usuario está escribiendo hembras, limpiamos machos
            if (!string.IsNullOrWhiteSpace(value))
            {
                MachosCapturados = string.Empty;
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