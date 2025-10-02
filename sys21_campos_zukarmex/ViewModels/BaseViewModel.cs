using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using sys21_campos_zukarmex.Services;

namespace sys21_campos_zukarmex.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    protected ConnectivityService? _connectivityService;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private bool isConnected = true;

    [ObservableProperty]
    private string connectionStatus = "Conectado";

    [ObservableProperty]
    private bool isAutorizacionEnabled = true;

    [ObservableProperty]
    private bool isHistorialEnabled = true;

    [ObservableProperty]
    private bool canSyncManually = false;

    public virtual async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Inicializa el servicio de conectividad para este ViewModel
    /// </summary>
    protected virtual void InitializeConnectivity(ConnectivityService connectivityService)
    {
        _connectivityService = connectivityService;

        // Suscribirse a cambios de conectividad
        _connectivityService.ConnectivityChanged += OnConnectivityChanged;
        _connectivityService.PropertyChanged += OnConnectivityServicePropertyChanged;

        // Sincronizar estado inicial
        UpdateConnectivityState();
    }

    /// <summary>
    /// Actualiza el estado de conectividad local basado en el servicio global
    /// </summary>
    protected virtual void UpdateConnectivityState()
    {
        if (_connectivityService == null) return;

        IsConnected = _connectivityService.IsConnected;
        ConnectionStatus = _connectivityService.ConnectionStatus;
        IsAutorizacionEnabled = _connectivityService.IsAutorizacionEnabled;
        IsHistorialEnabled = _connectivityService.IsHistorialEnabled;
        CanSyncManually = _connectivityService.CanSyncManually;
    }

    /// <summary>
    /// Maneja cambios en las propiedades del servicio de conectividad
    /// </summary>
    private void OnConnectivityServicePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ConnectivityService.IsConnected) ||
            e.PropertyName == nameof(ConnectivityService.ConnectionStatus) ||
            e.PropertyName == nameof(ConnectivityService.IsAutorizacionEnabled) ||
            e.PropertyName == nameof(ConnectivityService.IsHistorialEnabled) ||
            e.PropertyName == nameof(ConnectivityService.CanSyncManually))
        {
            MainThread.BeginInvokeOnMainThread(UpdateConnectivityState);
        }
    }

    /// <summary>
    /// Maneja eventos de cambio de conectividad
    /// </summary>
    protected virtual void OnConnectivityChanged(object? sender, bool isConnected)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            OnConnectivityStateChanged(isConnected);
        });
    }

    /// <summary>
    /// Método virtual que pueden sobrescribir los ViewModels derivados para reaccionar a cambios de conectividad
    /// </summary>
    protected virtual void OnConnectivityStateChanged(bool isConnected)
    {
        // Los ViewModels derivados pueden sobrescribir este método
        System.Diagnostics.Debug.WriteLine($"?? {GetType().Name} - Conectividad cambió a: {isConnected}");
    }

    /// <summary>
    /// Verifica si una funcionalidad específica está disponible
    /// </summary>
    protected bool IsFeatureAvailable(string featureName)
    {
        return featureName.ToLower() switch
        {
            "autorizacion" => IsAutorizacionEnabled,
            "historial" => IsHistorialEnabled,
            "sync" => CanSyncManually || IsConnected,
            "vales" => true, // Siempre disponible
            _ => IsConnected
        };
    }

    /// <summary>
    /// Muestra un mensaje cuando una funcionalidad no está disponible
    /// </summary>
    protected async Task ShowFeatureUnavailableMessageAsync(string featureName)
    {
        var message = featureName.ToLower() switch
        {
            "autorizacion" => "La función de Autorización requiere conexión a internet.",
            "historial" => "La función de Historial requiere conexión a internet.",
            _ => $"La función {featureName} no está disponible sin conexión a internet."
        };

        await Shell.Current.DisplayAlert("Función No Disponible", message, "OK");
    }

    [RelayCommand]
    protected virtual async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    protected virtual async Task CheckConnectivityAsync()
    {
        if (_connectivityService != null)
        {
            await _connectivityService.CheckConnectivityAsync();
        }
    }

    protected void SetBusy(bool value)
    {
        IsBusy = value;
        IsRefreshing = value;
    }

    /// <summary>
    /// Limpia las suscripciones cuando el ViewModel se destruye
    /// </summary>
    protected virtual void Cleanup()
    {
        if (_connectivityService != null)
        {
            _connectivityService.ConnectivityChanged -= OnConnectivityChanged;
            _connectivityService.PropertyChanged -= OnConnectivityServicePropertyChanged;
        }
    }
}