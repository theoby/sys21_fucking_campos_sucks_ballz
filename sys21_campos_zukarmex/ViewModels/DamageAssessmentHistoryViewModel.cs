using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Linq;
using System.Threading.Tasks;
using System;
// Se asume que este using es necesario para MVVM Toolkit
using CommunityToolkit.Mvvm.ComponentModel.__Internals;

namespace sys21_campos_zukarmex.ViewModels
{
    // ¡Asegúrate de que la clase sea parcial!
    public partial class DamageAssessmentHistoryViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;

        // Lista completa para el filtro
        private List<SalidaMuestroDaños> _allAssessments = new List<SalidaMuestroDaños>();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HistoryCount))]
        [NotifyPropertyChangedFor(nameof(HasHistoryItems))]
        // El campo privado 'historialAssessments' genera la propiedad pública 'HistorialAssessments'
        private ObservableCollection<SalidaMuestroDaños> historialAssessments;

        [ObservableProperty]
        private bool isRefreshing; // Para el control RefreshView

        [ObservableProperty]
        private string searchText = string.Empty; // Para la barra de búsqueda

        // Propiedades de Conteo
        public int HistoryCount => HistorialAssessments?.Count ?? 0;
        public bool HasHistoryItems => HistorialAssessments?.Any() ?? false;

        // Sólo necesita ApiService, sin DatabaseService.
        public DamageAssessmentHistoryViewModel(ApiService apiService)
        {
            _apiService = apiService;
            HistorialAssessments = new ObservableCollection<SalidaMuestroDaños>();
            Title = "Historial de Muestreos";

            _ = LoadHistorialAssessmentsAsync();
        }

        // --- Comando para cargar los datos de la API (Similar a LoadPending) ---
        [RelayCommand]
        public async Task LoadHistorialAssessmentsAsync()
        {
            if (IsBusy) return;
            SetBusy(true);
            IsRefreshing = true;

            try
            {
                // Se asume que GetDamageAssessmentHistoryAsync existe y usa GetCatalogAsync
                var list = await _apiService.GetDamageAssessmentHistoryAsync();

                _allAssessments = list.OrderByDescending(d => d.Fecha).ToList();

                // Actualiza la lista observable usando la lógica de filtrado
                ApplySearchFilter();

                if (!_allAssessments.Any() && IsConnected)
                {
                    // Usa IsConnected de BaseViewModel
                    await Shell.Current.DisplayAlert("Información",
                        "No se encontraron registros en el historial de muestreos.", "OK");
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


        // --- Lógica de Filtrado de Búsqueda ---
        private void ApplySearchFilter()
        {
            HistorialAssessments.Clear(); // Usa la propiedad pública generada

            IEnumerable<SalidaMuestroDaños> filtered = _allAssessments;

            // Usa la propiedad pública generada: SearchText
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = _allAssessments.Where(d =>
                    // El error de 'int a char' desaparece con .ToString()
                    d.Id.ToString().Contains(searchLower) ||
                    d.IdCampo.ToString().Contains(searchLower) 
                );
            }

            foreach (var item in filtered)
            {
                HistorialAssessments.Add(item);
            }

            // Usa OnPropertyChanged de BaseViewModel
            OnPropertyChanged(nameof(HistoryCount));
            OnPropertyChanged(nameof(HasHistoryItems));
        }

        // Corrección: Método parcial para el cambio de 'SearchText'. 
        // ¡La firma debe ser (oldValue, newValue)!
        partial void OnSearchTextChanged(string oldValue, string newValue)
        {
            // Busca automáticamente al escribir o borrar (si la longitud es >= 2)
            if (string.IsNullOrWhiteSpace(newValue) || newValue.Length >= 2)
            {
                ApplySearchFilter();
            }
        }

        // Comando para ver detalles
        [RelayCommand]
        public async Task ViewAssessmentDetailsAsync(SalidaMuestroDaños assessment)
        {
            if (assessment == null) return;

            await Shell.Current.DisplayAlert("Detalle de Muestreo",
                                             $"ID: {assessment.Id}\nFecha: {assessment.Fecha:dd/MM/yyyy HH:mm}\nUsuario: Tallos: {assessment.NumeroTallos}",
                                             "OK");
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            if (IsBusy) return;

            try
            {
                IsRefreshing = true;
                await LoadHistorialAssessmentsAsync();
            }
            finally
            {
                IsRefreshing = false;
            }
        }
    }
}