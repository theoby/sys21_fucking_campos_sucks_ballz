// EN ViewModels/IrrigationLinePendingViewModel.cs

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace sys21_campos_zukarmex.ViewModels
{
    // Cambiamos el nombre de la clase y el modelo a IrrigationLine
    public partial class IrrigationLinePendingViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly ApiService _apiService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPendingItems))]
        [NotifyPropertyChangedFor(nameof(PendingCount))]
        // Usamos el modelo SalidaLineaDeRiego
        private ObservableCollection<SalidaLineaDeRiego> pendingEntries;

        // Propiedades de ayuda para la UI
        public int PendingCount => PendingEntries?.Count ?? 0;
        public bool HasPendingItems => PendingEntries?.Any() ?? false;

        public IrrigationLinePendingViewModel(DatabaseService databaseService, ApiService apiService)
        {
            _databaseService = databaseService;
            _apiService = apiService;
            PendingEntries = new ObservableCollection<SalidaLineaDeRiego>();
            Title = "Líneas de Riego Pendientes";

            Debug.WriteLine("WASAAAA");
            // Carga la lista local al iniciar
            _ = LoadPendingEntriesAsync();
        }

        // --- Comando para cargar los datos de la DB local ---
        [RelayCommand]
        public async Task LoadPendingEntriesAsync()
        {
            if (IsBusy) return;
            SetBusy(true);

            try
            {
                // Carga de la DB local, apuntando al modelo correcto
                var list = await _databaseService.GetAllAsync<SalidaLineaDeRiego>();
                Debug.WriteLine(list.Count());
                PendingEntries.Clear();

                // Ordenar por fecha para mejor visualización
                foreach (var item in list.OrderByDescending(d => d.Fecha))
                {
                    PendingEntries.Add(item);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error de Carga",
                    $"No se pudieron cargar los registros de línea de riego pendientes: {ex.Message}", "OK");
            }
            finally
            {
                SetBusy(false);
            }
        }


        // --- Comando para Enviar y Eliminar (Sincronizar) ---
        [RelayCommand]
        public async Task SendAllPendingAsync()
        {
            if (!HasPendingItems)
            {
                await Shell.Current.DisplayAlert("Sincronización", "No hay registros pendientes para enviar.", "OK");
                return;
            }

            if (IsBusy) return;
            SetBusy(true);

            // Confirmación antes de enviar
            bool confirmed = await Shell.Current.DisplayAlert("Confirmar Sincronización",
                                                                $"Se enviarán {PendingEntries.Count} registros de línea de riego. ¿Desea continuar?",
                                                                "Sí, Enviar", "Cancelar");
            if (!confirmed)
            {
                SetBusy(false);
                return;
            }

            var itemsToSend = PendingEntries.ToList();

            try
            {
                // 1. Enviar la lista completa a la API
                // NOTA: Se asume que _apiService.SendPendingIrrigationLineAsync existe y devuelve un objeto con 'Success' y 'Message'.
                var response = await _apiService.SendPendingIrrigationEntriesAsync(itemsToSend);

                if (response != null && response.Success)
                {
                    // 2. ÉXITO: Eliminar los registros de la DB local
                    await _databaseService.DeleteListAsync(itemsToSend);

                    await Shell.Current.DisplayAlert("Éxito de Sincronización",
                                                        $"Se enviaron {itemsToSend.Count} registros de riego y se eliminaron de la base de datos local.",
                                                        "OK");

                    // 3. Recargar la lista (debería quedar vacía)
                    await LoadPendingEntriesAsync();
                }
                else
                {
                    // Fallo confirmado por la API. No eliminamos los datos locales.
                    string errorMessage = response?.Message ?? "Error desconocido devuelto por la API.";
                    await Shell.Current.DisplayAlert("Error de Sincronización",
                                                        $"Error al enviar los datos. La API devolvió un error: {errorMessage}",
                                                        "OK");
                }
            }
            catch (Exception ex)
            {
                // Fallo de conexión, timeout o servicio
                await Shell.Current.DisplayAlert("Fallo de Conexión",
                                                    $"Fallo al intentar sincronizar: {ex.Message}. Verifique su conexión e intente de nuevo.",
                                                    "OK");
            }
            finally
            {
                SetBusy(false);
            }
        }

        // --- Comando para Eliminar Individualmente ---
        [RelayCommand]
        public async Task DeleteEntryAsync(SalidaLineaDeRiego entry)
        {
            if (entry == null) return;

            bool confirmed = await Shell.Current.DisplayAlert("Confirmar Eliminación",
                                                                $"¿Está seguro de que desea eliminar el registro de Línea Riego ID: {entry.IdLineaRiego} localmente?",
                                                                "Sí, Eliminar", "Cancelar");

            if (!confirmed) return;

            SetBusy(true);

            try
            {
                // 1. Eliminar de la base de datos local
                await _databaseService.DeleteAsync(entry);

                // 2. Eliminar de la colección Observable
                PendingEntries.Remove(entry);

                // Notificar que el conteo ha cambiado
                OnPropertyChanged(nameof(PendingCount));
                OnPropertyChanged(nameof(HasPendingItems));

                await Shell.Current.DisplayAlert("Éxito", $"Registro Id: {entry.Id} eliminado localmente.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error de Eliminación",
                                                    $"No se pudo eliminar el registro: {ex.Message}", "OK");
            }
            finally
            {
                SetBusy(false);
            }
        }
    }
}