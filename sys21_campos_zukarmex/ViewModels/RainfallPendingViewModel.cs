using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;



namespace sys21_campos_zukarmex.ViewModels
{
    public partial class RainfallPendingViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly ApiService _apiService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPendingItems), nameof(PendingCount))]
        private ObservableCollection<SalidaPrecipitacion> pendingRainfalls = new();

        [ObservableProperty]
        private bool isRefreshing;

        public int PendingCount => PendingRainfalls.Count;
        public bool HasPendingItems => PendingRainfalls.Any();

        public RainfallPendingViewModel(DatabaseService databaseService, ApiService apiService)
        {
            _databaseService = databaseService;
            _apiService = apiService;
            Title = "Precipitaciones Pendientes";
        }

        [RelayCommand]
        public async Task LoadPendingAsync()
        {
            if (IsBusy) return;
            SetBusy(true);
            try
            {
                var empresaList = await _databaseService.GetAllAsync<Empresa>();
                var pluviometroList = await _databaseService.GetAllAsync<Pluviometro>();

                var list = await _databaseService.GetAllAsync<SalidaPrecipitacion>();

                foreach (var item in list)
                {
                    item.EmpresaNombre = empresaList.FirstOrDefault(e => e.Id == item.IdEmpresa)?.Nombre ?? "Empresa N/D";
                    item.PluviometroNombre = pluviometroList.FirstOrDefault(p => p.Id == item.IdPluviometro)?.Nombre ?? "Pluviómetro N/D";
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    PendingRainfalls.Clear();
                    foreach (var item in list.OrderByDescending(i => i.Fecha)) PendingRainfalls.Add(item);
                    OnPropertyChanged(nameof(PendingCount));
                });
            }
            catch (Exception ex) { await Shell.Current.DisplayAlert("Error", $"Error al cargar: {ex.Message}", "OK"); }
            finally { SetBusy(false); }
        }

        [RelayCommand]
        public async Task SendAllPendingAsync()
        {
            if (!HasPendingItems || !await Shell.Current.DisplayAlert("Confirmar", $"Enviar {PendingCount} registros?", "Sí", "No")) return;
            if (IsBusy) return;
            SetBusy(true);

            var itemsToSend = PendingRainfalls.ToList();
            try
            {
                var response = await _apiService.SendPendingRainfallsAsync(itemsToSend);
                if (response.Success)
                {
                    await _databaseService.DeleteListAsync(itemsToSend);
                    await Shell.Current.DisplayAlert("Éxito", $"Se enviaron {itemsToSend.Count} registros.", "OK");
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        PendingRainfalls.Clear();
                        OnPropertyChanged(nameof(PendingCount));
                    });
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error de API", response.Message, "OK");
                }
            }
            catch (Exception ex) { await Shell.Current.DisplayAlert("Error", $"Fallo al sincronizar: {ex.Message}", "OK"); }
            finally { SetBusy(false); }
        }

        [RelayCommand]
        public async Task DeleteAsync(SalidaPrecipitacion rainfall)
        {
            if (rainfall == null || !await Shell.Current.DisplayAlert("Confirmar", $"¿Eliminar registro ID: {rainfall.Id}?", "Sí", "No")) return;
            SetBusy(true);
            try
            {
                if (await _databaseService.DeleteAsync(rainfall) > 0)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        PendingRainfalls.Remove(rainfall);
                        OnPropertyChanged(nameof(PendingCount));
                    });
                }
            }
            catch (Exception ex) { await Shell.Current.DisplayAlert("Error", $"No se pudo eliminar: {ex.Message}", "OK"); }
            finally { SetBusy(false); }
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            if (IsBusy) return;

            try
            {
                IsRefreshing = true;
                await LoadPendingAsync();
            }
            finally
            {
                IsRefreshing = false;
            }
        }
    }
}