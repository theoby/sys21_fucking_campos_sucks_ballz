using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;

namespace sys21_campos_zukarmex.ViewModels;

public partial class StatusViewModel : BaseViewModel
{
    private readonly ApiService _apiService;
    private readonly DatabaseService _databaseService;

    public StatusViewModel(ApiService apiService, DatabaseService databaseService)
    {
        _apiService = apiService;
        _databaseService = databaseService;
        Title = "Estado de Vales";
        PendingVales = new ObservableCollection<Salida>();
    }

    [ObservableProperty]
    private ObservableCollection<Salida> pendingVales;

    [ObservableProperty]
    private int totalPending;

    [ObservableProperty]
    private int totalSynced;

    [ObservableProperty]
    private string lastUpdateTime = string.Empty;

    [ObservableProperty]
    private bool isRefreshing;

    [RelayCommand]
    private async Task PageAppearingAsync()
    {
        await LoadStatusAsync();
    }

    [RelayCommand]
    private async Task LoadStatusAsync()
    {
        IsRefreshing = true;
        try
        {
            var localVales = await _databaseService.GetAllAsync<Salida>();
            var pendingList = localVales.Where(v => !v.Status).ToList();

            PendingVales.Clear();
            foreach (var vale in pendingList.OrderByDescending(v => v.Fecha))
            {
                PendingVales.Add(vale);
            }

            TotalPending = PendingVales.Count;
            TotalSynced = localVales.Count - TotalPending;
            LastUpdateTime = DateTime.Now.ToString("HH:mm:ss");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error al cargar estado: {ex.Message}", "OK");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task SyncPendingValesAsync()
    {
        if (IsBusy) return;

        if (!PendingVales.Any())
        {
            await Shell.Current.DisplayAlert("Informacion", "No hay vales pendientes para sincronizar.", "OK");
            return;
        }

        var confirm = await Shell.Current.DisplayAlert("Confirmar Sincronizacion",
            $"Se encontraron {PendingVales.Count} vales pendientes. Deseas intentar sincronizarlos ahora?",
            "Si, Sincronizar", "Cancelar");

        if (!confirm) return;

        SetBusy(true);
        var ValesExitosos = new List<string>();
        var ValesFallidos = new List<string>();

        // Creamos una copia de la lista para poder iterar sobre ella de forma segura
        var valesASincronizar = PendingVales.ToList();

        foreach (var vale in valesASincronizar)
        {
            try
            {
           
                vale.SalidaDetalle = await _databaseService.GetDetallesBySalidaAsync(vale.Id);

                // Reutilizamos el metodo del ApiService
                var apiResponse = await _apiService.SaveValeAsync(vale);

                if (apiResponse.Success)
                {
                    ValesExitosos.Add($"Temporal id-Vale #{vale.Id} ({vale.Concepto})");
                    //Eliminamos de la bd Local para que no este en pendiente
                    await _databaseService.DeleteDetallesBySalidaAsync(vale.Id);
                    await _databaseService.DeleteAsync(vale);
                }
                else
                {
                    ValesFallidos.Add($"Temporal id-Vale #{vale.Id}: {apiResponse.Message}");
                }
            }
            catch (Exception ex)
            {
                ValesFallidos.Add($"Temporal id-Vale #{vale.Id}: {ex.Message}");
            }
        }

        // Al terminar el bucle, refrescamos la lista de pendientes
        await LoadStatusAsync();

        // Construimos y mostramos el mensaje de resumen
        var resumen = new StringBuilder();
        resumen.AppendLine($"Sincronizacion finalizada.\n");
        resumen.AppendLine($"Exitos: {ValesExitosos.Count}");
        resumen.AppendLine($"Fallidos: {ValesFallidos.Count}\n");

        if (ValesExitosos.Any())
        {
            resumen.AppendLine("Vales Sincronizados:");
            foreach (var exito in ValesExitosos)
            {
                resumen.AppendLine($"- {exito}");
            }
        }

        if (ValesFallidos.Any())
        {
            resumen.AppendLine("\nVales con Error:");
            foreach (var fallo in ValesFallidos)
            {
                resumen.AppendLine($"- {fallo}");
            }
        }

        await Shell.Current.DisplayAlert("Resumen de Sincronizacion", resumen.ToString(), "OK");


        await LoadStatusAsync();
        SetBusy(false);
    }


    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadStatusAsync();
    }

    [RelayCommand]
    private async Task ViewValeDetailsAsync(Salida vale)
    {
        if (vale == null) return;

        var details =$"Temporal id-Vale: {vale.Id}\n" +
                     $"Fecha: {vale.Fecha:dd/MM/yyyy}\n" +
                     $"Concepto: {vale.Concepto}\n" +
                     $"Usuario: {vale.Usuario}\n" +
                     $"Estado: {(vale.Status ? "Sincronizado" : "Pendiente")}";

        await Shell.Current.DisplayAlert("Detalles del Vale", details, "OK");
    }
}