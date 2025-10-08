using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class ValeViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly ApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly ConnectivityService _connectivityService;
        public ConnectivityService ConnectivitySvc => _connectivityService;

        private bool isInitialized = false;

        [ObservableProperty]
        private ObservableCollection<Empresa> empresas;

        [ObservableProperty]
        private ObservableCollection<Campo> campos;

        
        [ObservableProperty]
        private ObservableCollection<Maquinaria> equipos; 
        

        [ObservableProperty]
        private Empresa? selectedEmpresa;

        [ObservableProperty]
        private Campo? selectedCampo;

        [ObservableProperty]
        private Maquinaria? selectedEquipo;

        [ObservableProperty]
        private DateTime fecha = DateTime.Now;

        public ValeViewModel(DatabaseService databaseService, ApiService apiService, SessionService sessionService, ConnectivityService connectivityService)
        {
            _databaseService = databaseService;
            _apiService = apiService;
            _sessionService = sessionService;
            _connectivityService = connectivityService;
            Title = "uso de Maquinaria";

            empresas = new ObservableCollection<Empresa>();
            campos = new ObservableCollection<Campo>();
            equipos = new ObservableCollection<Maquinaria>();
            _ = LoadCatalogsAsync();
        }



        private async Task LoadCatalogsAsync()
        {
            Debug.WriteLine("Inicio LoadCatalogs");
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

                // Cargar todas las empresas
                var empresaList = await _databaseService.GetAllAsync<Empresa>();
                Empresas.Clear();
                foreach (var empresa in empresaList.OrderBy(z => z.Nombre))
                {
                    Debug.WriteLine("Zafras:");
                    Debug.WriteLine(empresa);
                    Empresas.Add(empresa);
                }

                // cargar todas los equipos (Maquinarias)
                var equipoList = await _databaseService.GetAllAsync<Maquinaria>();
                Equipos.Clear();
                foreach (var equipo in equipoList.OrderBy(z => z.Nombre))
                {
                    Debug.WriteLine("Zafras:");
                    Debug.WriteLine(equipo);
                    Equipos.Add(equipo);
                }

                //Cargar Predios (Campos)
                var allCamposFromDb = await _databaseService.GetAllAsync<Campo>();
                List<Campo> filteredCampos;

                if (session.TipoUsuario == 1)
                {
                    filteredCampos = allCamposFromDb;
                }
                else
                {
                    filteredCampos = allCamposFromDb.Where(c => c.IdInspector == session.IdInspector).ToList();
                }

                // Poblar la lista de Campos en la UI
                Campos.Clear();
                foreach (var campo in filteredCampos.OrderBy(c => c.Nombre))
                {
                    Campos.Add(campo);
                }
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

        [RelayCommand]
        private async Task AddCaptureAsync()
        {
           
        }

      
    }
}