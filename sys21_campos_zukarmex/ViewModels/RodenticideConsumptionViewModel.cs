using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class RodenticideConsumptionViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly ApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly ConnectivityService _connectivityService;

        public ConnectivityService ConnectivitySvc => _connectivityService;

        [ObservableProperty]
        private ObservableCollection<Zafra> zafras = new();

        [ObservableProperty]
        private ObservableCollection<Campo> campos = new();

        [ObservableProperty]
        private Zafra? selectedZafra;

        [ObservableProperty]
        private Campo? selectedCampos;

        [ObservableProperty]
        private DateTime fecha = DateTime.Now;

        [ObservableProperty]
        private string cantidadComederos = "0";

        [ObservableProperty]
        private string cantidadPastillas = "0";

        [ObservableProperty]
        private string consumo = "0";

        [ObservableProperty]
        private bool areFieldsLocked = false;

        public RodenticideConsumptionViewModel(
            DatabaseService databaseService,
            ApiService apiService,
            SessionService sessionService,
            ConnectivityService connectivityService)
        {
            _databaseService = databaseService;
            _apiService = apiService;
            _sessionService = sessionService;
            _connectivityService = connectivityService;

            _ = LoadCatalogsAsync();
        }

        private async Task LoadCatalogsAsync()
        {
            if (IsBusy) return;

            try
            {
                SetBusy(true);

                var session = await _sessionService.GetCurrentSessionAsync();
                if (session == null)
                {
                    await Shell.Current.DisplayAlert("Error de Sesión", "No se pudo obtener la sesión del usuario.", "OK");
                    return;
                }

                // Cargar Zafras desde DB
                var zafraList = await _databaseService.GetAllAsync<Zafra>();
                Zafras.Clear();
                foreach (var zafra in zafraList.OrderBy(z => z.Nombre))
                    Zafras.Add(zafra);

                // Cargar Campos según sesión
                var allCampos = await _databaseService.GetAllAsync<Campo>();
                var filteredCampos = session.TipoUsuario == 1
                    ? allCampos
                    : allCampos.Where(c => c.IdInspector == session.IdInspector).ToList();

                Campos.Clear();
                foreach (var campo in filteredCampos.OrderBy(c => c.Nombre))
                    Campos.Add(campo);
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

    } 
    
}
