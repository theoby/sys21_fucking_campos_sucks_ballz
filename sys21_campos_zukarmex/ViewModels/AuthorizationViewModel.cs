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
    public partial class AuthorizationViewModel : BaseViewModel
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
        private ObservableCollection<Pluviometro> pluviometros;

        [ObservableProperty]
        private Empresa? selectedEmpresa;

        [ObservableProperty]
        private Empresa? selectedPluviometro;

        [ObservableProperty]
        private DateTime fecha = DateTime.Now;


        public AuthorizationViewModel(DatabaseService databaseService, ApiService apiService, SessionService sessionService, ConnectivityService connectivityService)
        {
            _databaseService = databaseService;
            _apiService = apiService;
            _sessionService = sessionService;
            _connectivityService = connectivityService;
            Title = "Precipitacion pluvial";

            empresas = new ObservableCollection<Empresa>();
            pluviometros = new ObservableCollection<Pluviometro>();
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

                // cargar todas los pluviometros
                var pluviometroList = await _databaseService.GetAllAsync<Pluviometro>();
                Pluviometros.Clear();
                foreach (var pluviometro in pluviometroList.OrderBy(z => z.Nombre))
                {
                    Debug.WriteLine("Zafras:");
                    Debug.WriteLine(pluviometro);
                    Pluviometros.Add(pluviometro);
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