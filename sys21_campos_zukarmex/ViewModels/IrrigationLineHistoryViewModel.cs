// EN ViewModels/IrrigationLineHistoryViewModel.cs

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using sys21_campos_zukarmex.Models; // Contiene SalidaIrrigationLine
using sys21_campos_zukarmex.Services;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic; // Para List<T>

namespace sys21_campos_zukarmex.ViewModels
{
    // Asegúrate de que la clase sea parcial y herede de BaseViewModel
    public partial class IrrigationLineHistoryViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;

        // Lista completa para la fuente de datos y el filtro
        private List<SalidaLineaDeRiego> _allEntries = new List<SalidaLineaDeRiego>();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HistoryCount))]
        [NotifyPropertyChangedFor(nameof(HasHistoryItems))]
        // Lista que se muestra en la UI (filtrada)
        private ObservableCollection<SalidaLineaDeRiego> historialEntries;

        [ObservableProperty]
        private bool isRefreshing; // Para el control RefreshView

        [ObservableProperty]
        private string searchText = string.Empty; // Para la barra de búsqueda

        // Propiedades de Conteo
        public int HistoryCount => HistorialEntries?.Count ?? 0;
        public bool HasHistoryItems => HistorialEntries?.Any() ?? false;

        public IrrigationLineHistoryViewModel(ApiService apiService)
        {
            _apiService = apiService;
            HistorialEntries = new ObservableCollection<SalidaLineaDeRiego>();
            Title = "Historial Línea de Riego";

            // Carga inicial al crear el ViewModel (Misma lógica que tu referencia)
            _ = LoadHistoryDataAsync();
        }

        // --- Comando para cargar/refrescar los datos de la API ---

        [RelayCommand]
        public async Task LoadHistoryDataAsync()
        {
            if (IsBusy) return;
            SetBusy(true);
            IsRefreshing = true;

            try
            {
                // Se asume que GetIrrigationLineHistoryAsync existe y trae List<SalidaIrrigationLine>
                var list = await _apiService.GetIrrigationLineHistoryAsync();

                _allEntries = list.OrderByDescending(d => d.Fecha).ToList();

                // Actualiza la lista observable usando la lógica de filtrado
                ApplySearchFilter();

                if (!_allEntries.Any() && IsConnected) // Usa IsConnected de BaseViewModel
                {
                    await Shell.Current.DisplayAlert("Información",
                        "No se encontraron registros de Línea de Riego en el historial del servidor.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error de Carga",
                    $"No se pudo cargar el historial de líneas de riego: {ex.Message}", "OK");
            }
            finally
            {
                SetBusy(false);
                IsRefreshing = false;
            }
        }

        // --- Lógica de Filtrado de Búsqueda ---

        // Este método parcial se llama automáticamente cuando cambia SearchText
        partial void OnSearchTextChanged(string oldValue, string newValue)
        {
            // Opcional: Solo busca si la cadena es vacía (para borrar el filtro) o tiene 2+ caracteres
            if (string.IsNullOrWhiteSpace(newValue) || newValue.Length >= 2 || newValue.Length < oldValue.Length)
            {
                ApplySearchFilter();
            }
        }

        private void ApplySearchFilter()
        {
            HistorialEntries.Clear();

            IEnumerable<SalidaLineaDeRiego> filtered = _allEntries;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = _allEntries.Where(d =>
                    // Buscar por ID Campo, ID Línea Riego u Observaciones
                    d.IdCampo.ToString().Contains(searchLower) ||
                    d.IdLineaRiego.ToString().Contains(searchLower) ||
                    (d.Observaciones != null && d.Observaciones.ToLower().Contains(searchLower))
                );
            }

            foreach (var item in filtered)
            {
                HistorialEntries.Add(item);
            }

            OnPropertyChanged(nameof(HistoryCount));
            OnPropertyChanged(nameof(HasHistoryItems));
        }
        [RelayCommand]
        public async Task RefreshAsync()
        {
            if (IsBusy) return;

            try
            {
                IsRefreshing = true;
                await LoadHistoryDataAsync();
            }
            finally
            {
                IsRefreshing = false;
            }
        }
    }
}