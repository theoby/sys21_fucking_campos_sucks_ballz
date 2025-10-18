using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class MachineryUsageHistoryViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;

        // Lista completa para el filtro
        private List<SalidaMaquinaria> _allAssessments = new List<SalidaMaquinaria>();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HistoryCount))]
        [NotifyPropertyChangedFor(nameof(HasHistoryItems))]
        private ObservableCollection<SalidaMaquinaria> historialMachineryUsage;

        [ObservableProperty]
        private bool isRefreshing; // Para el control RefreshView

        // Propiedades de Conteo
        public int HistoryCount => HistorialMachineryUsage?.Count ?? 0;
        public bool HasHistoryItems => HistorialMachineryUsage?.Any() ?? false;

        // Sólo necesita ApiService, sin DatabaseService.
        public MachineryUsageHistoryViewModel(ApiService apiService)
        {
            _apiService = apiService;
            HistorialMachineryUsage = new ObservableCollection<SalidaMaquinaria>();
            Title = "Historial de Muestreos";

            _ = LoadHistorialMachineryUsageAsync();
        }

        // --- Comando para cargar los datos de la API (Similar a LoadPending) ---
        [RelayCommand]
        public async Task LoadHistorialMachineryUsageAsync()
        {
            if (IsBusy) return;
            SetBusy(true);
            IsRefreshing = true;

            try
            {
                var list = await _apiService.GetMachineryUsageHistoryAsync();

                _allAssessments = list.OrderByDescending(d => d.Fecha).ToList();

                if (!_allAssessments.Any() && IsConnected)
                {
                    // Usa IsConnected de BaseViewModel
                    await Shell.Current.DisplayAlert("Información",
                        "No se encontraron registros en el historial de usos.", "OK");
                }
                HistorialMachineryUsage.Clear();
                foreach (var item in _allAssessments)
                {
                    HistorialMachineryUsage.Add(item);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error de Carga",
                    $"No se pudo cargar el historial: {ex.Message}", "OK");
            }
            finally
            {
                SetBusy(false);
                IsRefreshing = false;
            }
        }

        // Comando para refrescar
        [RelayCommand]
        public async Task RefreshAsync()
        {
            if (IsBusy) return;

            try
            {
                IsRefreshing = true;
                await LoadHistorialMachineryUsageAsync();
            }
            finally
            {
                IsRefreshing = false;
            }
        }
    }
}