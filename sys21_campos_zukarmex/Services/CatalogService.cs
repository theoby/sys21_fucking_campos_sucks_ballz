using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Models.DTOs.Api;
using sys21_campos_zukarmex.Models.DTOs.Search;
using sys21_campos_zukarmex.Models.DTOs.Sync;
using sys21_campos_zukarmex.Models.DTOs.Validation;
using sys21_campos_zukarmex.Services.Repositories;

namespace sys21_campos_zukarmex.Services;

public interface ICatalogService
{
    // Generic CRUD operations
    Task<List<T>> GetAllAsync<T>() where T : class, new();
    Task<T?> GetByIdAsync<T>(int id) where T : class, new();
    Task<ValidationResult> ValidateAsync<T>(T entity) where T : class;
    Task<ApiResponse<T>> SaveAsync<T>(T entity) where T : class;
    Task<ApiResponse<bool>> DeleteAsync<T>(int id) where T : class, new();
    Task<SearchResponse<T>> SearchAsync<T>(SearchRequest request) where T : class, new();

    // Specific catalog operations
    Task<List<Almacen>> GetAlmacenesByCampoAsync(int idCampo);
    Task<List<Articulo>> GetArticulosByFamiliaAsync(int idFamilia);
    Task<List<Campo>> GetCamposByEmpresaAsync(int idEmpresa);
    Task<List<SubFamilia>> GetSubFamiliasByFamiliaAsync(int idFamilia);

    // Sync operations
    Task<SyncResult> SyncCatalogFromApiAsync<T>(string catalogName) where T : class, new();
    Task<SyncStatistics> GetSyncStatisticsAsync();
    
    // Advanced sync operations
    Task<List<SyncResult>> ForceFullResyncAsync(IProgress<SyncStatus>? progress = null);
    Task<SyncIntegrityReport> VerifySyncIntegrityAsync();
}

public class CatalogService : ICatalogService
{
    private readonly ApiService _apiService;
    private readonly DatabaseService _databaseService;
    private readonly SyncService _syncService;

    // Repositories
    private readonly IAlmacenRepository _almacenRepository;
    private readonly IArticuloRepository _articuloRepository;
    private readonly ICampoRepository _campoRepository;
    private readonly IEmpresaRepository _empresaRepository;
    private readonly IFamiliaRepository _familiaRepository;
    private readonly IInspectorRepository _inspectorRepository;
    private readonly ILoteRepository _loteRepository;
    private readonly IMaquinariaRepository _maquinariaRepository;
    private readonly ISubFamiliaRepository _subFamiliaRepository;

    public CatalogService(
        ApiService apiService,
        DatabaseService databaseService,
        SyncService syncService,
        IAlmacenRepository almacenRepository,
        IArticuloRepository articuloRepository,
        ICampoRepository campoRepository,
        IEmpresaRepository empresaRepository,
        IFamiliaRepository familiaRepository,
        IInspectorRepository inspectorRepository,
        ILoteRepository loteRepository,
        IMaquinariaRepository maquinariaRepository,
        ISubFamiliaRepository subFamiliaRepository)
    {
        _apiService = apiService;
        _databaseService = databaseService;
        _syncService = syncService;
        _almacenRepository = almacenRepository;
        _articuloRepository = articuloRepository;
        _campoRepository = campoRepository;
        _empresaRepository = empresaRepository;
        _familiaRepository = familiaRepository;
        _inspectorRepository = inspectorRepository;
        _loteRepository = loteRepository;
        _maquinariaRepository = maquinariaRepository;
        _subFamiliaRepository = subFamiliaRepository;
    }

    #region Generic CRUD Operations

    public async Task<List<T>> GetAllAsync<T>() where T : class, new()
    {
        return await _databaseService.GetAllAsync<T>();
    }

    public async Task<T?> GetByIdAsync<T>(int id) where T : class, new()
    {
        return await _databaseService.GetByIdAsync<T>(id);
    }

    public Task<ValidationResult> ValidateAsync<T>(T entity) where T : class
    {
        var result = new ValidationResult { IsValid = true };

        try
        {
            // Basic validation based on entity type
            switch (entity)
            {
                case Almacen almacen:
                    if (string.IsNullOrWhiteSpace(almacen.Nombre))
                        result.Errors.Add("El nombre del almacén es requerido");
                    if (almacen.IdCampo <= 0)
                        result.Errors.Add("El campo es requerido");
                    break;

                case Articulo articulo:
                    if (string.IsNullOrWhiteSpace(articulo.Nombre))
                        result.Errors.Add("El nombre del artículo es requerido");
                    if (string.IsNullOrWhiteSpace(articulo.Unidad))
                        result.Errors.Add("La unidad es requerida");
                    if (articulo.IdFamilia <= 0)
                        result.Errors.Add("La familia es requerida");
                    break;

                case Campo campo:
                    if (string.IsNullOrWhiteSpace(campo.Nombre))
                        result.Errors.Add("El nombre del campo es requerido");
                    if (campo.IdEmpresa <= 0)
                        result.Errors.Add("La empresa es requerida");
                    if (campo.IdInspector <= 0)
                        result.Errors.Add("El inspector es requerido");
                    break;

                case Lote lote:
                    if (string.IsNullOrWhiteSpace(lote.Nombre))
                        result.Errors.Add("El nombre del lote es requerido");
                    if (lote.IdCampo <= 0)
                        result.Errors.Add("El campo es requerido");
                    if (lote.Hectareas <= 0)
                        result.Errors.Add("Las hectáreas deben ser mayor a 0");
                    break;

                // Add more validation rules for other entities as needed
            }

            result.IsValid = !result.Errors.Any();
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Error de validación: {ex.Message}");
        }

        return Task.FromResult(result);
    }

    public async Task<ApiResponse<T>> SaveAsync<T>(T entity) where T : class
    {
        try
        {
            // Validate first
            var validation = await ValidateAsync(entity);
            if (!validation.IsValid)
            {
                return new ApiResponse<T>
                {
                    Success = false,
                    Message = string.Join(", ", validation.Errors),
                    Data = entity
                };
            }

            // Save locally first
            var localResult = await _databaseService.SaveAsync(entity);
            if (localResult > 0)
            {
                // Try to sync to API if connected
                try
                {
                    var apiResult = await SaveToApiAsync(entity);
                    if (apiResult.Success)
                    {
                        return apiResult;
                    }
                    // If API fails, still return success for local save
                    return new ApiResponse<T>
                    {
                        Success = true,
                        Message = "Guardado localmente. Se sincronizará cuando esté disponible la conexión.",
                        Data = entity
                    };
                }
                catch
                {
                    // API not available, return local success
                    return new ApiResponse<T>
                    {
                        Success = true,
                        Message = "Guardado localmente. Se sincronizará cuando esté disponible la conexión.",
                        Data = entity
                    };
                }
            }

            return new ApiResponse<T>
            {
                Success = false,
                Message = "Error al guardar localmente",
                Data = entity
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = ex.Message,
                Data = entity
            };
        }
    }

    public async Task<ApiResponse<bool>> DeleteAsync<T>(int id) where T : class, new()
    {
        try
        {
            // Delete locally first
            var localResult = await _databaseService.DeleteByIdAsync<T>(id);
            if (localResult > 0)
            {
                // Try to delete from API if connected
                try
                {
                    var apiResult = await DeleteFromApiAsync<T>(id);
                    if (apiResult.Success)
                    {
                        return apiResult;
                    }
                    // If API fails, still return success for local delete
                    return new ApiResponse<bool>
                    {
                        Success = true,
                        Message = "Eliminado localmente. Se sincronizará cuando esté disponible la conexión.",
                        Data = true
                    };
                }
                catch
                {
                    // API not available, return local success
                    return new ApiResponse<bool>
                    {
                        Success = true,
                        Message = "Eliminado localmente. Se sincronizará cuando esté disponible la conexión.",
                        Data = true
                    };
                }
            }

            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Error al eliminar localmente",
                Data = false
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = ex.Message,
                Data = false
            };
        }
    }

    public async Task<SearchResponse<T>> SearchAsync<T>(SearchRequest request) where T : class, new()
    {
        try
        {
            var allItems = await GetAllAsync<T>();
            var filteredItems = allItems;

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                filteredItems = FilterBySearchTerm(allItems, request.SearchTerm);
            }

            // Apply sorting if provided
            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                filteredItems = SortItems(filteredItems, request.SortBy, request.SortDescending);
            }

            // Apply pagination
            var totalCount = filteredItems.Count;
            var pagedItems = filteredItems
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new SearchResponse<T>
            {
                Data = pagedItems,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
        catch (Exception ex)
        {
            return new SearchResponse<T>
            {
                Data = new List<T>(),
                TotalCount = 0,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }

    #endregion

    #region Specific Catalog Operations

    public async Task<List<Almacen>> GetAlmacenesByCampoAsync(int idCampo)
    {
        return await _almacenRepository.GetByCampoAsync(idCampo);
    }

    public async Task<List<Articulo>> GetArticulosByFamiliaAsync(int idFamilia)
    {
        return await _articuloRepository.GetByFamiliaAsync(idFamilia);
    }

    public async Task<List<Campo>> GetCamposByEmpresaAsync(int idEmpresa)
    {
        return await _campoRepository.GetByEmpresaAsync(idEmpresa);
    }

    public async Task<List<SubFamilia>> GetSubFamiliasByFamiliaAsync(int idFamilia)
    {
        return await _subFamiliaRepository.GetByFamiliaAsync(idFamilia);
    }

    public async Task<List<Lote>> GetLotesByCampoAsync(int idCampo)
    {
        return await _loteRepository.GetByCampoAsync(idCampo);
    }

    #endregion

    #region Sync Operations

    public async Task<SyncResult> SyncCatalogFromApiAsync<T>(string catalogName) where T : class, new()
    {
        return await _syncService.SyncCatalogAsync<T>(catalogName);
    }

    public async Task<SyncStatistics> GetSyncStatisticsAsync()
    {
        return await _syncService.GetSyncStatisticsAsync();
    }

    /// <summary>
    /// Sincronización completa forzada con borrado e inserción completa de todas las tablas
    /// </summary>
    public async Task<List<SyncResult>> ForceFullResyncAsync(IProgress<SyncStatus>? progress = null)
    {
        return await _syncService.ForceFullResyncAsync(progress);
    }

    /// <summary>
    /// Verificar integridad de todas las tablas después de sincronización
    /// </summary>
    public async Task<SyncIntegrityReport> VerifySyncIntegrityAsync()
    {
        return await _syncService.VerifySyncIntegrityAsync();
    }

    #endregion

    #region Private Helper Methods

    private async Task<ApiResponse<T>> SaveToApiAsync<T>(T entity) where T : class
    {
        return entity switch
        {
            Almacen almacen => await _apiService.CreateAlmacenAsync(almacen) as ApiResponse<T> ?? new ApiResponse<T>(),
            Articulo articulo => await _apiService.CreateArticuloAsync(articulo) as ApiResponse<T> ?? new ApiResponse<T>(),
            Campo campo => await _apiService.CreateCampoAsync(campo) as ApiResponse<T> ?? new ApiResponse<T>(),
            Empresa empresa => await _apiService.CreateEmpresaAsync(empresa) as ApiResponse<T> ?? new ApiResponse<T>(),
            Familia familia => await _apiService.CreateFamiliaAsync(familia) as ApiResponse<T> ?? new ApiResponse<T>(),
            Inspector inspector => await _apiService.CreateInspectorAsync(inspector) as ApiResponse<T> ?? new ApiResponse<T>(),
            Lote lote => await _apiService.CreateLoteAsync(lote) as ApiResponse<T> ?? new ApiResponse<T>(),
            Maquinaria maquinaria => await _apiService.CreateMaquinariaAsync(maquinaria) as ApiResponse<T> ?? new ApiResponse<T>(),
            SubFamilia subFamilia => await _apiService.CreateSubFamiliaAsync(subFamilia) as ApiResponse<T> ?? new ApiResponse<T>(),
            _ => new ApiResponse<T> { Success = false, Message = "Tipo de entidad no soportado" }
        };
    }

    private async Task<ApiResponse<bool>> DeleteFromApiAsync<T>(int id) where T : class, new()
    {
        return typeof(T).Name switch
        {
            nameof(Almacen) => await _apiService.DeleteAlmacenAsync(id),
            nameof(Articulo) => await _apiService.DeleteArticuloAsync(id),
            nameof(Campo) => await _apiService.DeleteCampoAsync(id),
            nameof(Empresa) => await _apiService.DeleteEmpresaAsync(id),
            nameof(Familia) => await _apiService.DeleteFamiliaAsync(id),
            nameof(Inspector) => await _apiService.DeleteInspectorAsync(id),
            nameof(Lote) => await _apiService.DeleteLoteAsync(id),
            nameof(Maquinaria) => await _apiService.DeleteMaquinariaAsync(id),
            nameof(SubFamilia) => await _apiService.DeleteSubFamiliaAsync(id),
            _ => new ApiResponse<bool> { Success = false, Message = "Tipo de entidad no soportado" }
        };
    }

    private List<T> FilterBySearchTerm<T>(List<T> items, string searchTerm)
    {
        return items.Where(item =>
        {
            var nameProperty = typeof(T).GetProperty("Nombre");
            if (nameProperty != null)
            {
                var nameValue = nameProperty.GetValue(item)?.ToString();
                return nameValue?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true;
            }
            return false;
        }).ToList();
    }

    private List<T> SortItems<T>(List<T> items, string sortBy, bool descending)
    {
        var property = typeof(T).GetProperty(sortBy);
        if (property == null) return items;

        return descending
            ? items.OrderByDescending(item => property.GetValue(item)).ToList()
            : items.OrderBy(item => property.GetValue(item)).ToList();
    }

    #endregion
}