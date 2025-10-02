using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using sys21_campos_zukarmex.Models.DTOs.Api;
using sys21_campos_zukarmex.Services;
using sys21_campos_zukarmex.Views;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class HistorialViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly ConnectivityService _connectivityService;

        public HistorialViewModel(ApiService apiService, SessionService sessionService, ConnectivityService connectivityService)
        {
            _apiService = apiService;
            _sessionService = sessionService;
            _connectivityService = connectivityService;
            Title = "Historial de Vales de Salida";
            ValesHistorial = new ObservableCollection<HistorialValeItem>();
            
            // Inicializar conectividad
            InitializeConnectivity(_connectivityService);
        }

        [ObservableProperty]
        private ObservableCollection<HistorialValeItem> valesHistorial;

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private string lastUpdateTime = string.Empty;

        [ObservableProperty]
        private int totalRegistros;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private string noInternetMessage = "Esta funcion requiere conexion a internet activa.";

        private List<HistorialValeItem> _allValesHistorial = new List<HistorialValeItem>();

        public override async Task InitializeAsync()
        {
            if (!IsConnected)
            {
                await Shell.Current.DisplayAlert("Sin Conexion", 
                    "La funcion de Historial requiere conexion a internet.", "OK");
                return;
            }
            
            await CheckConnectivityAndLoadDataAsync();
        }

        protected override void OnConnectivityStateChanged(bool isConnected)
        {
            base.OnConnectivityStateChanged(isConnected);
            
            if (!isConnected)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ValesHistorial.Clear();
                    _allValesHistorial.Clear();
                    TotalRegistros = 0;
                    LastUpdateTime = string.Empty;
                });
            }
            else
            {
                // Cuando se recupera la conexion, recargar datos
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await LoadHistorialAsync();
                });
            }
        }

        [RelayCommand]
        private async Task PageAppearingAsync()
        {
            await CheckConnectivityAndLoadDataAsync();
        }

        [RelayCommand]
        private async Task LoadHistorialAsync()
        {
            if (!IsConnected)
            {
                await Shell.Current.DisplayAlert("Sin Conexion", 
                    "No se puede cargar el historial sin conexion a internet.", "OK");
                return;
            }

            SetBusy(true);
            IsRefreshing = true;

            try
            {
                System.Diagnostics.Debug.WriteLine("=== INICIANDO CARGA DE HISTORIAL ===");
                
                // Verificar token antes de la llamada
                var session = await _sessionService.GetCurrentSessionAsync();
                if (session == null || string.IsNullOrEmpty(session.Token))
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: No hay sesion activa o token invalido");
                    await Shell.Current.DisplayAlert("Error de Autenticacion", 
                        "Su sesion ha expirado. Por favor, inicie sesion nuevamente.", "OK");
                    await Shell.Current.GoToAsync("//login");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"Token presente: {session.Token.Substring(0, Math.Min(10, session.Token.Length))}...");
                System.Diagnostics.Debug.WriteLine($"Endpoint: {AppConfigService.ValesSalidasActualesEndpoint}");
                System.Diagnostics.Debug.WriteLine($"URL Base API: {AppConfigService.ApiBaseUrl}");
                
                var historialResponse = await _apiService.GetValesHistorialAsync();
                
                System.Diagnostics.Debug.WriteLine($"Respuesta de API recibida - Success: {historialResponse.Success}");
                System.Diagnostics.Debug.WriteLine($"Estado: {historialResponse.Estado}");
                System.Diagnostics.Debug.WriteLine($"Total de datos: {historialResponse.TotalDatos}");
                System.Diagnostics.Debug.WriteLine($"Mensaje: {historialResponse.Mensaje}");
                System.Diagnostics.Debug.WriteLine($"Datos count: {historialResponse.Datos?.Count ?? 0}");
                
                if (historialResponse.Success && historialResponse.Datos != null)
                {
                    _allValesHistorial = historialResponse.Datos
                        .OrderByDescending(v => v.Fecha)
                        .Take(20)
                        .ToList();
                    
                    System.Diagnostics.Debug.WriteLine($"Vales procesados: {_allValesHistorial.Count}");
                    
                    // Log some sample data
                    foreach (var vale in _allValesHistorial.Take(3))
                    {
                        System.Diagnostics.Debug.WriteLine($"Vale: ID={vale.Id}, Concepto={vale.Concepto}, Usuario={vale.Usuario}");
                    }
                    
                    ApplySearchFilter();
                    
                    TotalRegistros = historialResponse.TotalDatos;
                    LastUpdateTime = DateTime.Now.ToString("HH:mm:ss");
                    
                    System.Diagnostics.Debug.WriteLine($"UI actualizada - ValesHistorial.Count: {ValesHistorial.Count}");
                    System.Diagnostics.Debug.WriteLine($"TotalRegistros: {TotalRegistros}");
                    System.Diagnostics.Debug.WriteLine($"LastUpdateTime: {LastUpdateTime}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Error en la respuesta de la API o datos nulos");
                    
                    // Manejo especifico de errores del servidor
                    string errorMessage = historialResponse.Mensaje ?? "No se pudieron cargar los datos del historial";
                    
                    if (historialResponse.Estado == 500)
                    {
                        errorMessage = "Error interno del servidor. Por favor, intente mas tarde o verifique su conexion.";
                        System.Diagnostics.Debug.WriteLine("ERROR 500: Internal Server Error detectado");
                    }
                    else if (historialResponse.Estado == 401)
                    {
                        errorMessage = "Su sesion ha expirado. Iniciando sesion nuevamente...";
                        System.Diagnostics.Debug.WriteLine("ERROR 401: Token invalido o expirado");
                        await Shell.Current.GoToAsync("//login");
                        return;
                    }
                    else if (historialResponse.Estado == 403)
                    {
                        errorMessage = "No tiene permisos para acceder al historial de vales.";
                        System.Diagnostics.Debug.WriteLine("ERROR 403: Acceso denegado");
                    }
                    else if (historialResponse.Estado == 404)
                    {
                        errorMessage = "El endpoint del historial no fue encontrado. Verifique la configuracion del servidor.";
                        System.Diagnostics.Debug.WriteLine("ERROR 404: Endpoint no encontrado");
                    }
                    
                    await Shell.Current.DisplayAlert("Error", errorMessage, "OK");
                    
                    ValesHistorial.Clear();
                    _allValesHistorial.Clear();
                    TotalRegistros = 0;
                }
            }
            catch (HttpRequestException httpEx)
            {
                System.Diagnostics.Debug.WriteLine($"Error HTTP en LoadHistorialAsync: {httpEx}");
                await Shell.Current.DisplayAlert("Error de Conexion", 
                    "No se pudo conectar con el servidor. Verifique su conexion a internet y la configuracion del servidor.", "OK");
                
                ValesHistorial.Clear();
                _allValesHistorial.Clear();
                TotalRegistros = 0;
            }
            catch (TaskCanceledException taskEx)
            {
                System.Diagnostics.Debug.WriteLine($"Timeout en LoadHistorialAsync: {taskEx}");
                await Shell.Current.DisplayAlert("Error de Timeout", 
                    "La operacion tardo demasiado tiempo. Intente nuevamente.", "OK");
                
                ValesHistorial.Clear();
                _allValesHistorial.Clear();
                TotalRegistros = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Excepcion en LoadHistorialAsync: {ex}");
                await Shell.Current.DisplayAlert("Error", 
                    $"Error inesperado al cargar el historial: {ex.Message}", "OK");
                
                ValesHistorial.Clear();
                _allValesHistorial.Clear();
                TotalRegistros = 0;
            }
            finally
            {
                SetBusy(false);
                IsRefreshing = false;
                System.Diagnostics.Debug.WriteLine("=== FINALIZANDO CARGA DE HISTORIAL ===");
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            if (!IsConnected)
            {
                await Shell.Current.DisplayAlert("Sin Conexion", 
                    "No se pueden actualizar los datos sin conexion a internet.", "OK");
                return;
            }

            await LoadHistorialAsync();
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            ApplySearchFilter();
        }

        [RelayCommand]
        private void ClearSearch()
        {
            SearchText = string.Empty;
            ApplySearchFilter();
        }

        [RelayCommand]
        private async Task ViewValeDetailsAsync(HistorialValeItem vale)
        {
            if (vale == null) return;

            if (!IsConnected)
            {
                await Shell.Current.DisplayAlert("Sin Conexión", "Se necesita conexión a internet para ver los detalles.", "OK");
                return;
            }

            SetBusy(true);
            try
            {
                var detalles = await _apiService.GetValeDetallesAsync(vale.Id);

                if (detalles.Any())
                {
                    var popup = new ValeDetallesPopup(detalles);

                    await Shell.Current.CurrentPage.ShowPopupAsync(popup);
                }
                else
                {
                    await Shell.Current.DisplayAlert("Sin Detalles", "No se encontraron artículos para este vale.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudieron cargar los detalles: {ex.Message}", "OK");
            }
            finally
            {
                SetBusy(false);
            }
        }

        [RelayCommand]
        private async Task CheckConnectionAsync()
        {
            await _connectivityService.CheckConnectivityAsync();
            
            if (IsConnected)
            {
                await Shell.Current.DisplayAlert("Conexion Verificada", 
                    "La conexion a internet esta activa. Puede cargar el historial.", "OK");
                await LoadHistorialAsync();
            }
            else
            {
                await Shell.Current.DisplayAlert("Sin Conexion", 
                    "No hay conexion a internet disponible.", "OK");
            }
        }

        [RelayCommand]
        private async Task DiagnosticInfoAsync()
        {
            try
            {
                var session = await _sessionService.GetCurrentSessionAsync();
                var diagnostic = "=== INFORMACION DE DIAGNOSTICO ===\n\n";
                
                diagnostic += $"Conectividad: {(IsConnected ? "Activa" : "Sin conexion")}\n";
                diagnostic += $"URL Base API: {AppConfigService.ApiBaseUrl}\n";
                diagnostic += $"Endpoint Historial: {AppConfigService.ValesSalidasActualesEndpoint}\n";
                diagnostic += $"URL Completa: {AppConfigService.ApiBaseUrl}{AppConfigService.ValesSalidasActualesEndpoint}\n\n";
                
                if (session != null)
                {
                    diagnostic += $"Usuario: {session.NombreCompleto}\n";
                    diagnostic += $"Empresa ID: {session.IdEmpresa}\n";
                    diagnostic += $"Tipo Usuario: {session.TipoUsuario}\n";
                    diagnostic += $"Token presente: {(!string.IsNullOrEmpty(session.Token) ? "Si" : "No")}\n";
                    
                    if (!string.IsNullOrEmpty(session.Token))
                    {
                        diagnostic += $"Token (primeros 10 chars): {session.Token.Substring(0, Math.Min(10, session.Token.Length))}...\n";
                    }
                    
                    diagnostic += $"Fecha expiracion: {session.ExpirationDate:yyyy-MM-dd HH:mm:ss}\n";
                    diagnostic += $"Token expirado: {(session.ExpirationDate < DateTime.Now ? "Si" : "No")}\n";
                }
                else
                {
                    diagnostic += "No hay sesion activa\n";
                }
                
                diagnostic += $"\nUltimo error: {LastUpdateTime}\n";
                diagnostic += $"Total registros cargados: {TotalRegistros}\n";
                diagnostic += $"Registros mostrados: {ValesHistorial.Count}";
                
                await Shell.Current.DisplayAlert("Diagnostico", diagnostic, "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error en Diagnostico", 
                    $"Error obteniendo informacion de diagnostico: {ex.Message}", "OK");
            }
        }

        private async Task CheckConnectivityAndLoadDataAsync()
        {
            if (IsConnected)
            {
                await LoadHistorialAsync();
            }
            else
            {
                ValesHistorial.Clear();
                _allValesHistorial.Clear();
                TotalRegistros = 0;
                
                await Shell.Current.DisplayAlert("Sin Internet", 
                    "Esta funcion requiere conexion a internet activa. " +
                    "Verifique su conexion e intente nuevamente.", "OK");
            }
        }

        private void ApplySearchFilter()
        {
            System.Diagnostics.Debug.WriteLine($"=== APLICANDO FILTRO DE BUSQUEDA ===");
            System.Diagnostics.Debug.WriteLine($"SearchText: '{SearchText}'");
            System.Diagnostics.Debug.WriteLine($"_allValesHistorial.Count: {_allValesHistorial.Count}");
            
            ValesHistorial.Clear();

            IEnumerable<HistorialValeItem> filteredVales = _allValesHistorial;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filteredVales = _allValesHistorial.Where(v =>
                    v.Concepto.ToLower().Contains(searchLower) ||
                    v.Usuario.ToLower().Contains(searchLower) ||
                    v.Predio.ToLower().Contains(searchLower) ||
                    v.Almacen.ToLower().Contains(searchLower) ||
                    v.Id.ToString().Contains(searchLower)
                );
            }

            foreach (var vale in filteredVales)
            {
                ValesHistorial.Add(vale);
                System.Diagnostics.Debug.WriteLine($"Agregado a ValesHistorial: {vale.Id} - {vale.Concepto}");
            }

            System.Diagnostics.Debug.WriteLine($"Filtro aplicado: {ValesHistorial.Count} de {_allValesHistorial.Count} registros");
            System.Diagnostics.Debug.WriteLine($"=== FIN FILTRO DE BUSQUEDA ===");
        }

        partial void OnSearchTextChanged(string value)
        {
            // Auto-search cuando el usuario escribe
            if (string.IsNullOrWhiteSpace(value) || value.Length >= 2)
            {
                ApplySearchFilter();
            }
        }
    }
}