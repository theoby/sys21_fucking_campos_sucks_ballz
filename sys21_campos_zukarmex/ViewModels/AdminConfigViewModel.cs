using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Models.DTOs.Sync;
using sys21_campos_zukarmex.Services;

namespace sys21_campos_zukarmex.ViewModels;

public partial class AdminConfigViewModel : BaseViewModel
{
    private readonly IConfiguracionService _configuracionService;
    private readonly SyncService _syncService;
    private readonly ApiService _apiService;
    private readonly DatabaseService _databaseService;
    private readonly SessionService _sessionService;

    public AdminConfigViewModel(
        IConfiguracionService configuracionService,
        SyncService syncService,
        ApiService apiService,
        DatabaseService databaseService,
        SessionService sessionService)
    {
        _configuracionService = configuracionService;
        _syncService = syncService;
        _apiService = apiService;
        _databaseService = databaseService;
        _sessionService = sessionService;

        Title = "Configuracion del Sistema";
        Configuraciones = new ObservableCollection<Configuracion>();
        SyncStatuses = new ObservableCollection<SyncStatus>();
        
        // Inicializar comandos
        GuardarConfiguracionCommand = new AsyncRelayCommand(GuardarConfiguracionAsync);
        SincronizarCatalogosCommand = new AsyncRelayCommand(SincronizarCatalogosAsync);
        VolverAlLoginCommand = new AsyncRelayCommand(VolverAlLoginAsync);

    }

    #region Properties

    [ObservableProperty]
    private string ruta = string.Empty;

    [ObservableProperty]
    private string dispositivo = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Configuracion> configuraciones;

    [ObservableProperty]
    private ObservableCollection<SyncStatus> syncStatuses;

    [ObservableProperty]
    private bool isSyncInProgress;

    [ObservableProperty]
    private string syncProgress = string.Empty;

    [ObservableProperty]
    private int syncProgressValue;

    [ObservableProperty]
    private string mensajeEstado = string.Empty;

    [ObservableProperty]
    private bool hasExistingConfig;

    #endregion

    #region Commands

    public IAsyncRelayCommand GuardarConfiguracionCommand { get; }
    public IAsyncRelayCommand SincronizarCatalogosCommand { get; }
    public IAsyncRelayCommand VolverAlLoginCommand { get; }

    #endregion

    #region Command Implementations

    [RelayCommand]
    private async Task PageAppearingAsync()
    {
        await LoadConfiguracionesAsync();
    }

    private async Task GuardarConfiguracionAsync()
    {
        if (IsBusy) return;

        if (string.IsNullOrWhiteSpace(Ruta))
        {
            await Shell.Current.DisplayAlert("Error", "La ruta de la API es obligatoria", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(Dispositivo))
        {
            await Shell.Current.DisplayAlert("Error", "El nombre del dispositivo es obligatorio", "OK");
            return;
        }

        if (!Uri.TryCreate(Ruta.Trim(), UriKind.Absolute, out var uri))
        {
            await Shell.Current.DisplayAlert("Error", "La ruta debe ser una URL valida (ej: https://api.ejemplo.com/)", "OK");
            return;
        }

        SetBusy(true);
        MensajeEstado = "Guardando configuracion...";

        try
        {
            var configuracion = new Configuracion
            {
                Ruta = Ruta.Trim().TrimEnd('/') + "/", // Asegurar que termine con /
                Dispositivo = Dispositivo.Trim(), // Usar el valor ingresado por el usuario
                Fecha = DateTime.Now
            };
            //Reiniciar valores
            await _databaseService.ClearTableAsync<Configuracion>();
            //Guardar Config
            var result = await _configuracionService.SaveConfiguracionAsync(configuracion);

            if (result > 0)
            {
                MensajeEstado = "Configuracion guardada exitosamente";
                
                // Actualizar la lista
                await LoadConfiguracionesAsync();
                
                // Limpiar formulario pero mantener el dispositivo para facilitar futuras configuraciones
                Ruta = string.Empty;
                // No limpiar Dispositivo para que el usuario no tenga que volver a escribirlo
                
                await Shell.Current.DisplayAlert("Exito", "Configuracion guardada correctamente", "OK");
            }
            else
            {
                MensajeEstado = "Error al guardar la configuracion";
                await Shell.Current.DisplayAlert("Error", "No se pudo guardar la configuracion", "OK");
            }
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error: {ex.Message}";
            await Shell.Current.DisplayAlert("Error", $"Error al guardar configuracion: {ex.Message}", "OK");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task SincronizarCatalogosAsync()
    {
        if (IsBusy || IsSyncInProgress) return;

        // Verificar que hay configuracion
        if (!await _configuracionService.ExisteConfiguracionAsync())
        {
            await Shell.Current.DisplayAlert("Error", "Debe guardar una configuracion antes de sincronizar", "OK");
            return;
        }

        IsSyncInProgress = true;
        SyncProgressValue = 0;
        SyncProgress = "Iniciando sincronizacion...";
        SyncStatuses.Clear();

        try
        {
            // Configurar progreso
            var progress = new Progress<SyncStatus>(status =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SyncProgress = $"Sincronizando {status.CatalogName}... {status.Progress}%";
                    SyncProgressValue = status.Progress;
                    
                    // Actualizar o agregar status
                    var existingStatus = SyncStatuses.FirstOrDefault(s => s.CatalogName == status.CatalogName);
                    if (existingStatus != null)
                    {
                        existingStatus.Progress = status.Progress;
                        existingStatus.Status = status.Status;
                        existingStatus.IsCompleted = status.IsCompleted;
                    }
                    else
                    {
                        SyncStatuses.Add(status);
                    }
                });
            });

            // Ejecutar sincronizacion
            var results = await _syncService.SyncAllCatalogsAsync(progress);

            // Verificar resultados
            var completedCount = results.Count(r => r.IsCompleted);
            var totalCount = results.Count;

            SyncProgress = $"Sincronizacion completada: {completedCount}/{totalCount} catalogos";
            SyncProgressValue = 100;

            if (completedCount == totalCount)
            {
                await Shell.Current.DisplayAlert("Exito", 
                    $"Sincronizacion completada exitosamente\n{completedCount} catalogos sincronizados", "OK");
            }
            else
            {
                var failedCount = totalCount - completedCount;
                await Shell.Current.DisplayAlert("Advertencia", 
                    $"Sincronizacion parcial\n{completedCount} exitosos, {failedCount} fallidos", "OK");
            }
        }
        catch (Exception ex)
        {
            SyncProgress = "Error en la sincronizacion";
            await Shell.Current.DisplayAlert("Error", $"Error durante la sincronizacion: {ex.Message}", "OK");
        }
        finally
        {
            IsSyncInProgress = false;
        }
    }

    private async Task VolverAlLoginAsync()
    {
        if (IsBusy) return;

        try
        {
            SetBusy(true);
            MensajeEstado = "Sincronizando empresas...";

            // Sincronizar empresas antes de volver al login
            try
            {
                System.Diagnostics.Debug.WriteLine("=== Iniciando sincronizacion de empresas ===");
                System.Diagnostics.Debug.WriteLine($"URL API: {AppConfigService.ApiBaseUrl}");
                System.Diagnostics.Debug.WriteLine($"Endpoint: {AppConfigService.EmpresasEndpoint}");
                
                var empresas = await _apiService.GetEmpresasAsync();
                
                System.Diagnostics.Debug.WriteLine($"Respuesta recibida - Total empresas: {empresas.Count}");
                
                if (empresas.Any())
                {
                    await _databaseService.ClearTableAsync<Empresa>();
                    await _databaseService.InsertAllAsync(empresas);
                    MensajeEstado = $"Se sincronizaron {empresas.Count} empresas exitosamente";
                    
                    System.Diagnostics.Debug.WriteLine($"Empresas guardadas en BD: {empresas.Count}");
                    foreach (var empresa in empresas.Take(3)) // Log primeras 3 empresas
                    {
                        System.Diagnostics.Debug.WriteLine($"  - Empresa ID: {empresa.Id}, Nombre: {empresa.Nombre ?? "Sin nombre"}");
                    }
                }
                else
                {
                    MensajeEstado = "No se encontraron empresas para sincronizar";
                    System.Diagnostics.Debug.WriteLine("API devolvio lista vacia de empresas");
                }
            }
            catch (Exception ex)
            {
                MensajeEstado = $"Error sincronizando empresas: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error en sincronizacion de empresas: {ex}");
                // Continuar aunque falle la sincronizacion de empresas
            }

            // Navegar al login y borraro Sesion para prevenir el log automatico
            await _sessionService.ClearSessionAsync();

            await Shell.Current.GoToAsync("//login");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error al regresar al login: {ex.Message}", "OK");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task LoadConfiguracionesAsync()
    {
        try
        {
            var configs = await _configuracionService.GetAllConfiguracionesAsync();
            
            Configuraciones.Clear();
            foreach (var config in configs.OrderByDescending(c => c.Fecha))
            {
                Configuraciones.Add(config);
            }

            HasExistingConfig = Configuraciones.Any();
            
            // Cargar la configuracion mas reciente en los campos
            var configActiva = Configuraciones.FirstOrDefault();
            if (configActiva != null)
            {
                Ruta = configActiva.Ruta;
                Dispositivo = configActiva.Dispositivo;
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error al cargar configuraciones: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task LimpiarMemoriaAsync()
    {
        if (IsBusy) return;

        bool confirm = await Shell.Current.DisplayAlert(
            "Confirmar Limpieza",
            "Seguro que quieres limpiar la memoria? Esto eliminara todos los datos locales guardados en la App.",
            "Si, Limpiar",
            "Cancelar");

        if (!confirm) return;

        SetBusy(true);
        try
        {
            await _databaseService.ResetDatabaseAsync();

            await Shell.Current.DisplayAlert("Exito", "La memoria local ha sido limpiada y la base de datos se ha reiniciado.", "OK");

            await LoadConfiguracionesAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Ocurrio un error al limpiar la memoria: {ex.Message}", "OK");
        }
        finally
        {
            SetBusy(false);
        }
    }

    #endregion

    #region Initialization

    public override async Task InitializeAsync()
    {
        // Cargar configuraciones existentes primero
        await LoadConfiguracionesAsync();
        
        // Si no hay configuraciones existentes, usar valores por defecto
        if (!HasExistingConfig)
        {
            // Solo establecer dispositivo por defecto si esta vacio
            if (string.IsNullOrWhiteSpace(Dispositivo))
            {
                Dispositivo = DeviceInfo.Name ?? "Dispositivo-Desconocido";
            }
            
            // Ruta permanece vacia para que el usuario la complete
            MensajeEstado = "Ingrese la configuracion inicial";
        }
        else
        {
            MensajeEstado = $"Se encontraron {Configuraciones.Count} configuraciones guardadas";
        }
    }

    #endregion
}