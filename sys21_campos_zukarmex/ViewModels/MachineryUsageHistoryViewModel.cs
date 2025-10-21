using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Collections.Generic;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class MachineryUsageHistoryViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;
        private readonly DatabaseService _databaseService;
        private readonly SessionService _sessionService;

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
        public MachineryUsageHistoryViewModel(ApiService apiService, DatabaseService databaseService, SessionService sessionService)
        {
            _apiService = apiService;
            _databaseService = databaseService;
            _sessionService = sessionService;
            HistorialMachineryUsage = new ObservableCollection<SalidaMaquinaria>();
            Title = "Historial de Muestreos";

        }

        [RelayCommand]
        public async Task PageAppearingAsync()
        {
            await LoadHistorialMachineryUsageAsync();
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
                var session = await _sessionService.GetCurrentSessionAsync();
                var empresaList = await _databaseService.GetAllAsync<Empresa>();
                var equipoList = await _databaseService.GetAllAsync<Maquinaria>();
                var allCampos = await _databaseService.GetAllAsync<Campo>();
                var filteredCampos = session.TipoUsuario == 1 ? allCampos : allCampos.Where(c => c.IdInspector == session.IdInspector).ToList();

                var listFromApi = await _apiService.GetMachineryUsageHistoryAsync();

                foreach (var item in listFromApi)
                {
                    item.CampoNombre = filteredCampos.FirstOrDefault(c => c.Id == item.IdCampo)?.Nombre ?? "Predio N/D";
                    item.MaquinariaNombre = equipoList.FirstOrDefault(m => m.IdPk == item.IdMaquinaria)?.Nombre ?? "Equipo N/D";
                    var equipo = equipoList.FirstOrDefault(m => m.IdPk == item.IdMaquinaria);
                    item.EmpresaNombre = empresaList.FirstOrDefault(e => e.Id == equipo?.IdGrupo)?.Nombre ?? "Empresa N/D";
                }

                _allAssessments = listFromApi.OrderByDescending(d => d.Fecha).ToList();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    HistorialMachineryUsage.Clear();
                    foreach (var item in _allAssessments)
                    {
                        HistorialMachineryUsage.Add(item);
                    }
                    OnPropertyChanged(nameof(HistoryCount));
                    OnPropertyChanged(nameof(HasHistoryItems));
                });
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