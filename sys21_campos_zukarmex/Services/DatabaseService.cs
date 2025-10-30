using SQLite;
using sys21_campos_zukarmex.Models;
using System.Reflection;
using System.Linq.Expressions;

namespace sys21_campos_zukarmex.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection? _database;

    public async Task InitializeAsync()
    {
        if (_database is not null)
            return;

        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "sys21_campos_zukarmex.db3");
        _database = new SQLiteAsyncConnection(databasePath);

        await CreateTablesAsync();
    }

    private async Task CreateTablesAsync()
    {
        if (_database is null) return;

        await _database.CreateTableAsync<Almacen>();
        await _database.CreateTableAsync<Articulo>();
        await _database.CreateTableAsync<Campo>();
        await _database.CreateTableAsync<Configuracion>();
        await _database.CreateTableAsync<Empresa>();
        await _database.CreateTableAsync<Familia>();
        await _database.CreateTableAsync<Inspector>();
        await _database.CreateTableAsync<Lote>();
        await _database.CreateTableAsync<Maquinaria>();
        await _database.CreateTableAsync<Pluviometro>();
        await _database.CreateTableAsync<Receta>();
        await _database.CreateTableAsync<RecetaArticulo>();
        await _database.CreateTableAsync<Salida>();
        await _database.CreateTableAsync<SalidaDetalle>();
        await _database.CreateTableAsync<SubFamilia>();
        await _database.CreateTableAsync<User>();
        await _database.CreateTableAsync<Zafra>();
        await _database.CreateTableAsync<Session>();
        await _database.CreateTableAsync<Ciclo>();
        await _database.CreateTableAsync<SalidaTrampeoRatas>();
        await _database.CreateTableAsync<LineaDeRiego>();
        await _database.CreateTableAsync<SalidaLineaDeRiego>();
        await _database.CreateTableAsync<SalidaRodenticida>();
        await _database.CreateTableAsync<SalidaPrecipitacion>();
        await _database.CreateTableAsync<SalidaMuestroDaños>();
        await _database.CreateTableAsync<SalidaMaquinaria>();
    }

    #region Generic CRUD Operations

    public async Task<List<T>> GetAllAsync<T>() where T : new()
    {
        await InitializeAsync();
        return await _database!.Table<T>().ToListAsync();
    }


    public async Task<T?> GetByIdAsync<T>(int id) where T : class, new()
    {
        await InitializeAsync();

        // Use reflection to get the Id property
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty == null)
            return null;

        var table = _database!.Table<T>();
        var items = await table.ToListAsync();

        return items.FirstOrDefault(item =>
        {
            var itemId = idProperty.GetValue(item);

            return Convert.ToInt32(itemId).Equals(id);
        });
    }

    //Eliminar contenido y avances de id
    public async Task ResetTableAsync<T>() where T : new()
    {
        await InitializeAsync();

        var tableName = typeof(T).Name;

        // 1. Borra todos los registros de la tabla
        await _database.DeleteAllAsync<T>();

        try
        {
            await _database.ExecuteAsync($"DELETE FROM sqlite_sequence WHERE name = '{tableName}'");
            System.Diagnostics.Debug.WriteLine($"Secuencia de la tabla '{tableName}' reiniciada.");
        }
        catch (SQLite.SQLiteException)
        {
            System.Diagnostics.Debug.WriteLine($"La tabla '{tableName}' no tiene una secuencia de autoincremento para reiniciar, o ya está reiniciada.");
        }
    }


    public async Task<List<T>> GetWhereAsync<T>(Expression<Func<T, bool>> predicate) where T : new()
    {
        await InitializeAsync();
        return await _database!.Table<T>().Where(predicate).ToListAsync();
    }

    public async Task<T?> GetFirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate) where T : class, new()
    {
        await InitializeAsync();
        return await _database!.Table<T>().Where(predicate).FirstOrDefaultAsync();
    }

    public async Task<int> CountAsync<T>() where T : new()
    {
        await InitializeAsync();
        return await _database!.Table<T>().CountAsync();
    }

    public async Task<int> CountWhereAsync<T>(Expression<Func<T, bool>> predicate) where T : new()
    {
        await InitializeAsync();
        return await _database!.Table<T>().Where(predicate).CountAsync();
    }

    public async Task<int> SaveAsync<T>(T item) where T : class
    {
        await InitializeAsync();
        
        // Use reflection to get the Id property
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty == null)
            return await _database!.InsertAsync(item);

        var id = idProperty.GetValue(item);
        if (id?.Equals(0) != false) // if id is 0 or null
        {
            return await _database!.InsertAsync(item);
        }
        else
        {
            return await _database!.UpdateAsync(item);
        }
    }

    public async Task<int> SaveAllAsync<T>(List<T> items) where T : class
    {
        if (items == null || !items.Any())
            return 0;

        await InitializeAsync();
        
        // Para operaciones masivas de inserci�n (como sync), usar InsertAllAsync es m�s eficiente
        // y devuelve el n�mero correcto de registros insertados
        try
        {
            // Primero intentar insertar todos como nuevos registros (m�s com�n en sync)
            var insertedCount = await _database!.InsertAllAsync(items);
            System.Diagnostics.Debug.WriteLine($"InsertAllAsync completado: {insertedCount} registros insertados");
            return insertedCount;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en InsertAllAsync: {ex.Message}");
            
            // Si falla InsertAllAsync (por duplicados), usar el m�todo transaccional individual
            return await SaveAllWithTransactionAsync(items);
        }
    }

    private async Task<int> SaveAllWithTransactionAsync<T>(List<T> items) where T : class
    {
        var result = 0;
        
        await _database!.RunInTransactionAsync(tran =>
        {
            foreach (var item in items)
            {
                try
                {
                    var idProperty = typeof(T).GetProperty("Id");
                    if (idProperty != null)
                    {
                        var id = idProperty.GetValue(item);
                        if (id?.Equals(0) != false) // if id is 0 or null
                        {
                            var insertResult = tran.Insert(item);
                            if (insertResult > 0) result++; // Count successful inserts
                        }
                        else
                        {
                            var updateResult = tran.Update(item);
                            if (updateResult > 0) result++; // Count successful updates
                        }
                    }
                    else
                    {
                        var insertResult = tran.Insert(item);
                        if (insertResult > 0) result++; // Count successful inserts
                    }
                }
                catch (Exception itemEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error procesando item individual: {itemEx.Message}");
                    // Continuar con el siguiente item
                }
            }
        });

        System.Diagnostics.Debug.WriteLine($"SaveAllWithTransactionAsync completado: {result} registros procesados de {items.Count}");
        return result;
    }

    public async Task<int> DeleteAsync<T>(T item)
    {
        await InitializeAsync();
        return await _database!.DeleteAsync(item);
    }

    public async Task DeleteListAsync<T>(List<T> items) where T : new()
    {
        await InitializeAsync();

        foreach (var item in items)
        {
            await _database!.DeleteAsync(item);
        }
    }
    public async Task<int> DeleteByIdAsync<T>(int id) where T : class, new()
    {
        await InitializeAsync();
        var item = await GetByIdAsync<T>(id);
        if (item != null)
        {
            return await _database!.DeleteAsync(item);
        }
        return 0;
    }

    public async Task<int> DeleteWhereAsync<T>(Expression<Func<T, bool>> predicate) where T : new()
    {
        await InitializeAsync();
        var items = await GetWhereAsync(predicate);
        var result = 0;
        foreach (var item in items)
        {
            result += await _database!.DeleteAsync(item);
        }
        return result;
    }

    public async Task<int> ClearTableAsync<T>() where T : new()
    {
        await InitializeAsync();
        return await _database!.DeleteAllAsync<T>();
    }

    public async Task<int> InsertAllAsync<T>(List<T> items)
    {
        if (items == null || !items.Any())
        {
            System.Diagnostics.Debug.WriteLine("InsertAllAsync: Lista vac�a o nula");
            return 0;
        }

        await InitializeAsync();
        
        try
        {
            System.Diagnostics.Debug.WriteLine($"InsertAllAsync: Intentando insertar {items.Count} registros de tipo {typeof(T).Name}");
            var result = await _database!.InsertAllAsync(items);
            System.Diagnostics.Debug.WriteLine($"InsertAllAsync exitoso: {result} registros insertados de {items.Count}");
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en InsertAllAsync para {typeof(T).Name}: {ex.Message}");
            
            // Log detalles adicionales para debugging
            if (items.Any())
            {
                var firstItem = items.First();
                var idProperty = typeof(T).GetProperty("Id");
                if (idProperty != null)
                {
                    var firstId = idProperty.GetValue(firstItem);
                    System.Diagnostics.Debug.WriteLine($"Primer item ID: {firstId}");
                }
            }
            
            throw; // Re-throw para que el caller pueda manejar el error
        }
    }

    public async Task<int> UpdateAllAsync<T>(List<T> items)
    {
        await InitializeAsync();
        return await _database!.UpdateAllAsync(items);
    }

    #endregion

    #region Specific Catalog Methods

    // Almacen
    public async Task<List<Almacen>> GetAlmacenesByCampoAsync(int idCampo)
    {
        return await GetWhereAsync<Almacen>(a => a.IdCampo == idCampo);
    }

    public async Task<Almacen?> GetAlmacenByNameAsync(string nombre)
    {
        return await GetFirstOrDefaultAsync<Almacen>(a => a.Nombre == nombre);
    }

    // Articulo
    public async Task<List<Articulo>> GetArticulosByFamiliaAsync(int idFamilia)
    {
        return await GetWhereAsync<Articulo>(a => a.IdFamilia == idFamilia);
    }

    public async Task<List<Articulo>> GetArticulosBySubFamiliaAsync(int idSubFamilia)
    {
        return await GetWhereAsync<Articulo>(a => a.IdSubFamilia == idSubFamilia);
    }

    public async Task<Articulo?> GetArticuloByNameAsync(string nombre)
    {
        return await GetFirstOrDefaultAsync<Articulo>(a => a.Nombre == nombre);
    }

    // Ciclo
    public async Task<Ciclo?> GetCicloByNameAsync(string nombre)
    {
        return await GetFirstOrDefaultAsync<Ciclo>(c => c.Nombre == nombre);
    }

    //Linea De Riego
    public async Task<LineaDeRiego?> GetLineaDeRiegoByNameAsync(string nombre)
    {
        return await GetFirstOrDefaultAsync<LineaDeRiego>(c => c.Nombre == nombre);
    }

    // Campo
    public async Task<List<Campo>> GetCamposByInspectorAsync(int idInspector)
    {
        return await GetWhereAsync<Campo>(c => c.IdInspector == idInspector);
    }

    public async Task<List<Campo>> GetCamposByEmpresaAsync(int idEmpresa)
    {
        return await GetWhereAsync<Campo>(c => c.IdEmpresa == idEmpresa);
    }

    public async Task<Campo?> GetCampoByNameAsync(string nombre)
    {
        return await GetFirstOrDefaultAsync<Campo>(c => c.Nombre == nombre);
    }

    // Familia
    public async Task<Familia?> GetFamiliaByNameAsync(string nombre)
    {
        return await GetFirstOrDefaultAsync<Familia>(f => f.Nombre == nombre);
    }

    // SubFamilia
    public async Task<List<SubFamilia>> GetSubFamiliasByFamiliaAsync(int idFamilia)
    {
        return await GetWhereAsync<SubFamilia>(sf => sf.IdFamilia == idFamilia);
    }

    public async Task<SubFamilia?> GetSubFamiliaByNameAsync(string nombre)
    {
        return await GetFirstOrDefaultAsync<SubFamilia>(sf => sf.Nombre == nombre);
    }


    // Zafra
    public async Task<Zafra?> GetZafraByNameAsync(string nombre)
    {
        return await GetFirstOrDefaultAsync<Zafra>(z => z.Nombre == nombre);
    }

    public async Task<Zafra?> GetActiveZafraAsync()
    {
        return await GetFirstOrDefaultAsync<Zafra>(z => z.Activa || z.IsActive);
    }

    // Pluviometro
    public async Task<Pluviometro?> GetPluviometroByNameAsync(string nombre)
    {
        return await GetFirstOrDefaultAsync<Pluviometro>(p => p.Nombre == nombre);
    }


    // User
    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await GetFirstOrDefaultAsync<User>(u => u.Username == username);
    }

    public async Task<List<User>> GetUsersByTipoAsync(int tipo)
    {
        return await GetWhereAsync<User>(u => u.Tipo == tipo);
    }

    // Salida (Vales)
    public async Task<List<Salida>> GetValesByUserAsync(string username)
    {
        return await GetWhereAsync<Salida>(s => s.Usuario == username);
    }

    public async Task<List<Salida>> GetValesByStatusAsync(string status)
    {
        return await GetWhereAsync<Salida>(s => s.StatusText == status);
    }

    public async Task<List<Salida>> GetValesByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        return await GetWhereAsync<Salida>(s => s.Fecha >= fromDate && s.Fecha <= toDate);
    }

    // Salida - limpiar datos cuando un usuario diferente inicia sesion
    public async Task ClearUserTransactionDataAsync()
    {
        // Usamos ResetTableAsync para que los IDs se reinicien
        await ResetTableAsync<SalidaDetalle>();
        await ResetTableAsync<Salida>();
        System.Diagnostics.Debug.WriteLine(">>> Tablas de Salida y SalidaDetalle han sido limpiadas y reiniciadas.");
    }

    public async Task<List<Salida>> GetValesPendingAuthorizationAsync()
    {
        return await GetWhereAsync<Salida>(s => s.Autorizado == null || s.Autorizado == false);
    }

    // SalidaDetalle
    public async Task<List<SalidaDetalle>> GetDetallesBySalidaAsync(int idSalida)
    {
        return await GetWhereAsync<SalidaDetalle>(sd => sd.IdSalida == idSalida);
    }

    public async Task<int> DeleteDetallesBySalidaAsync(int idSalida)
    {
        return await DeleteWhereAsync<SalidaDetalle>(sd => sd.IdSalida == idSalida);
    }

    // Session
    public async Task<Session?> GetActiveSessionAsync()
    {
        return await GetFirstOrDefaultAsync<Session>(s => s.IsActive);
    }

    public async Task<int> ClearActiveSessionsAsync()
    {
        var sessions = await GetWhereAsync<Session>(s => s.IsActive);
        var result = 0;
        foreach (var session in sessions)
        {
            session.IsActive = false;
            result += await _database!.UpdateAsync(session);
        }
        return result;
    }

    // Inspector
    public async Task<Inspector?> GetInspectorByNameAsync(string nombre)
    {
        return await GetFirstOrDefaultAsync<Inspector>(i => i.Nombre == nombre);
    }

    // Empresa
    public async Task<Empresa?> GetEmpresaByNameAsync(string nombre)
    {
        return await GetFirstOrDefaultAsync<Empresa>(e => e.Nombre == nombre);
    }

    // Maquinaria
    public async Task<Maquinaria?> GetMaquinariaByNameAsync(string nombre)
    {
        return await GetFirstOrDefaultAsync<Maquinaria>(m => m.Nombre == nombre);
    }

    // Lote
    public async Task<Lote?> GetLoteByNameAsync(string nombre)
    {
        return await GetFirstOrDefaultAsync<Lote>(l => l.Nombre == nombre);
    }

    public async Task<List<Lote>> GetLotesByCampoAsync(int idCampo)
    {
        return await GetWhereAsync<Lote>(l => l.IdCampo == idCampo);
    }

    // Receta
    public async Task<Receta?> GetRecetaByNameAsync(string nombreReceta)
    {
        return await GetFirstOrDefaultAsync<Receta>(r => r.NombreReceta == nombreReceta);
    }

    public async Task<List<Receta>> SearchRecetasAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<Receta>();
            
        return await GetWhereAsync<Receta>(r => 
            r.NombreReceta.Contains(searchTerm) || 
            r.IdReceta.ToString().Contains(searchTerm));
    }

    public async Task<List<Receta>> GetRecetasByAlmacenAsync(int almacenId)
    {
        return await GetWhereAsync<Receta>(r => r.Almacen == almacenId);
    }

    public async Task<List<Receta>> GetRecetasByTipoAsync(int tipoReceta)
    {
        return await GetWhereAsync<Receta>(r => r.TipoReceta == tipoReceta);
    }

    public async Task<Receta?> GetRecetaWithArticulosAsync(int idReceta)
    {
        // Buscar por IdReceta (no por Id interno)
        var receta = await GetFirstOrDefaultAsync<Receta>(r => r.IdReceta == idReceta);
        if (receta != null)
        {
            // Obtener artículos usando IdReceta
            var articulos = await GetRecetaArticulosByRecetaAsync(receta.IdReceta);
            
            // Enriquecer los artículos con nombres
            foreach (var articuloReceta in articulos)
            {
                var articulo = await GetByIdAsync<Articulo>(articuloReceta.IdArticulo);
                if (articulo != null)
                {
                    articuloReceta.ArticuloNombre = articulo.Nombre;
                    articuloReceta.Unidad = articulo.Unidad;
                }

                var familia = await GetByIdAsync<Familia>(articuloReceta.IdFamilia);
                if (familia != null)
                {
                    articuloReceta.FamiliaNombre = familia.Nombre;
                }

                var subfamilia = await GetByIdAsync<SubFamilia>(articuloReceta.IdSubFamilia);
                if (subfamilia != null)
                {
                    articuloReceta.SubFamiliaNombre = subfamilia.Nombre;
                }
            }
            
            receta.Articulos = articulos;
            
            // Enriquecer con nombres de campo y almacén
            var almacen = await GetByIdAsync<Almacen>(receta.IdAlmacen);
            if (almacen != null)
            {
                receta.AlmacenNombre = almacen.Nombre;
            }
            
            var campo = await GetByIdAsync<Campo>(receta.IdCampo);
            if (campo != null)
            {
                receta.CampoNombre = campo.Nombre;
            }
        }
        return receta;
    }

    // RecetaArticulo
    public async Task<List<RecetaArticulo>> GetRecetaArticulosByRecetaAsync(int idReceta)
    {
        return await GetWhereAsync<RecetaArticulo>(ra => ra.IdReceta == idReceta);
    }

    public async Task<List<RecetaArticulo>> GetRecetaArticulosByArticuloAsync(int idArticulo)
    {
        return await GetWhereAsync<RecetaArticulo>(ra => ra.IdArticulo == idArticulo);
    }

    public async Task<RecetaArticulo?> GetRecetaArticuloByRecetaAndArticuloAsync(int idReceta, int idArticulo)
    {
        return await GetFirstOrDefaultAsync<RecetaArticulo>(ra => ra.IdReceta == idReceta && ra.IdArticulo == idArticulo);
    }

    public async Task<int> DeleteRecetaArticulosByRecetaAsync(int idReceta)
    {
        return await DeleteWhereAsync<RecetaArticulo>(ra => ra.IdReceta == idReceta);
    }

    /// <summary>
    /// Obtiene una receta por su IdReceta (no por Id interno)
    /// </summary>
    public async Task<Receta?> GetRecetaByIdRecetaAsync(int idReceta)
    {
        return await GetFirstOrDefaultAsync<Receta>(r => r.IdReceta == idReceta);
    }

    /// <summary>
    /// Obtiene recetas por campo
    /// </summary>
    public async Task<List<Receta>> GetRecetasByCampoAsync(int idCampo)
    {
        return await GetWhereAsync<Receta>(r => r.IdCampo == idCampo);
    }

    /// <summary>
    /// Sincroniza una receta completa con sus artículos (para uso con API)
    /// </summary>
    public async Task<int> SyncRecetaWithArticulosAsync(Receta receta, List<RecetaArticulo> articulos)
    {
        await InitializeAsync();
        
        try
        {
            await _database!.RunInTransactionAsync(tran =>
            {
                // 1. Verificar si la receta ya existe por IdReceta
                var existingReceta = tran.Table<Receta>().FirstOrDefault(r => r.IdReceta == receta.IdReceta);
                
                if (existingReceta != null)
                {
                    // Actualizar receta existente
                    existingReceta.IdCampo = receta.IdCampo;
                    existingReceta.IdAlmacen = receta.IdAlmacen;
                    existingReceta.NombreReceta = receta.NombreReceta;
                    existingReceta.TipoReceta = receta.TipoReceta;
                    tran.Update(existingReceta);
                    
                    // Eliminar artículos existentes de esta receta
                    var existingArticulos = tran.Table<RecetaArticulo>().Where(ra => ra.IdReceta == receta.IdReceta).ToList();
                    foreach (var articulo in existingArticulos)
                    {
                        tran.Delete(articulo);
                    }
                }
                else
                {
                    // Insertar nueva receta
                    tran.Insert(receta);
                }
                
                // 2. Insertar todos los artículos de la receta
                foreach (var articulo in articulos)
                {
                    articulo.IdReceta = receta.IdReceta; // Asegurar que tienen el IdReceta correcto
                    tran.Insert(articulo);
                }
            });
            
            return articulos.Count;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en SyncRecetaWithArticulosAsync: {ex.Message}");
            throw;
        }
    }

    // Configuracion
    public async Task<Configuracion?> GetConfiguracionActivaAsync()
    {
        var configs = await GetAllAsync<Configuracion>();
        return configs.OrderByDescending(c => c.Fecha).FirstOrDefault();
    }

    public async Task<List<Configuracion>> GetAllConfiguracionesAsync()
    {
        return await GetAllAsync<Configuracion>();
    }

    #endregion

    #region Transaction Support

    public async Task ExecuteInTransactionAsync(Action<SQLiteConnection> action)
    {
        await InitializeAsync();
        await _database!.RunInTransactionAsync(action);
    }

    public Task<T> ExecuteInTransactionAsync<T>(Func<SQLiteConnection, T> func)
    {
        // This method signature doesn't exist in SQLite-net-pcl
        // Remove it for now since it's causing compilation errors
        throw new NotImplementedException("This method is not supported by SQLite-net-pcl");
    }

    #endregion

    #region Database Maintenance

    public Task<bool> DatabaseExistsAsync()
    {
        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "sys21_campos_zukarmex.db3");
        return Task.FromResult(File.Exists(databasePath));
    }

    public async Task DeleteDatabaseAsync()
    {
        if (_database != null)
        {
            await _database.CloseAsync();
            _database = null;
        }
        
        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "sys21_campos_zukarmex.db3");
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }

    public async Task ResetDatabaseAsync()
    {
        if (_database != null)
        {
            await _database.CloseAsync();
            _database = null;
        }

        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "sys21_campos_zukarmex.db3");
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }

        await InitializeAsync();
    }

    public Task<long> GetDatabaseSizeAsync()
    {
        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "sys21_campos_zukarmex.db3");
        if (File.Exists(databasePath))
        {
            var fileInfo = new FileInfo(databasePath);
            return Task.FromResult(fileInfo.Length);
        }
        return Task.FromResult(0L);
    }
   
    #endregion
}