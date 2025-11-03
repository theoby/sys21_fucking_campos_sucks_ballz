// EN ViewModels/IrrigationLineHistoryViewModel.cs

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using sys21_campos_zukarmex.Models; // Contiene SalidaIrrigationLine
using sys21_campos_zukarmex.Services;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace sys21_campos_zukarmex.ViewModels
{
    // Asegúrate de que la clase sea parcial y herede de BaseViewModel
    public partial class IrrigationLineHistoryViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;
        private readonly DatabaseService _databaseService; 
        private readonly SessionService _sessionService;

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

        public IrrigationLineHistoryViewModel(ApiService apiService, DatabaseService databaseService, SessionService sessionService)
        {
            _apiService = apiService;
            _databaseService = databaseService; 
            _sessionService = sessionService;
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

                var session = await _sessionService.GetCurrentSessionAsync();
                var allCampos = await _databaseService.GetAllAsync<Campo>();
                var filteredCampos = session.TipoUsuario == 1 ? allCampos : allCampos.Where(c => c.IdInspector == session.IdInspector).ToList();

                var lineasPredefinidas = await _databaseService.GetAllAsync<LineaDeRiego>();

                var listFromApi = await _apiService.GetIrrigationLineHistoryAsync();

                foreach (var item in listFromApi)
                {
                    item.CampoNombre = filteredCampos.FirstOrDefault(c => c.Id == item.IdCampo)?.Nombre ?? "Predio N/D";
                    item.LineaRiegoNombre = lineasPredefinidas.FirstOrDefault(l => l.Id == item.IdLineaRiego)?.Nombre ?? "Línea N/D";
                }

                _allEntries = listFromApi.OrderByDescending(d => d.Fecha).ToList();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    HistorialEntries.Clear();
                    foreach (var item in _allEntries) HistorialEntries.Add(item);
                    OnPropertyChanged(nameof(HistoryCount));
                    OnPropertyChanged(nameof(HasHistoryItems));
                });

                if (!_allEntries.Any())
                {
                    await Shell.Current.DisplayAlert("Información", "No se encontraron registros en el historial.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudo cargar el historial: {ex.Message}", "OK");
            }
            finally
            {
                SetBusy(false);
                IsRefreshing = false;
            }
        }

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
            await LoadHistoryDataAsync();
        }
    }
}