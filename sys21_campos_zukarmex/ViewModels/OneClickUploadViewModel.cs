using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Models.DTOs.Sync; // Reutilizamos el modelo SyncStatus
using sys21_campos_zukarmex.Services;
using sys21_campos_zukarmex.Models.DTOs.Api;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class OneClickUploadViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly ApiService _apiService;
        private readonly ConnectivityService _connectivityService;

        [ObservableProperty]
        private ObservableCollection<SyncStatus> uploadStatuses = new();

        [ObservableProperty]
        private int overallProgress;

        [ObservableProperty]
        private string syncMessage = "Listo para enviar registros locales.";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPendingItems))]
        private int totalPendingCount;

        public bool HasPendingItems => TotalPendingCount > 0;

        public OneClickUploadViewModel(DatabaseService databaseService, ApiService apiService, ConnectivityService connectivityService)
        {
            _databaseService = databaseService;
            _apiService = apiService;
            _connectivityService = connectivityService;
            Title = "Enviar Pendientes";
        }

        [RelayCommand]
        public async Task LoadPendingCountsAsync()
        {
            if (IsBusy) return;
            SetBusy(true);

            try
            {
                UploadStatuses.Clear();
                int totalCount = 0;

                // 1. Muestreo de Daño
                var damageCount = await _databaseService.CountAsync<SalidaMuestroDaños>();
                if (damageCount > 0)
                {
                    UploadStatuses.Add(new SyncStatus { CatalogName = "Muestreo de Daño", Status = $"{damageCount} pendientes" });
                    totalCount += damageCount;
                }

                // 2. Trampeo de Ratas
                var ratCount = await _databaseService.CountAsync<SalidaTrampeoRatas>();
                if (ratCount > 0)
                {
                    UploadStatuses.Add(new SyncStatus { CatalogName = "Trampeo de Ratas", Status = $"{ratCount} pendientes" });
                    totalCount += ratCount;
                }

                // 3. Consumo de Rodenticida
                var rodenticideCount = await _databaseService.CountAsync<SalidaRodenticida>();
                if (rodenticideCount > 0)
                {
                    UploadStatuses.Add(new SyncStatus { CatalogName = "Consumo de Rodenticida", Status = $"{rodenticideCount} pendientes" });
                    totalCount += rodenticideCount;
                }

                // 4. Línea de Riego
                var irrigationCount = await _databaseService.CountAsync<SalidaLineaDeRiego>();
                if (irrigationCount > 0)
                {
                    UploadStatuses.Add(new SyncStatus { CatalogName = "Línea de Riego", Status = $"{irrigationCount} pendientes" });
                    totalCount += irrigationCount;
                }

                // 5. Uso de Maquinaria
                var machineryCount = await _databaseService.CountAsync<SalidaMaquinaria>();
                if (machineryCount > 0)
                {
                    UploadStatuses.Add(new SyncStatus { CatalogName = "Uso de Maquinaria", Status = $"{machineryCount} pendientes" });
                    totalCount += machineryCount;
                }

                // 6. Precipitación Pluvial
                var rainfallCount = await _databaseService.CountAsync<SalidaPrecipitacion>();
                if (rainfallCount > 0)
                {
                    UploadStatuses.Add(new SyncStatus { CatalogName = "Precipitación Pluvial", Status = $"{rainfallCount} pendientes" });
                    totalCount += rainfallCount;
                }

                TotalPendingCount = totalCount;
                SyncMessage = $"Se encontraron {TotalPendingCount} registros pendientes.";
            }
            catch (Exception ex)
            {
                SyncMessage = $"Error al cargar pendientes: {ex.Message}";
            }
            finally
            {
                SetBusy(false);
            }
        }

        [RelayCommand]
        public async Task SendAllAsync()
        {
            if (IsBusy || !HasPendingItems || !_connectivityService.IsConnected ||
                !await Shell.Current.DisplayAlert("Confirmar Envío", $"Se enviarán {TotalPendingCount} registros. ¿Desea continuar?", "Sí, Enviar", "Cancelar"))
            {
                if (!_connectivityService.IsConnected) await Shell.Current.DisplayAlert("Sin Conexión", "Se necesita internet para enviar.", "OK");
                return;
            }

            SetBusy(true);
            SyncMessage = "Iniciando envío...";
            OverallProgress = 0;
            int totalSuccess = 0;
            int totalError = 0;
            
            try
            {
                totalSuccess += await SendItemsAsync<SalidaMuestroDaños>(
                    "Muestreo de Daño",
                    item => _apiService.SaveDamageAssessmentAsync(item)
                );

                totalSuccess += await SendItemsAsync<SalidaTrampeoRatas>(
                    "Trampeo de Ratas",
                    item => _apiService.SaveRatCaptureAsync(item)
                );

                totalSuccess += await SendItemsAsync<SalidaRodenticida>(
                    "Consumo de Rodenticida",
                    item => _apiService.SaveRodenticideConsumptionAsync(item)
                );

                totalSuccess += await SendItemsAsync<SalidaLineaDeRiego>(
                    "Línea de Riego",
                    item => _apiService.SaveIrrigationEntryAsync(item)
                );

                totalSuccess += await SendItemsAsync<SalidaMaquinaria>(
                    "Uso de Maquinaria",
                    item => _apiService.SaveMachineryUsageAsync(item)
                );

                totalSuccess += await SendItemsAsync<SalidaPrecipitacion>(
                    "Precipitación Pluvial",
                    item => _apiService.SaveRainfallAsync(item)
                );
                
                SyncMessage = $"Envío completado. {totalSuccess} registros enviados.";
                await Shell.Current.DisplayAlert("Éxito", SyncMessage, "OK");
            }
            catch (Exception ex)
            {
                totalError++;
                SyncMessage = $"Error durante el envío: {ex.Message}";
                await Shell.Current.DisplayAlert("Error", SyncMessage, "OK");
            }
            finally
            {
                OverallProgress = 100;
                SetBusy(false);
                await LoadPendingCountsAsync(); 
            }
        }
        private async Task<int> SendItemsAsync<T>(string moduleName, Func<T, Task<ApiResponse<T>>> apiCall) where T : class, new()
        {
            var status = UploadStatuses.FirstOrDefault(s => s.CatalogName == moduleName);
            if (status == null) return 0;

            status.Status = "Obteniendo registros...";
            var items = await _databaseService.GetAllAsync<T>();
            if (!items.Any())
            {
                status.Status = "Sin pendientes.";
                status.IsCompleted = true;
                return 0;
            }

            int itemsSent = 0;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                status.Status = $"Enviando {i + 1} de {items.Count}...";

                try
                {
                    var response = await apiCall(item);
                    if (response.Success)
                    {
                        await _databaseService.DeleteAsync(item);
                        itemsSent++;
                    }
                    else
                    {
                        throw new Exception(response.Message ?? "Error desconocido de la API");
                    }
                }
                catch (Exception ex)
                {
                    status.Status = $"Error: {ex.Message}";
                    status.IsCompleted = true; 
                    
                }
            }
            status.Status = $"Éxito ({itemsSent} enviados)";
            status.IsCompleted = true; 
            return itemsSent;
        }
    }
}

