using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Models.DTOs.Api;
using sys21_campos_zukarmex.Services;
using sys21_campos_zukarmex.Views;

namespace sys21_campos_zukarmex.ViewModels;

public partial class AuthorizationViewModel : BaseViewModel
{
    private readonly ApiService _apiService;
    private readonly DatabaseService _databaseService;
    private readonly SessionService _sessionService;
    private readonly ConnectivityService _connectivityService;

    public AuthorizationViewModel(ApiService apiService, DatabaseService databaseService, SessionService sessionService, ConnectivityService connectivityService)
    {
        _apiService = apiService;
        _databaseService = databaseService;
        _sessionService = sessionService;
        _connectivityService = connectivityService;
        Title = "Autorizacion de Vales";
        ValesForAuthorization = new ObservableCollection<ValePendienteApiResponse>();
        
        // Inicializar conectividad
        InitializeConnectivity(_connectivityService);
    }

    [ObservableProperty]
    private ObservableCollection<ValePendienteApiResponse> valesForAuthorization;

    [ObservableProperty]
    private int totalPendingAuth;

    [ObservableProperty]
    private bool canAuthorize;

    [ObservableProperty]
    private string userRole = string.Empty;

    [ObservableProperty]
    private string noInternetMessage = "Esta funcion requiere conexion a internet activa.";

    public override async Task InitializeAsync()
    {
        await CheckAuthorizationPermissionsAsync();
        
        if (!IsConnected)
        {
            await Shell.Current.DisplayAlert("Sin Conexion", 
                "La funcion de Autorizacion requiere conexion a internet.", "OK");
            return;
        }
        
        if (CanAuthorize)
        {
            await LoadValesForAuthorizationAsync();
        }
    }

    protected override void OnConnectivityStateChanged(bool isConnected)
    {
        base.OnConnectivityStateChanged(isConnected);
        
        if (!isConnected)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ValesForAuthorization.Clear();
                TotalPendingAuth = 0;
            });
        }
        else
        {
            // Cuando se recupera la conexion, recargar datos
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (CanAuthorize)
                {
                    await LoadValesForAuthorizationAsync();
                }
            });
        }
    }

    [RelayCommand]
    private async Task CheckAuthorizationPermissionsAsync()
    {
        try
        {
            CanAuthorize = await _sessionService.CanAuthorizeAsync();
            UserRole = await _sessionService.GetCurrentUserRoleAsync();
            
            if (!CanAuthorize)
            {
                await Shell.Current.DisplayAlert("Acceso Denegado", 
                    "No tiene permisos para autorizar vales", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", 
                $"Error al verificar permisos: {ex.Message}", "OK");
            CanAuthorize = false;
        }
    }

    [RelayCommand]
    private async Task LoadValesForAuthorizationAsync()
    {
        if (!CanAuthorize) return;

        if (!IsConnected)
        {
            await Shell.Current.DisplayAlert("Sin Conexion", 
                "No se pueden cargar los vales sin conexion a internet.", "OK");
            return;
        }

        SetBusy(true);
        try
        {
            var vales = await _apiService.GetValePendientesAsync();
            if (vales == null)
            {
                vales = new List<ValePendienteApiResponse>();
            }
            ValesForAuthorization.Clear();
            foreach (var vale in vales.OrderByDescending(v => v.Fecha))
            {
                ValesForAuthorization.Add(new ValePendienteApiResponse
                {
                    Id = vale.Id,
                    Predio = vale.Predio?.ToUpperInvariant() ?? string.Empty,
                    Almacen = vale.Almacen?.ToUpperInvariant() ?? string.Empty,
                    Fecha = vale.Fecha,
                    Concepto = vale.Concepto?.ToUpperInvariant() ?? string.Empty,
                    Usuario = vale.Usuario?.ToUpperInvariant() ?? string.Empty,
                    Estatus = vale.Estatus,
                });
            }

            TotalPendingAuth = ValesForAuthorization.Count;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", 
                $"Error al cargar vales para autorizacion: {ex.Message}", "OK");
        }
        finally
        {
            SetBusy(false);
        }
    }
    
    [RelayCommand]
    private async Task AuthorizeValeAsync(ValePendienteApiResponse vale)
    {
        if (vale == null || !CanAuthorize) return;

        if (!IsConnected)
        {
            await Shell.Current.DisplayAlert("Sin Conexion", 
                "No se puede autorizar vales sin conexion a internet.", "OK");
            return;
        }

        var confirm = await Shell.Current.DisplayAlert("Confirmar Autorizacion", 
            $"Autorizar el vale ID: {vale.Id}?\nConcepto: {vale.Concepto}", 
            "Autorizar", "Cancelar");

        if (!confirm) return;

        await ProcessAuthorizationAsync(vale, true);
    }

    [RelayCommand]
    private async Task RejectValeAsync(ValePendienteApiResponse vale)
    {
        if (vale == null || !CanAuthorize) return;

        if (!IsConnected)
        {
            await Shell.Current.DisplayAlert("Sin Conexion", 
                "No se puede rechazar vales sin conexion a internet.", "OK");
            return;
        }

        var confirm = await Shell.Current.DisplayAlert("Confirmar Rechazo", 
            $"Rechazar el vale ID: {vale.Id}?\nConcepto: {vale.Concepto}", 
            "Rechazar", "Cancelar");

        if (!confirm) return;

        await ProcessAuthorizationAsync(vale, false);
    }

    [RelayCommand]
    private async Task ViewValeDetailsAsync(ValePendienteApiResponse vale)
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
    private async Task RefreshAsync()
    {
        if (!IsConnected)
        {
            await Shell.Current.DisplayAlert("Sin Conexion", 
                "No se pueden actualizar los datos sin conexion a internet.", "OK");
            return;
        }

        await LoadValesForAuthorizationAsync();
    }

    [RelayCommand]
    private async Task CheckConnectionAsync()
    {
        await _connectivityService.CheckConnectivityAsync();
        
        if (IsConnected)
        {
            await Shell.Current.DisplayAlert("Conexion Verificada", 
                "La conexion a internet esta activa. Puede usar todas las funciones.", "OK");
        }
        else
        {
            await Shell.Current.DisplayAlert("Sin Conexion", 
                "No hay conexion a internet disponible.", "OK");
        }
    }

    private async Task ProcessAuthorizationAsync(ValePendienteApiResponse vale, bool authorize)
    {
        if (!IsConnected)
        {
            await Shell.Current.DisplayAlert("Sin Conexion", 
                "No se puede procesar la autorizacion sin conexion a internet.", "OK");
            return;
        }

        SetBusy(true);
        try
        {
            Debug.WriteLine($"Vale a AutorizaRRRR: {vale.Id}");
            var response = await _apiService.AuthorizeValeAsync(vale.Id, authorize);
            
            if (response.Success)
            {
                ValesForAuthorization.Remove(vale);
                TotalPendingAuth = ValesForAuthorization.Count;
                
                var action = authorize ? "autorizado" : "rechazado";
                await Shell.Current.DisplayAlert("Exito", 
                    $"Vale {action} correctamente", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", 
                    response.Message ?? "Error al procesar autorizacion", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", 
                $"Error al procesar autorizacion: {ex.Message}", "OK");
        }
        finally
        {
            SetBusy(false);
            await RefreshAsync();
        }
    }
}