using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Models.DTOs.Search;
using sys21_campos_zukarmex.Models.DTOs.Sync;
using sys21_campos_zukarmex.Services;
using sys21_campos_zukarmex.Services.Repositories;

namespace sys21_campos_zukarmex.ViewModels;

/// <summary>
/// Example ViewModel showing how to use the new CRUD system
/// </summary>
public partial class CatalogExampleViewModel : BaseViewModel
{
    private readonly CatalogService _catalogService;
    private readonly IAlmacenRepository _almacenRepository;
    private readonly IArticuloRepository _articuloRepository;

    public CatalogExampleViewModel(
        CatalogService catalogService,
        IAlmacenRepository almacenRepository,
        IArticuloRepository articuloRepository)
    {
        _catalogService = catalogService;
        _almacenRepository = almacenRepository;
        _articuloRepository = articuloRepository;
        
        Title = "Gesti�n de Cat�logos";
        LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
        SearchCommand = new AsyncRelayCommand<string>(SearchAsync);
        SaveAlmacenCommand = new AsyncRelayCommand(SaveAlmacenAsync);
        DeleteAlmacenCommand = new AsyncRelayCommand<Almacen>(DeleteAlmacenAsync);
        SyncCatalogCommand = new AsyncRelayCommand<string>(SyncCatalogAsync);
    }

    #region Properties

    [ObservableProperty]
    private ObservableCollection<Almacen> almacenes = new();

    [ObservableProperty]
    private ObservableCollection<Articulo> articulos = new();

    [ObservableProperty]
    private Almacen selectedAlmacen = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private SyncStatistics? syncStats;

    [ObservableProperty]
    private bool isSearching;

    #endregion

    #region Commands

    public IAsyncRelayCommand LoadDataCommand { get; }
    public IAsyncRelayCommand<string> SearchCommand { get; }
    public IAsyncRelayCommand SaveAlmacenCommand { get; }
    public IAsyncRelayCommand<Almacen> DeleteAlmacenCommand { get; }
    public IAsyncRelayCommand<string> SyncCatalogCommand { get; }

    #endregion

    #region Command Implementations

    private async Task LoadDataAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            // Load almacenes using the repository
            var almacenesList = await _almacenRepository.GetAllAsync();
            Almacenes.Clear();
            foreach (var almacen in almacenesList)
            {
                Almacenes.Add(almacen);
            }

            // Load articulos using the catalog service
            var articulosList = await _catalogService.GetAllAsync<Articulo>();
            Articulos.Clear();
            foreach (var articulo in articulosList)
            {
                Articulos.Add(articulo);
            }

            // Load sync statistics
            SyncStats = await _catalogService.GetSyncStatisticsAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error cargando datos: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SearchAsync(string? searchTerm)
    {
        if (IsSearching) return;

        try
        {
            IsSearching = true;

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                await LoadDataAsync();
                return;
            }

            // Use the catalog service search functionality
            var searchRequest = new SearchRequest
            {
                SearchTerm = searchTerm,
                Page = 1,
                PageSize = 50,
                SortBy = "Nombre"
            };

            var searchResult = await _catalogService.SearchAsync<Almacen>(searchRequest);
            
            Almacenes.Clear();
            foreach (var almacen in searchResult.Data)
            {
                Almacenes.Add(almacen);
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error en b�squeda: {ex.Message}", "OK");
        }
        finally
        {
            IsSearching = false;
        }
    }

    private async Task SaveAlmacenAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            // Validate the almacen
            var validation = await _catalogService.ValidateAsync(SelectedAlmacen);
            if (!validation.IsValid)
            {
                await Shell.Current.DisplayAlert("Validaci�n", 
                    string.Join("\n", validation.Errors), "OK");
                return;
            }

            // Save using the catalog service
            var result = await _catalogService.SaveAsync(SelectedAlmacen);
            
            if (result.Success)
            {
                await Shell.Current.DisplayAlert("�xito", 
                    result.Message, "OK");
                
                // Refresh the list
                await LoadDataAsync();
                
                // Clear the form
                SelectedAlmacen = new Almacen();
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", 
                    result.Message, "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", 
                $"Error guardando almac�n: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteAlmacenAsync(Almacen? almacen)
    {
        if (almacen == null || IsBusy) return;

        try
        {
            var confirm = await Shell.Current.DisplayAlert("Confirmar", 
                $"�Est� seguro de eliminar el almac�n '{almacen.Nombre}'?", 
                "S�", "No");
            
            if (!confirm) return;

            IsBusy = true;

            // Delete using the catalog service
            var result = await _catalogService.DeleteAsync<Almacen>(almacen.Id);
            
            if (result.Success)
            {
                await Shell.Current.DisplayAlert("�xito", 
                    "Almac�n eliminado correctamente", "OK");
                
                // Remove from collection
                Almacenes.Remove(almacen);
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", 
                    result.Message, "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", 
                $"Error eliminando almac�n: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SyncCatalogAsync(string? catalogName)
    {
        if (string.IsNullOrWhiteSpace(catalogName) || IsBusy) return;

        try
        {
            IsBusy = true;

            SyncResult result = catalogName.ToLower() switch
            {
                "almacenes" => await _catalogService.SyncCatalogFromApiAsync<Almacen>("almacenes"),
                "articulos" => await _catalogService.SyncCatalogFromApiAsync<Articulo>("articulos"),
                _ => new SyncResult { Success = false, Message = "Cat�logo no reconocido" }
            };

            if (result.Success)
            {
                await Shell.Current.DisplayAlert("Sincronizaci�n", 
                    result.Message, "OK");
                
                // Refresh the data
                await LoadDataAsync();
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", 
                    result.Message, "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", 
                $"Error sincronizando {catalogName}: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion

    #region Specific Repository Methods Examples

    /// <summary>
    /// Example of using specific repository methods
    /// </summary>
    public async Task LoadAlmacenesByCampoAsync(int idCampo)
    {
        try
        {
            var almacenes = await _almacenRepository.GetByCampoAsync(idCampo);
            // Process the filtered almacenes...
        }
        catch (Exception ex)
        {
            // Handle error...
        }
    }

    /// <summary>
    /// Example of using specific repository search
    /// </summary>
    public async Task SearchArticulosByNameAsync(string searchTerm)
    {
        try
        {
            var articulos = await _articuloRepository.SearchByNameAsync(searchTerm);
            // Process the search results...
        }
        catch (Exception ex)
        {
            // Handle error...
        }
    }

    /// <summary>
    /// Example of using repository for existence check
    /// </summary>
    public async Task<bool> CheckAlmacenExistsAsync(string nombre)
    {
        try
        {
            return await _almacenRepository.ExistsAsync(a => a.Nombre == nombre);
        }
        catch (Exception ex)
        {
            // Handle error...
            return false;
        }
    }

    #endregion
}