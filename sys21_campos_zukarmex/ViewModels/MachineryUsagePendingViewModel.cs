using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sys21_campos_zukarmex.ViewModels
{
    public partial class MachineryUsagePendingViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly ApiService _apiService;
        private readonly SessionService _sessionService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPendingItems), nameof(PendingCount))]
        private ObservableCollection<SalidaMaquinaria> pendingMachineryUsages = new();


        [ObservableProperty]
        private bool isRefreshing;


        public int PendingCount => PendingMachineryUsages.Count;
        public bool HasPendingItems => PendingMachineryUsages.Any();

        public MachineryUsagePendingViewModel(DatabaseService databaseService, ApiService apiService, SessionService sessionService)
        {
            _databaseService = databaseService;
            _apiService = apiService;
            _sessionService = sessionService;
            Title = "Usos Pendientes";
        }

        [RelayCommand]
        public async Task LoadPendingMachineryUsagesAsync()
        {
            if (IsBusy) return;
            SetBusy(true);
            try
            {
                var session = await _sessionService.GetCurrentSessionAsync();
                var empresaList = await _databaseService.GetAllAsync<Empresa>();
                var equipoList = await _databaseService.GetAllAsync<Maquinaria>();
                var allCampos = await _databaseService.GetAllAsync<Campo>();
                var filteredCampos = session.TipoUsuario == 1 ? allCampos : allCampos.Where(c => c.IdInspector == session.IdInspector).ToList();

                var list = await _databaseService.GetAllAsync<SalidaMaquinaria>();

                foreach (var item in list)
                {
                    item.CampoNombre = filteredCampos.FirstOrDefault(c => c.Id == item.IdCampo)?.Nombre ?? "Predio N/D";
                    item.MaquinariaNombre = equipoList.FirstOrDefault(m => m.IdPk == item.IdMaquinaria)?.Nombre ?? "Equipo N/D";
                    var equipo = equipoList.FirstOrDefault(m => m.IdPk == item.IdMaquinaria);
                    item.EmpresaNombre = empresaList.FirstOrDefault(e => e.Id == equipo?.IdGrupo)?.Nombre ?? "Empresa N/D";
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    PendingMachineryUsages.Clear();
                    foreach (var item in list.OrderByDescending(i => i.Fecha))
                    {
                        PendingMachineryUsages.Add(item);
                    }
                    OnPropertyChanged(nameof(PendingCount));
                    OnPropertyChanged(nameof(HasPendingItems));
                });
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudieron cargar los registros: {ex.Message}", "OK");
            }
            finally
            {
                SetBusy(false);
            }
        }

        [RelayCommand]
        public async Task SendAllPendingAsync()
        {
            if (!HasPendingItems || !await Shell.Current.DisplayAlert("Confirmar", $"Se enviarán {PendingCount} registros. ¿Desea continuar?", "Sí, Enviar", "No")) return;

            if (IsBusy) return;
            SetBusy(true);

            var itemsToSend = PendingMachineryUsages.ToList();
            try
            {
                var response = await _apiService.SendPendingMachineryUsageAsync(itemsToSend);

                if (response.Success)
                {
                    await _databaseService.DeleteListAsync(itemsToSend);
                    await Shell.Current.DisplayAlert("Éxito", $"Se enviaron {itemsToSend.Count} registros correctamente.", "OK");

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        PendingMachineryUsages.Clear();
                        OnPropertyChanged(nameof(PendingCount));
                        OnPropertyChanged(nameof(HasPendingItems));
                    });
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error de API", $"La API devolvió un error: {response.Message}", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fallo de Conexión", $"Error al sincronizar: {ex.Message}", "OK");
            }
            finally
            {
                SetBusy(false);
            }
        }

        [RelayCommand]
        public async Task DeleteMachineryUsagesAsync(SalidaMaquinaria use)
        {
            if (use == null || !await Shell.Current.DisplayAlert("Confirmar", $"¿Eliminar registro local ID: {use.Id}?", "Sí, Eliminar", "No")) return;

            SetBusy(true);
            try
            {
                var result = await _databaseService.DeleteAsync(use);
                if (result > 0)
                {
                    // Actualizar la UI en el hilo principal
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        PendingMachineryUsages.Remove(use);
                        OnPropertyChanged(nameof(PendingCount));
                        OnPropertyChanged(nameof(HasPendingItems));
                    });
                    
                    await Shell.Current.DisplayAlert("Éxito", $"Registro ID: {use.Id} eliminado.", "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", "No se pudo eliminar el registro de la base de datos.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudo eliminar: {ex.Message}", "OK");
            }
            finally
            {
                SetBusy(false);
            }
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            // si ya está ocupado no hacemos nada
            if (IsBusy) return;

            try
            {
                IsRefreshing = true;
                await LoadPendingMachineryUsagesAsync();
            }
            finally
            {
                // Garantizar que se apague el indicador aunque falle
                IsRefreshing = false;
            }
        }
    }

}