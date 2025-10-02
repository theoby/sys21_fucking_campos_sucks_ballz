using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using sys21_campos_zukarmex.Models.DTOs.Sync;
using sys21_campos_zukarmex.Services;
using sys21_campos_zukarmex.Models;

namespace sys21_campos_zukarmex.ViewModels;

public partial class SyncViewModel : BaseViewModel
{
    private readonly SyncService _syncService;
    private readonly DatabaseService _databaseService;

    public SyncViewModel(SyncService syncService, DatabaseService databaseService)
    {
        _syncService = syncService;
        Title = "Sincronizacion";
        SyncStatuses = new ObservableCollection<SyncStatus>();
        _databaseService = databaseService;
    }

    [ObservableProperty]
    private ObservableCollection<SyncStatus> syncStatuses;

    [ObservableProperty]
    private int overallProgress;

    [ObservableProperty]
    private bool isSyncCompleted;

    [ObservableProperty]
    private string syncMessage = "Presiona 'Sincronizar' para comenzar";

    public override async Task InitializeAsync()
    {
        await StartSyncCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    public async Task StartSyncAsync()
    {
        if (IsBusy) return;
        SetBusy(true);
        IsSyncCompleted = false;
        SyncMessage = "Sincronizando catalogos...";
        SyncStatuses.Clear();
        OverallProgress = 0;

        try
        {
            var progress = new Progress<SyncStatus>(status =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var existingStatus = SyncStatuses.FirstOrDefault(s => s.CatalogName == status.CatalogName);
                    if (existingStatus == null)
                    {
                        SyncStatuses.Add(status);
                    }
                    else
                    {
                        // Opcional: actualizar el mensaje de estado intermedio
                        existingStatus.Status = status.Status;
                    }
                    OverallProgress = status.Progress;
                });
            });

            var finalStatuses = await _syncService.SyncAllCatalogsAsync(progress);

            foreach (var finalStatus in finalStatuses)
            {
                var uiStatus = SyncStatuses.FirstOrDefault(s => s.CatalogName == finalStatus.CatalogName);
                if (uiStatus != null)
                {
                    uiStatus.Status = finalStatus.Status;
                    uiStatus.IsCompleted = true;
                }
            }

            var allCompletedSuccessfully = finalStatuses.All(s => s.IsSuccess);
            if (allCompletedSuccessfully)
            {
                IsSyncCompleted = true;
                SyncMessage = "Sincronizacion completada exitosamente";
                OverallProgress = 100;
            }
            else
            {
                IsSyncCompleted = false;
                SyncMessage = "Sincronizacion completada con errores";
            }
        }
        catch (Exception ex)
        {
            SyncMessage = $"Error durante la sincronizacion: {ex.Message}";
            IsSyncCompleted = false; // Asegura que se pueda reintentar
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task LimpiarValesLocalesAsync()
    { 
        try
        {
            await _databaseService.ResetTableAsync<SalidaDetalle>();
            await _databaseService.ResetTableAsync<Salida>();
        }
        catch (Exception ex)
        {
        }
    }

    [RelayCommand]
    private async Task ContinueToAppAsync()
    {
        if (IsSyncCompleted)
        {
            await Shell.Current.GoToAsync("//home");
        }
    }
}