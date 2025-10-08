using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Models.DTOs.Sync;
using sys21_campos_zukarmex.Services.Repositories;

namespace sys21_campos_zukarmex.Services;

public class SyncService
{
    private readonly ApiService _apiService;
    private readonly DatabaseService _databaseService;
    
    // Repositories
    private readonly IAlmacenRepository _almacenRepository;
    private readonly IArticuloRepository _articuloRepository;
    private readonly ICampoRepository _campoRepository;
    private readonly IEmpresaRepository _empresaRepository;
    private readonly IFamiliaRepository _familiaRepository;
    private readonly IInspectorRepository _inspectorRepository;
    private readonly ILoteRepository _loteRepository;
    private readonly IMaquinariaRepository _maquinariaRepository;
    private readonly IRecetaRepository _recetaRepository;
    private readonly ISubFamiliaRepository _subFamiliaRepository;
    private readonly IZafraRepository _zafraRepository;
    private readonly IPluviometroRepository _pluviometroRepository;
    private readonly ICicloRepository _cicloRepository;

    public SyncService(
        ApiService apiService, 
        DatabaseService databaseService,
        IAlmacenRepository almacenRepository,
        IArticuloRepository articuloRepository,
        ICampoRepository campoRepository,
        IEmpresaRepository empresaRepository,
        IFamiliaRepository familiaRepository,
        IInspectorRepository inspectorRepository,
        ICicloRepository cicloRepository,
        ILoteRepository loteRepository,
        IMaquinariaRepository maquinariaRepository,
        IRecetaRepository recetaRepository,
        IZafraRepository zafraRepository,
        ISubFamiliaRepository subFamiliaRepository,
        IPluviometroRepository pluviometroRepository)
    {
        _apiService = apiService;
        _databaseService = databaseService;
        _almacenRepository = almacenRepository;
        _articuloRepository = articuloRepository;
        _campoRepository = campoRepository;
        _empresaRepository = empresaRepository;
        _familiaRepository = familiaRepository;
        _inspectorRepository = inspectorRepository;
        _loteRepository = loteRepository;
        _zafraRepository = zafraRepository;
        _maquinariaRepository = maquinariaRepository;
        _cicloRepository = cicloRepository;
        _recetaRepository = recetaRepository;
        _subFamiliaRepository = subFamiliaRepository;
        _pluviometroRepository = pluviometroRepository;
    }

    public async Task<List<SyncStatus>> SyncAllCatalogsAsync(IProgress<SyncStatus>? progress = null)
    {
        var syncStatuses = new List<SyncStatus>();
        var totalCatalogs = 12; // Incrementado para incluir Recetas
        var currentCatalog = 0;

        // Sync Empresas first (needed for login)
        await SyncEmpresasAsync(progress, syncStatuses, ++currentCatalog, totalCatalogs);

        // Sync other catalogs
        await SyncCamposAsync(progress, syncStatuses, ++currentCatalog, totalCatalogs);
        await SyncInspectoresAsync(progress, syncStatuses, ++currentCatalog, totalCatalogs);
        await SyncAlmacenesAsync(progress, syncStatuses, ++currentCatalog, totalCatalogs);
        await SyncLotesAsync(progress, syncStatuses, ++currentCatalog, totalCatalogs);
        await SyncCiclosAsync(progress, syncStatuses, ++currentCatalog, totalCatalogs);
        await SyncFamiliasAsync(progress, syncStatuses, ++currentCatalog, totalCatalogs);
        await SyncZafrasAsync(progress, syncStatuses, ++currentCatalog, totalCatalogs);
        await SyncPluviometroAsync(progress, syncStatuses, ++currentCatalog, totalCatalogs);
        await SyncSubFamiliasAsync(progress, syncStatuses, ++currentCatalog, totalCatalogs);
        await SyncArticulosAsync(progress, syncStatuses, ++currentCatalog, totalCatalogs);
        await SyncMaquinariasAsync(progress, syncStatuses, ++currentCatalog, totalCatalogs);
        await SyncRecetasAsync(progress, syncStatuses, ++currentCatalog, totalCatalogs);
        
        return syncStatuses;
    }

    #region Individual Catalog Sync Methods

    private async Task SyncEmpresasAsync(IProgress<SyncStatus>? progress, List<SyncStatus> syncStatuses, int currentCatalog, int totalCatalogs)
    {
        await SyncCatalogAsync("Empresas", _apiService.GetEmpresasAsync, 
            _empresaRepository.ClearAllAsync, _empresaRepository.SaveAllAsync, 
            progress, syncStatuses, currentCatalog, totalCatalogs);
    }
    private async Task SyncCiclosAsync(IProgress<SyncStatus>? progress, List<SyncStatus> syncStatuses, int currentCatalog, int totalCatalogs)
    {
        await SyncCatalogAsync("Ciclos", _apiService.GetCiclosAsync,
            _cicloRepository.ClearAllAsync, _cicloRepository.SaveAllAsync,
            progress, syncStatuses, currentCatalog, totalCatalogs);
    }

    private async Task SyncZafrasAsync(IProgress<SyncStatus>? progress, List<SyncStatus> syncStatuses, int currentCatalog, int totalCatalogs)
    {
        await SyncCatalogAsync("Zafras", _apiService.GetZafrasAsync,
            _zafraRepository.ClearAllAsync, _zafraRepository.SaveAllAsync,
            progress, syncStatuses, currentCatalog, totalCatalogs);
    }
    private async Task SyncPluviometroAsync(IProgress<SyncStatus>? progress, List<SyncStatus> syncStatuses, int currentCatalog, int totalCatalogs)
    {
        await SyncCatalogAsync("Pluviometro", _apiService.GetPluviometrosAsync,
            _pluviometroRepository.ClearAllAsync, _pluviometroRepository.SaveAllAsync,
            progress, syncStatuses, currentCatalog, totalCatalogs);
    }


    private async Task SyncInspectoresAsync(IProgress<SyncStatus>? progress, List<SyncStatus> syncStatuses, int currentCatalog, int totalCatalogs)
    {
        await SyncCatalogAsync("Inspectores", _apiService.GetInspectoresAsync, 
            _inspectorRepository.ClearAllAsync, _inspectorRepository.SaveAllAsync, 
            progress, syncStatuses, currentCatalog, totalCatalogs);
    }

    private async Task SyncCamposAsync(IProgress<SyncStatus>? progress, List<SyncStatus> syncStatuses, int currentCatalog, int totalCatalogs)
    {
        await SyncCatalogAsync("Campos", _apiService.GetCamposAsync, 
            _campoRepository.ClearAllAsync, _campoRepository.SaveAllAsync, 
            progress, syncStatuses, currentCatalog, totalCatalogs);
    }

    private async Task SyncAlmacenesAsync(IProgress<SyncStatus>? progress, List<SyncStatus> syncStatuses, int currentCatalog, int totalCatalogs)
    {
        await SyncCatalogAsync("Almacenes", _apiService.GetAlmacenesAsync, 
            _almacenRepository.ClearAllAsync, _almacenRepository.SaveAllAsync, 
            progress, syncStatuses, currentCatalog, totalCatalogs);
    }

    private async Task SyncLotesAsync(IProgress<SyncStatus>? progress, List<SyncStatus> syncStatuses, int currentCatalog, int totalCatalogs)
    {
        await SyncCatalogAsync("Lotes", _apiService.GetLotesAsync, 
            _loteRepository.ClearAllAsync, _loteRepository.SaveAllAsync, 
            progress, syncStatuses, currentCatalog, totalCatalogs);
    }

    private async Task SyncFamiliasAsync(IProgress<SyncStatus>? progress, List<SyncStatus> syncStatuses, int currentCatalog, int totalCatalogs)
    {
        await SyncCatalogAsync("Familias", _apiService.GetFamiliasAsync, 
            _familiaRepository.ClearAllAsync, _familiaRepository.SaveAllAsync, 
            progress, syncStatuses, currentCatalog, totalCatalogs);
    }

    private async Task SyncSubFamiliasAsync(IProgress<SyncStatus>? progress, List<SyncStatus> syncStatuses, int currentCatalog, int totalCatalogs)
    {
        await SyncCatalogAsync("SubFamilias", _apiService.GetSubFamiliasAsync, 
            _subFamiliaRepository.ClearAllAsync, _subFamiliaRepository.SaveAllAsync, 
            progress, syncStatuses, currentCatalog, totalCatalogs);
    }

    private async Task SyncArticulosAsync(IProgress<SyncStatus>? progress, List<SyncStatus> syncStatuses, int currentCatalog, int totalCatalogs)
    {
        await SyncCatalogAsync("Articulos", _apiService.GetArticulosAsync, 
            _articuloRepository.ClearAllAsync, _articuloRepository.SaveAllAsync, 
            progress, syncStatuses, currentCatalog, totalCatalogs);
    }

    private async Task SyncMaquinariasAsync(IProgress<SyncStatus>? progress, List<SyncStatus> syncStatuses, int currentCatalog, int totalCatalogs)
    {
        await SyncCatalogAsync("Maquinarias", _apiService.GetMaquinariasAsync, 
            _maquinariaRepository.ClearAllAsync, _maquinariaRepository.SaveAllAsync, 
            progress, syncStatuses, currentCatalog, totalCatalogs);
    }

    private async Task SyncRecetasAsync(IProgress<SyncStatus>? progress, List<SyncStatus> syncStatuses, int currentCatalog, int totalCatalogs)
    {
        var syncStatus = new SyncStatus
        {
            CatalogName = "Recetas",
            Progress = (currentCatalog * 100) / totalCatalogs,
            Status = "Sincronizando..."
        };

        progress?.Report(syncStatus);
        syncStatuses.Add(syncStatus);

        try
        {
            System.Diagnostics.Debug.WriteLine($"[SyncRecetasAsync] === INICIANDO SINCRONIZACI�N ESPECIALIZADA DE RECETAS ===");
            System.Diagnostics.Debug.WriteLine($"[SyncRecetasAsync] Progreso: {currentCatalog}/{totalCatalogs} ({syncStatus.Progress}%)");
            
            // 1. LIMPIAR TABLA COMPLETA de recetas Y art�culos PRIMERO
            syncStatus.Status = "Limpiando tablas de recetas y art�culos...";
            progress?.Report(syncStatus);
            
            System.Diagnostics.Debug.WriteLine($"[SyncRecetasAsync] Limpiando tablas de RecetaArticulo y Receta ANTES de obtener datos...");
            var deletedRecetaArticulos = await _databaseService.ClearTableAsync<RecetaArticulo>();
            var deletedRecetas = await _databaseService.ClearTableAsync<Receta>();
            System.Diagnostics.Debug.WriteLine($"[SyncRecetasAsync] DELETE - {deletedRecetas} recetas eliminadas, {deletedRecetaArticulos} art�culos eliminados");

            // 2. Obtener datos de la API usando m�todo especializado
            syncStatus.Status = "Obteniendo recetas con art�culos desde API...";
            progress?.Report(syncStatus);
            
            System.Diagnostics.Debug.WriteLine($"[SyncRecetasAsync] Llamando a ApiService.GetRecetasAsync()...");
            var recetas = await _apiService.GetRecetasAsync();
            System.Diagnostics.Debug.WriteLine($"[SyncRecetasAsync] ApiService devolvi� {recetas?.Count ?? 0} recetas");

            if (recetas == null || !recetas.Any())
            {
                System.Diagnostics.Debug.WriteLine($"[SyncRecetasAsync] ?? WARNING: No hay recetas para sincronizar");
                syncStatus.Status = "WARNING: No hay recetas para sincronizar";
                syncStatus.IsCompleted = true;
                syncStatus.Progress = (currentCatalog * 100) / totalCatalogs;
                progress?.Report(syncStatus);
                System.Diagnostics.Debug.WriteLine("WARNING Recetas: Sin datos para sincronizar");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[SyncRecetasAsync] ? Recetas obtenidas exitosamente: {recetas.Count} recetas");

            // 3. GetRecetasAsync ya maneja la sincronizaci�n completa con SyncRecetaWithArticulosAsync
            System.Diagnostics.Debug.WriteLine($"[SyncRecetasAsync] Verificando conteo final despu�s de sincronizaci�n...");
            
            // Peque�a pausa para asegurar que la BD est� actualizada
            await Task.Delay(100);
            
            var finalRecetasCount = await _databaseService.CountAsync<Receta>();
            var finalArticulosCount = await _databaseService.CountAsync<RecetaArticulo>();
            
            System.Diagnostics.Debug.WriteLine($"[SyncRecetasAsync] VERIFY - {finalRecetasCount} recetas finales, {finalArticulosCount} art�culos finales en BD");

            // 4. Verificar sincronizaci�n exitosa
            if (finalRecetasCount > 0)
            {
                syncStatus.IsCompleted = true;
                syncStatus.Status = $"SUCCESS: {finalRecetasCount} recetas y {finalArticulosCount} art�culos sincronizados";
                syncStatus.Progress = (currentCatalog * 100) / totalCatalogs;
                System.Diagnostics.Debug.WriteLine($"[SyncRecetasAsync] ? COMPLETE: Sincronizaci�n exitosa - {finalRecetasCount} recetas, {finalArticulosCount} art�culos");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[SyncRecetasAsync] ? WARNING: No se sincronizaron recetas (finalCount = 0)");
                System.Diagnostics.Debug.WriteLine($"[SyncRecetasAsync] Datos recibidos de API: {recetas.Count} recetas");
                System.Diagnostics.Debug.WriteLine($"[SyncRecetasAsync] Esto indica un problema en SyncRecetaWithArticulosAsync");
                
                syncStatus.Status = "WARNING: Recetas recibidas pero no guardadas en BD";
                syncStatus.IsCompleted = false;
                
                // Log de las primeras recetas para debug
                foreach (var receta in recetas.Take(3))
                {
                    System.Diagnostics.Debug.WriteLine($"[SyncRecetasAsync] Debug receta: IdReceta={receta.IdReceta}, Nombre='{receta.NombreReceta}', Art�culos={receta.ArticulosCount}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SyncRecetasAsync] ? ERROR cr�tico: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[SyncRecetasAsync] Stack trace: {ex.StackTrace}");
            
            syncStatus.Status = $"ERROR: {ex.Message}";
            syncStatus.IsCompleted = false;
            
            // Verificar estado actual
            try
            {
                var currentRecetasCount = await _databaseService.CountAsync<Receta>();
                var currentArticulosCount = await _databaseService.CountAsync<RecetaArticulo>();
                System.Diagnostics.Debug.WriteLine($"[SyncRecetasAsync] Estado actual despu�s del error: {currentRecetasCount} recetas, {currentArticulosCount} art�culos");
            }
            catch (Exception verifyEx)
            {
                System.Diagnostics.Debug.WriteLine($"[SyncRecetasAsync] ERROR verificando estado: {verifyEx.Message}");
            }
        }

        progress?.Report(syncStatus);
        System.Diagnostics.Debug.WriteLine($"[SyncRecetasAsync] === FIN SINCRONIZACI�N RECETAS ===");
    }

    /// <summary>
    /// Sincronizaci�n especializada de recetas con art�culos para uso individual
    /// </summary>
    private async Task<SyncResult> SyncRecetasSpecialAsync(string catalogName)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"=== Sincronizaci�n individual especializada de {catalogName} ===");
            
            // PASO 1: BORRAR TABLAS COMPLETAS PRIMERO
            System.Diagnostics.Debug.WriteLine($"DELETE Limpiando tablas de recetas y art�culos ANTES de obtener datos...");
            var deletedArticulos = await _databaseService.ClearTableAsync<RecetaArticulo>();
            var deletedRecetas = await _databaseService.ClearTableAsync<Receta>();
            System.Diagnostics.Debug.WriteLine($"DELETE {catalogName}: {deletedRecetas} recetas eliminadas, {deletedArticulos} art�culos eliminados");
            
            // PASO 2: Obtener recetas usando m�todo especializado que maneja art�culos
            var recetas = await _apiService.GetRecetasAsync();
            System.Diagnostics.Debug.WriteLine($"API devolvi� {recetas?.Count ?? 0} recetas con art�culos");

            if (recetas != null && recetas.Any())
            {
                // GetRecetasAsync ya maneja la inserci�n con SyncRecetaWithArticulosAsync
                // Verificar conteo final
                var finalRecetasCount = await _databaseService.CountAsync<Receta>();
                var finalArticulosCount = await _databaseService.CountAsync<RecetaArticulo>();
                System.Diagnostics.Debug.WriteLine($"COUNT {catalogName}: {finalRecetasCount} recetas finales, {finalArticulosCount} art�culos finales en BD");
                
                if (finalRecetasCount > 0)
                {
                    return new SyncResult 
                    { 
                        Success = true, 
                        Message = $"SUCCESS Sincronizaci�n completa: {finalRecetasCount} recetas y {finalArticulosCount} art�culos sincronizados",
                        RecordsCount = finalRecetasCount
                    };
                }
                else
                {
                    return new SyncResult 
                    { 
                        Success = false, 
                        Message = $"WARNING Error: Se obtuvieron {recetas.Count} recetas de la API pero no se sincronizaron",
                        RecordsCount = 0
                    };
                }
            }
            
            // No hay datos de la API
            System.Diagnostics.Debug.WriteLine($"WARNING {catalogName}: API no devolvi� datos");
            
            return new SyncResult 
            { 
                Success = true, 
                Message = $"WARNING API sin datos para {catalogName}. Tablas locales limpiadas",
                RecordsCount = 0
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR sincronizando {catalogName}: {ex}");
            
            try
            {
                var currentRecetasCount = await _databaseService.CountAsync<Receta>();
                var currentArticulosCount = await _databaseService.CountAsync<RecetaArticulo>();
                
                System.Diagnostics.Debug.WriteLine($"COUNT {catalogName}: {currentRecetasCount} recetas, {currentArticulosCount} art�culos actuales despu�s del error");
                
                return new SyncResult 
                { 
                    Success = false, 
                    Message = $"ERROR Error sincronizando {catalogName}: {ex.Message}. Estado actual: {currentRecetasCount} recetas, {currentArticulosCount} art�culos",
                    RecordsCount = currentRecetasCount
                };
            }
            catch
            {
                return new SyncResult 
                { 
                    Success = false, 
                    Message = $"ERROR Error sincronizando {catalogName}: {ex.Message}",
                    RecordsCount = 0
                };
            }
        }
    }

    #endregion

    #region Generic Sync Method

    private async Task SyncCatalogAsync<T>(
        string catalogName,
        Func<Task<List<T>>> apiCall,
        Func<Task<int>> clearTableFunc,
        Func<List<T>, Task<int>> insertAllFunc,
        IProgress<SyncStatus>? progress,
        List<SyncStatus> syncStatuses,
        int currentCatalog,
        int totalCatalogs) where T : class
    {
        var syncStatus = new SyncStatus
        {
            CatalogName = catalogName,
            Progress = (currentCatalog * 100) / totalCatalogs,
            Status = "Sincronizando..."
        };

        progress?.Report(syncStatus);
        syncStatuses.Add(syncStatus);

        try
        {
            System.Diagnostics.Debug.WriteLine($"=== Iniciando sincronizacion de {catalogName} ===");
            
            // 1. Obtener datos de la API
            syncStatus.Status = $"Obteniendo datos de {catalogName} desde API...";
            progress?.Report(syncStatus);
            
            var data = await apiCall();
            System.Diagnostics.Debug.WriteLine($"API devolvio {data?.Count ?? 0} registros para {catalogName}");

            if (data == null || !data.Any())
            {
                syncStatus.Status = $"No hay datos para sincronizar en {catalogName}";
                syncStatus.IsCompleted = true;
                syncStatus.Progress = (currentCatalog * 100) / totalCatalogs;
                progress?.Report(syncStatus);
                System.Diagnostics.Debug.WriteLine($"WARNING {catalogName}: Sin datos para sincronizar");
                return;
            }

            // 2. BORRAR TABLA COMPLETA
            syncStatus.Status = $"Limpiando tabla de {catalogName}...";
            progress?.Report(syncStatus);
            
            var deletedCount = await clearTableFunc();
            System.Diagnostics.Debug.WriteLine($"DELETE {catalogName}: {deletedCount} registros eliminados");

            // 3. INSERTAR TODOS LOS DATOS NUEVOS
            syncStatus.Status = $"Insertando {data.Count} registros en {catalogName}...";
            progress?.Report(syncStatus);
            
            System.Diagnostics.Debug.WriteLine($"BEFORE INSERT {catalogName}: Iniciando inserci�n de {data.Count} registros");
            
            // Log detalles del primer registro para debugging
            if (data.Any())
            {
                var firstItem = data.First();
                var itemType = firstItem.GetType();
                var idProperty = itemType.GetProperty("Id");
                if (idProperty != null)
                {
                    var firstId = idProperty.GetValue(firstItem);
                    System.Diagnostics.Debug.WriteLine($"SAMPLE {catalogName}: Primer registro - Tipo: {itemType.Name}, ID: {firstId}");
                }
            }
            
            var insertedCount = await insertAllFunc(data);
            System.Diagnostics.Debug.WriteLine($"SUCCESS {catalogName}: {insertedCount} registros insertados de {data.Count} enviados");

            // Verificar conteo final en la base de datos
            var finalCount = catalogName switch
            {
                "Empresas" => await _empresaRepository.CountAsync(),
                "Inspectores" => await _inspectorRepository.CountAsync(),
                "Campos" => await _campoRepository.CountAsync(),
                "Almacenes" => await _almacenRepository.CountAsync(),
                "Lotes" => await _loteRepository.CountAsync(),
                "Familias" => await _familiaRepository.CountAsync(),
                "SubFamilias" => await _subFamiliaRepository.CountAsync(),
                "Articulos" => await _articuloRepository.CountAsync(),
                "Zafras" => await _zafraRepository.CountAsync(),
                "Maquinarias" => await _maquinariaRepository.CountAsync(),
                "Ciclos" => await _cicloRepository.CountAsync(),
                "Recetas" => await _recetaRepository.CountAsync(),
                "Pluviometros" => await _pluviometroRepository.CountAsync(),
                _ => 0
            };
            
            System.Diagnostics.Debug.WriteLine($"VERIFY {catalogName}: Conteo final en BD: {finalCount}");

            // 4. Verificar insercion exitosa
            if (insertedCount > 0)
            {
                syncStatus.IsCompleted = true;
                syncStatus.Status = $"SUCCESS Completado: {insertedCount} registros sincronizados";
                syncStatus.Progress = (currentCatalog * 100) / totalCatalogs;
                System.Diagnostics.Debug.WriteLine($"COMPLETE {catalogName}: Sincronizacion exitosa");
            }
            else
            {
                syncStatus.Status = $"WARNING Advertencia: No se insertaron registros en {catalogName}";
                System.Diagnostics.Debug.WriteLine($"WARNING {catalogName}: Sin registros insertados");
            }
        }
        catch (Exception ex)
        {
            syncStatus.Status = $"ERROR Error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"ERROR sincronizando {catalogName}: {ex}");
            
            // Intentar rollback si es posible
            try
            {
                // Log adicional para debugging
                System.Diagnostics.Debug.WriteLine($"VERIFY Intentando verificar estado de {catalogName} despues del error");
                var currentCount = 0;
                try
                {
                    // Evitar problemas de constraint usando repositorios especificos
                    currentCount = catalogName.ToLower() switch
                    {
                        "almacenes" => await _almacenRepository.CountAsync(),
                        "articulos" => await _articuloRepository.CountAsync(),
                        "campos" => await _campoRepository.CountAsync(),
                        "empresas" => await _empresaRepository.CountAsync(),
                        "familias" => await _familiaRepository.CountAsync(),
                        "inspectores" => await _inspectorRepository.CountAsync(),
                        "lotes" => await _loteRepository.CountAsync(),
                        "maquinarias" => await _maquinariaRepository.CountAsync(),
                        "recetas" => await _recetaRepository.CountAsync(),
                        "zafras" => await _zafraRepository.CountAsync(),
                        "ciclos" => await _cicloRepository.CountAsync(),
                        "subfamilias" => await _subFamiliaRepository.CountAsync(),
                        "pluviometros" => await _pluviometroRepository.CountAsync(),
                        _ => 0
                    };
                }
                catch
                {
                    currentCount = 0;
                }
                System.Diagnostics.Debug.WriteLine($"COUNT {catalogName}: {currentCount} registros actuales en BD");
            }
            catch (Exception verifyEx)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR verificando estado de {catalogName}: {verifyEx.Message}");
            }
        }

        progress?.Report(syncStatus);
    }

    #endregion

    #region Individual Catalog Sync (On-Demand)

    public async Task<SyncResult> SyncCatalogAsync<T>(string catalogName) where T : class, new()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"=== Sincronizacion individual de {catalogName} ===");
            
            // Obtener datos de la API segun el tipo de catalogo
            List<T>? data = catalogName.ToLower() switch
            {
                "almacenes" => await _apiService.GetAlmacenesAsync() as List<T>,
                "articulos" => await _apiService.GetArticulosAsync() as List<T>,
                "campos" => await _apiService.GetCamposAsync() as List<T>,
                "empresas" => await _apiService.GetEmpresasAsync() as List<T>,
                "familias" => await _apiService.GetFamiliasAsync() as List<T>,
                "inspectores" => await _apiService.GetInspectoresAsync() as List<T>,
                "lotes" => await _apiService.GetLotesAsync() as List<T>,
                "maquinarias" => await _apiService.GetMaquinariasAsync() as List<T>,
                "recetas" => await _apiService.GetRecetasAsync() as List<T>,
                "subfamilias" => await _apiService.GetSubFamiliasAsync() as List<T>,
                "ciclos" => await _apiService.GetCiclosAsync() as List<T>,
                "zafras" => await _apiService.GetZafrasAsync() as List<T>,
                "pluviometros" => await _apiService.GetPluviometrosAsync() as List<T>,
                _ => new List<T>()
            };

            System.Diagnostics.Debug.WriteLine($"API devolvio {data?.Count ?? 0} registros para {catalogName}");

            if (data != null && data.Any())
            {
                // PASO 1: BORRAR TABLA COMPLETA
                System.Diagnostics.Debug.WriteLine($"DELETE Limpiando tabla {catalogName}...");
                var deletedCount = await _databaseService.ClearTableAsync<T>();
                System.Diagnostics.Debug.WriteLine($"DELETE {catalogName}: {deletedCount} registros eliminados");
                
                // PASO 2: INSERTAR TODOS LOS DATOS NUEVOS
                System.Diagnostics.Debug.WriteLine($"INSERT Insertando {data.Count} registros en {catalogName}...");
                var insertedCount = await _databaseService.InsertAllAsync(data);
                System.Diagnostics.Debug.WriteLine($"SUCCESS {catalogName}: {insertedCount} registros insertados");
                
                // PASO 3: VERIFICAR INSERCION
                var finalCount = await _databaseService.CountAsync<T>();
                System.Diagnostics.Debug.WriteLine($"COUNT {catalogName}: {finalCount} registros finales en BD");
                
                if (insertedCount > 0)
                {
                    return new SyncResult 
                    { 
                        Success = true, 
                        Message = $"SUCCESS Sincronizacion completa: {insertedCount} registros de {catalogName} (Borrado: {deletedCount}, Insertado: {insertedCount})",
                        RecordsCount = insertedCount
                    };
                }
                else
                {
                    return new SyncResult 
                    { 
                        Success = false, 
                        Message = $"WARNING Error: Se obtuvieron {data.Count} registros de la API pero no se insertaron en {catalogName}",
                        RecordsCount = 0
                    };
                }
            }
            
            // No hay datos de la API
            System.Diagnostics.Debug.WriteLine($"WARNING {catalogName}: API no devolvi� datos");
            
            // Aun asi, limpiar la tabla para mantener consistencia
            var deletedCountEmpty = await _databaseService.ClearTableAsync<T>();
            System.Diagnostics.Debug.WriteLine($"DELETE {catalogName}: Tabla limpiada ({deletedCountEmpty} registros eliminados) por falta de datos en API");
            
            return new SyncResult 
            { 
                Success = true, 
                Message = $"WARNING API sin datos para {catalogName}. Tabla local limpiada ({deletedCountEmpty} registros eliminados)",
                RecordsCount = 0
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR sincronizando {catalogName}: {ex}");
            
            // Verificar estado actual para debugging
            try
            {
                // Evitar problemas de constraint usando repositorios especificos
                var currentCount = catalogName.ToLower() switch
                {
                    "almacenes" => await _almacenRepository.CountAsync(),
                    "articulos" => await _articuloRepository.CountAsync(),
                    "campos" => await _campoRepository.CountAsync(),
                    "empresas" => await _empresaRepository.CountAsync(),
                    "familias" => await _familiaRepository.CountAsync(),
                    "inspectores" => await _inspectorRepository.CountAsync(),
                    "lotes" => await _loteRepository.CountAsync(),
                    "ciclos" => await _cicloRepository.CountAsync(),
                    "maquinarias" => await _maquinariaRepository.CountAsync(),
                    "recetas" => await _recetaRepository.CountAsync(),
                    "subfamilias" => await _subFamiliaRepository.CountAsync(),
                    "zafras" => await _zafraRepository.CountAsync(),
                    "pluviometros" => await _pluviometroRepository.CountAsync(),
                    _ => 0
                };
                
                System.Diagnostics.Debug.WriteLine($"COUNT {catalogName}: {currentCount} registros actuales despues del error");
                
                return new SyncResult 
                { 
                    Success = false, 
                    Message = $"ERROR Error sincronizando {catalogName}: {ex.Message}. Registros actuales: {currentCount}",
                    RecordsCount = currentCount
                };
            }
            catch
            {
                return new SyncResult 
                { 
                    Success = false, 
                    Message = $"ERROR Error sincronizando {catalogName}: {ex.Message}",
                    RecordsCount = 0
                };
            }
        }
    }

    #endregion

    #region Advanced Sync Operations

    /// <summary>
    /// Sincronizacion completa con verificacion y limpieza forzada de todas las tablas
    /// </summary>
    public async Task<List<SyncResult>> ForceFullResyncAsync(IProgress<SyncStatus>? progress = null)
    {
        var results = new List<SyncResult>();
        
        System.Diagnostics.Debug.WriteLine("FORCE Iniciando sincronizacion completa FORZADA");
        
        try
        {
            // Lista de todos los catalogos a sincronizar
            var catalogsToSync = new[]
            {
                ("empresas", "Empresas"),
                ("inspectores", "Inspectores"),
                ("campos", "Campos"),
                ("almacenes", "Almacenes"),
                ("lotes", "Lotes"),
                ("familias", "Familias"),
                ("subfamilias", "SubFamilias"),
                ("articulos", "Articulos"),
                ("maquinarias", "Maquinarias"),
                ("recetas", "Recetas"),
                ("zafras", "Zafras"),
                ("pluviometros", "Pluviometros"),
                ("ciclos", "Ciclos"),
            };

            var totalCatalogs = catalogsToSync.Length;
            var currentCatalog = 0;

            foreach (var (catalogKey, catalogDisplay) in catalogsToSync)
            {
                currentCatalog++;
                
                var syncStatus = new SyncStatus
                {
                    CatalogName = catalogDisplay,
                    Progress = (currentCatalog * 100) / totalCatalogs,
                    Status = $"Sincronizacion forzada {currentCatalog}/{totalCatalogs}..."
                };
                
                progress?.Report(syncStatus);
                
                try
                {
                    // Sincronizacion individual con el tipo correcto
                    SyncResult result = catalogKey switch
                    {
                        "empresas" => await SyncCatalogAsync<Empresa>(catalogKey),
                        "inspectores" => await SyncCatalogAsync<Inspector>(catalogKey),
                        "campos" => await SyncCatalogAsync<Campo>(catalogKey),
                        "almacenes" => await SyncCatalogAsync<Almacen>(catalogKey),
                        "lotes" => await SyncCatalogAsync<Lote>(catalogKey),
                        "familias" => await SyncCatalogAsync<Familia>(catalogKey),
                        "subfamilias" => await SyncCatalogAsync<SubFamilia>(catalogKey),
                        "articulos" => await SyncCatalogAsync<Articulo>(catalogKey),
                        "maquinarias" => await SyncCatalogAsync<Maquinaria>(catalogKey),
                        "recetas" => await SyncRecetasSpecialAsync(catalogKey),
                        "zafras" => await SyncCatalogAsync<Zafra>(catalogKey),
                        "ciclos" => await SyncCatalogAsync<Ciclo>(catalogKey),
                        _ => new SyncResult { Success = false, Message = $"Catalogo {catalogKey} no reconocido" }
                    };

                    results.Add(result);
                    
                    syncStatus.IsCompleted = result.Success;
                    syncStatus.Status = result.Success ? 
                        $"SUCCESS {catalogDisplay}: {result.RecordsCount} registros" : 
                        $"ERROR {catalogDisplay}: {result.Message}";
                    
                    progress?.Report(syncStatus);
                }
                catch (Exception ex)
                {
                    var errorResult = new SyncResult
                    {
                        Success = false,
                        Message = $"ERROR Error sincronizando {catalogDisplay}: {ex.Message}",
                        RecordsCount = 0
                    };
                    
                    results.Add(errorResult);
                    
                    syncStatus.Status = $"ERROR Error en {catalogDisplay}";
                    progress?.Report(syncStatus);
                }
            }
            
            // Reporte final
            var successCount = results.Count(r => r.Success);
            var totalRecords = results.Where(r => r.Success).Sum(r => r.RecordsCount);
            
            System.Diagnostics.Debug.WriteLine($"COMPLETE Sincronizacion completa terminada: {successCount}/{totalCatalogs} exitosos, {totalRecords} registros totales");
            
            return results;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR en sincronizacion completa forzada: {ex}");
            
            results.Add(new SyncResult
            {
                Success = false,
                Message = $"ERROR Error general en sincronizacion: {ex.Message}",
                RecordsCount = 0
            });
            
            return results;
        }
    }

    /// <summary>
    /// Verificar integridad de todas las tablas despues de sincronizacion
    /// </summary>
    public async Task<SyncIntegrityReport> VerifySyncIntegrityAsync()
    {
        var report = new SyncIntegrityReport
        {
            VerificationDate = DateTime.Now,
            CatalogCounts = new Dictionary<string, int>()
        };

        try
        {
            System.Diagnostics.Debug.WriteLine("VERIFY Verificando integridad de sincronizacion...");

            // Verificar cada tabla
            var catalogChecks = new Dictionary<string, Func<Task<int>>>
            {
                { "Empresas", () => _empresaRepository.CountAsync() },
                { "Inspectores", () => _inspectorRepository.CountAsync() },
                { "Campos", () => _campoRepository.CountAsync() },
                { "Almacenes", () => _almacenRepository.CountAsync() },
                { "Lotes", () => _loteRepository.CountAsync() },
                { "Familias", () => _familiaRepository.CountAsync() },
                { "SubFamilias", () => _subFamiliaRepository.CountAsync() },
                { "Articulos", () => _articuloRepository.CountAsync() },
                { "Maquinarias", () => _maquinariaRepository.CountAsync() },
                { "Recetas", () => _recetaRepository.CountAsync() },
                { "Zafras", () => _zafraRepository.CountAsync() },
                { "Pluviometros", () => _pluviometroRepository.CountAsync()},
                { "Ciclos", () => _cicloRepository.CountAsync()},
    
            };

            foreach (var (catalogName, countFunc) in catalogChecks)
            {
                try
                {
                    var count = await countFunc();
                    report.CatalogCounts[catalogName] = count;
                    
                    if (count > 0)
                    {
                        report.SuccessfulSyncs++;
                        System.Diagnostics.Debug.WriteLine($"SUCCESS {catalogName}: {count} registros");
                    }
                    else
                    {
                        report.EmptyTables++;
                        System.Diagnostics.Debug.WriteLine($"WARNING {catalogName}: 0 registros");
                    }
                }
                catch (Exception ex)
                {
                    report.ErroredTables++;
                    report.CatalogCounts[catalogName] = -1; // Indica error
                    System.Diagnostics.Debug.WriteLine($"ERROR verificando {catalogName}: {ex.Message}");
                }
            }

            report.TotalTables = catalogChecks.Count;
            report.TotalRecords = report.CatalogCounts.Where(kv => kv.Value > 0).Sum(kv => kv.Value);
            
            System.Diagnostics.Debug.WriteLine($"STATS Reporte de integridad: {report.SuccessfulSyncs}/{report.TotalTables} tablas con datos, {report.TotalRecords} registros totales");
            
            return report;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR verificando integridad: {ex}");
            report.CatalogCounts["ERROR"] = -1;
            return report;
        }
    }

    public async Task<int> GetPendingValesCountAsync()
    {
        var valesPendientesCount = await _apiService.GetValePendientesAsync();
        return valesPendientesCount.Count();
    }

    #endregion

    #region Upload Local Changes

    public async Task<List<SyncResult>> UploadPendingChangesAsync()
    {
        var results = new List<SyncResult>();

        // Upload pending vales (Salidas)
        var pendingSalidas = await _databaseService.GetValesByStatusAsync("Pending");
        foreach (var salida in pendingSalidas)
        {
            try
            {
                var result = await _apiService.SaveValeAsync(salida);
                if (result.Success)
                {
                    salida.StatusText = "Synced";
                    await _databaseService.SaveAsync(salida);
                    results.Add(new SyncResult 
                    { 
                        Success = true, 
                        Message = $"Vale {salida.Id} subido exitosamente" 
                    });
                }
                else
                {
                    results.Add(new SyncResult 
                    { 
                        Success = false, 
                        Message = $"Error subiendo vale {salida.Id}: {result.Message}" 
                    });
                }
            }
            catch (Exception ex)
            {
                results.Add(new SyncResult 
                { 
                    Success = false, 
                    Message = $"Excepcion subiendo vale {salida.Id}: {ex.Message}" 
                });
            }
        }

        return results;
    }

    #endregion

    #region Sync Status and Statistics

    public async Task<SyncStatistics> GetSyncStatisticsAsync()
    {
        var stats = new SyncStatistics();

        try
        {
            stats.AlmacenesCount = await _almacenRepository.CountAsync();
            stats.ArticulosCount = await _articuloRepository.CountAsync();
            stats.CamposCount = await _campoRepository.CountAsync();
            stats.EmpresasCount = await _empresaRepository.CountAsync();
            stats.FamiliasCount = await _familiaRepository.CountAsync();
            stats.InspectoresCount = await _inspectorRepository.CountAsync();
            stats.LotesCount = await _loteRepository.CountAsync();
            stats.MaquinariasCount = await _maquinariaRepository.CountAsync();
            stats.RecetasCount = await _recetaRepository.CountAsync();
            stats.SubFamiliasCount = await _subFamiliaRepository.CountAsync();
            stats.ZafrasCount = await _zafraRepository.CountAsync();
            stats.PluviometrosCount = await _pluviometroRepository.CountAsync();
            stats.CiclosCount = await _cicloRepository.CountAsync();

            stats.TotalRecords = stats.AlmacenesCount + stats.ArticulosCount + stats.CamposCount +
                               stats.EmpresasCount + stats.FamiliasCount +
                               stats.InspectoresCount + stats.LotesCount + stats.MaquinariasCount +
                               stats.RecetasCount + stats.SubFamiliasCount + stats.ZafrasCount + stats.PluviometrosCount + stats.CiclosCount; 

            stats.LastSyncDate = DateTime.Now;
        }
        catch (Exception ex)
        {
            stats.ErrorMessage = ex.Message;
        }

        return stats;
    }

    #endregion
}