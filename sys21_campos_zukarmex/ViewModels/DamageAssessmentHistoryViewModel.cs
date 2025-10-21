using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;

namespace sys21_campos_zukarmex.ViewModels
{
    // ¡Asegúrate de que la clase sea parcial!
    public partial class DamageAssessmentHistoryViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;
        private readonly DatabaseService _databaseService;
        private readonly SessionService _sessionService;

        // Lista completa para el filtro
        private List<SalidaMuestroDaños> _allAssessments = new List<SalidaMuestroDaños>();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HistoryCount))]
        [NotifyPropertyChangedFor(nameof(HasHistoryItems))]
        private ObservableCollection<SalidaMuestroDaños> historialAssessments;

        [ObservableProperty]
        private bool isRefreshing; // Para el control RefreshView

        [ObservableProperty]
        private string searchText = string.Empty; // Para la barra de búsqueda

        // Propiedades de Conteo
        public int HistoryCount => HistorialAssessments?.Count ?? 0;
        public bool HasHistoryItems => HistorialAssessments?.Any() ?? false;

        // Sólo necesita ApiService, sin DatabaseService.
        public DamageAssessmentHistoryViewModel(ApiService apiService, SessionService sessionService, DatabaseService databaseService)
        {
            _apiService = apiService;
            _databaseService = databaseService;
            _sessionService = sessionService;
            HistorialAssessments = new ObservableCollection<SalidaMuestroDaños>();
            Title = "Historial de Muestreos";

            _ = LoadHistorialAssessmentsAsync();
        }

        [RelayCommand]
        public async Task PageAppearingAsync()
        {
            await LoadHistorialAssessmentsAsync();
        }

        [RelayCommand]
        public async Task LoadHistorialAssessmentsAsync()
        {
            if (IsBusy) return;
            SetBusy(true);
            IsRefreshing = true;

            try
            {
                var session = await _sessionService.GetCurrentSessionAsync();
                var zafraList = await _databaseService.GetAllAsync<Zafra>();
                var cicloList = await _databaseService.GetAllAsync<Ciclo>();
                var allCampos = await _databaseService.GetAllAsync<Campo>();

                var filteredCampos = session.TipoUsuario == 1
                    ? allCampos
                    : allCampos.Where(c => c.IdInspector == session.IdInspector).ToList();

                var listFromApi = await _apiService.GetDamageAssessmentHistoryAsync();

                foreach (var item in listFromApi)
                {
                    item.ZafraNombre = zafraList.FirstOrDefault(z => z.Id == item.IdTemporada)?.Nombre ?? "Zafra N/D";
                    item.CampoNombre = filteredCampos.FirstOrDefault(c => c.Id == item.IdCampo)?.Nombre ?? "Predio N/D";
                    item.CicloNombre = cicloList.FirstOrDefault(c => c.Id == item.IdCiclo)?.Nombre ?? "Ciclo N/D";
                }

                _allAssessments = listFromApi.OrderByDescending(d => d.Fecha).ToList();

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


        private void ApplySearchFilter()
        {
            HistorialAssessments.Clear(); // Usa la propiedad pública generada

            IEnumerable<SalidaMuestroDaños> filtered = _allAssessments;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = _allAssessments.Where(d =>
                    // El error de 'int a char' desaparece con .ToString()
                    (d.CampoNombre != null && d.CampoNombre.ToLower().Contains(searchLower)) || 
                    (d.ZafraNombre != null && d.ZafraNombre.ToLower().Contains(searchLower)) 
                );
            }

            foreach (var item in filtered)
            {
                HistorialAssessments.Add(item);
            }

            OnPropertyChanged(nameof(HistoryCount));
            OnPropertyChanged(nameof(HasHistoryItems));
        }

        
        partial void OnSearchTextChanged(string oldValue, string newValue)
        {
            if (string.IsNullOrWhiteSpace(newValue) || newValue.Length >= 2)
            {
                ApplySearchFilter();
            }
        }

        [RelayCommand]
        public async Task ViewAssessmentDetailsAsync(SalidaMuestroDaños assessment)
        {
            if (assessment == null) return;

            // Mensaje de alerta actualizado para mostrar nombres
            string details = $"ID: {assessment.Id}\n" +
                             $"Fecha: {assessment.Fecha:dd/MM/yyyy HH:mm}\n" +
                             $"Zafra: {assessment.ZafraNombre}\n" +
                             $"Predio: {assessment.CampoNombre}\n" +
                             $"Ciclo: {assessment.CicloNombre}\n\n" +
                             $"Tallos: {assessment.NumeroTallos}\n" +
                             $"Daño Viejo: {assessment.DañoViejo}\n" +
                             $"Daño Nuevo: {assessment.DañoNuevo}";

            await Shell.Current.DisplayAlert("Detalle de Muestreo", details, "OK");
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            await LoadHistorialAssessmentsAsync();
        }
    }
}