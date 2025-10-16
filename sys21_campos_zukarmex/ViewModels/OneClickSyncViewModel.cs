using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using sys21_campos_zukarmex.Models.DTOs.Sync;
using sys21_campos_zukarmex.Services;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class OneClickSyncViewModel : BaseViewModel
    {
        private readonly SyncService _syncService;

        public OneClickSyncViewModel(SyncService syncService)
        {
            _syncService = syncService;
            Title = "Sincronización Manual";
            SyncStatuses = new ObservableCollection<SyncStatus>();
            SyncMessage = "Presione 'Actualizar Catálogos' para comenzar.";
        }

        [ObservableProperty]
        private ObservableCollection<SyncStatus> syncStatuses;

        [ObservableProperty]
        private int overallProgress;

        [ObservableProperty]
        private bool isSyncCompleted = true;

        [ObservableProperty]
        private string syncMessage;

        [RelayCommand]
        public async Task StartSyncAsync()
        {
            if (IsBusy) return;
            SetBusy(true);
            IsSyncCompleted = false;
            SyncMessage = "Sincronizando catálogos...";
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

                if (finalStatuses.All(s => s.IsSuccess))
                {
                    SyncMessage = "Sincronización completada exitosamente";
                    OverallProgress = 100;
                }
                else
                {
                    SyncMessage = "Sincronización completada con errores";
                }
            }
            catch (Exception ex)
            {
                SyncMessage = $"Error durante la sincronización: {ex.Message}";
            }
            finally
            {
                SetBusy(false);
                IsSyncCompleted = true; // Permite volver a presionar el botón
            }
        }
    }
}