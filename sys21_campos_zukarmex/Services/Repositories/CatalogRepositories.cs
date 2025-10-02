using sys21_campos_zukarmex.Models;

namespace sys21_campos_zukarmex.Services.Repositories;

// Repositories for simple catalogs with basic CRUD + name search

public interface IFamiliaRepository : IRepository<Familia>
{
    Task<Familia?> GetByNameAsync(string nombre);
}

public class FamiliaRepository : BaseRepository<Familia>, IFamiliaRepository
{
    public FamiliaRepository(DatabaseService databaseService) : base(databaseService) { }

    public async Task<Familia?> GetByNameAsync(string nombre)
    {
        return await _databaseService.GetFamiliaByNameAsync(nombre);
    }
}

public interface ISubFamiliaRepository : IRepository<SubFamilia>
{
    Task<List<SubFamilia>> GetByFamiliaAsync(int idFamilia);
    Task<SubFamilia?> GetByNameAsync(string nombre);
}

public class SubFamiliaRepository : BaseRepository<SubFamilia>, ISubFamiliaRepository
{
    public SubFamiliaRepository(DatabaseService databaseService) : base(databaseService) { }

    public async Task<List<SubFamilia>> GetByFamiliaAsync(int idFamilia)
    {
        return await _databaseService.GetSubFamiliasByFamiliaAsync(idFamilia);
    }

    public async Task<SubFamilia?> GetByNameAsync(string nombre)
    {
        return await _databaseService.GetSubFamiliaByNameAsync(nombre);
    }
}

public interface IInspectorRepository : IRepository<Inspector>
{
    Task<Inspector?> GetByNameAsync(string nombre);
}

public class InspectorRepository : BaseRepository<Inspector>, IInspectorRepository
{
    public InspectorRepository(DatabaseService databaseService) : base(databaseService) { }

    public async Task<Inspector?> GetByNameAsync(string nombre)
    {
        return await _databaseService.GetInspectorByNameAsync(nombre);
    }
}

public interface IEmpresaRepository : IRepository<Empresa>
{
    Task<Empresa?> GetByNameAsync(string nombre);
}

public class EmpresaRepository : BaseRepository<Empresa>, IEmpresaRepository
{
    public EmpresaRepository(DatabaseService databaseService) : base(databaseService) { }

    public async Task<Empresa?> GetByNameAsync(string nombre)
    {
        return await _databaseService.GetEmpresaByNameAsync(nombre);
    }
}

public interface IMaquinariaRepository : IRepository<Maquinaria>
{
    Task<Maquinaria?> GetByNameAsync(string nombre);
}

public class MaquinariaRepository : BaseRepository<Maquinaria>, IMaquinariaRepository
{
    public MaquinariaRepository(DatabaseService databaseService) : base(databaseService) { }

    public async Task<Maquinaria?> GetByNameAsync(string nombre)
    {
        return await _databaseService.GetMaquinariaByNameAsync(nombre);
    }
}

public interface ILoteRepository : IRepository<Lote>
{
    Task<Lote?> GetByNameAsync(string nombre);
    Task<List<Lote>> GetByCampoAsync(int idCampo);
}

public class LoteRepository : BaseRepository<Lote>, ILoteRepository
{
    public LoteRepository(DatabaseService databaseService) : base(databaseService) { }

    public async Task<Lote?> GetByNameAsync(string nombre)
    {
        return await _databaseService.GetLoteByNameAsync(nombre);
    }

    public async Task<List<Lote>> GetByCampoAsync(int idCampo)
    {
        return await _databaseService.GetLotesByCampoAsync(idCampo);
    }
}

// Repositories for more complex catalogs

public interface IRecetaRepository : IRepository<Receta>
{
    Task<Receta?> GetByNameAsync(string nombreReceta);
    Task<List<Receta>> GetByAlmacenAsync(int almacenId);
    Task<List<Receta>> GetByTipoAsync(int tipoReceta);
    Task<Receta?> GetRecetaWithArticulosAsync(int idReceta);
    Task<List<RecetaArticulo>> GetArticulosByRecetaIdAsync(int idReceta);
    Task<int> SaveRecetaWithArticulosAsync(Receta receta);
    Task<int> DeleteRecetaWithArticulosAsync(int idReceta);
    Task<int> AddArticuloToRecetaAsync(int idReceta, RecetaArticulo articulo);
    Task<int> RemoveArticuloFromRecetaAsync(int idReceta, int idArticulo);
}

public class RecetaRepository : BaseRepository<Receta>, IRecetaRepository
{
    public RecetaRepository(DatabaseService databaseService) : base(databaseService) { }

    public async Task<Receta?> GetByNameAsync(string nombreReceta)
    {
        return await _databaseService.GetRecetaByNameAsync(nombreReceta);
    }

    public async Task<List<Receta>> GetByAlmacenAsync(int almacenId)
    {
        return await _databaseService.GetRecetasByAlmacenAsync(almacenId);
    }

    public async Task<List<Receta>> GetByTipoAsync(int tipoReceta)
    {
        return await _databaseService.GetRecetasByTipoAsync(tipoReceta);
    }

    public async Task<Receta?> GetRecetaWithArticulosAsync(int idReceta)
    {
        return await _databaseService.GetRecetaWithArticulosAsync(idReceta);
    }

    public async Task<List<RecetaArticulo>> GetArticulosByRecetaIdAsync(int idReceta)
    {
        return await _databaseService.GetRecetaArticulosByRecetaAsync(idReceta);
    }

    public async Task<int> SaveRecetaWithArticulosAsync(Receta receta)
    {
        // Guardar la receta primero
        var result = await _databaseService.SaveAsync(receta);
        
        // Si la receta se guardó exitosamente y tiene artículos
        if (result > 0 && receta.Articulos != null && receta.Articulos.Any())
        {
            // Asegurar que todos los artículos tengan el IdReceta correcto
            foreach (var articulo in receta.Articulos)
            {
                articulo.IdReceta = receta.Id;
                await _databaseService.SaveAsync(articulo);
            }
        }
        
        return result;
    }

    public async Task<int> DeleteRecetaWithArticulosAsync(int idReceta)
    {
        // Primero eliminar todos los artículos de la receta
        await _databaseService.DeleteRecetaArticulosByRecetaAsync(idReceta);
        
        // Luego eliminar la receta
        return await _databaseService.DeleteByIdAsync<Receta>(idReceta);
    }

    public async Task<int> AddArticuloToRecetaAsync(int idReceta, RecetaArticulo articulo)
    {
        articulo.IdReceta = idReceta;
        return await _databaseService.SaveAsync(articulo);
    }

    public async Task<int> RemoveArticuloFromRecetaAsync(int idReceta, int idArticulo)
    {
        var articuloToRemove = await _databaseService.GetRecetaArticuloByRecetaAndArticuloAsync(idReceta, idArticulo);
        if (articuloToRemove != null)
        {
            return await _databaseService.DeleteAsync(articuloToRemove);
        }
        return 0;
    }
}

public interface IRecetaArticuloRepository : IRepository<RecetaArticulo>
{
    Task<List<RecetaArticulo>> GetByRecetaAsync(int idReceta);
    Task<List<RecetaArticulo>> GetByArticuloAsync(int idArticulo);
    Task<RecetaArticulo?> GetByRecetaAndArticuloAsync(int idReceta, int idArticulo);
    Task<int> DeleteByRecetaAsync(int idReceta);
}

public class RecetaArticuloRepository : BaseRepository<RecetaArticulo>, IRecetaArticuloRepository
{
    public RecetaArticuloRepository(DatabaseService databaseService) : base(databaseService) { }

    public async Task<List<RecetaArticulo>> GetByRecetaAsync(int idReceta)
    {
        return await _databaseService.GetRecetaArticulosByRecetaAsync(idReceta);
    }

    public async Task<List<RecetaArticulo>> GetByArticuloAsync(int idArticulo)
    {
        return await _databaseService.GetRecetaArticulosByArticuloAsync(idArticulo);
    }

    public async Task<RecetaArticulo?> GetByRecetaAndArticuloAsync(int idReceta, int idArticulo)
    {
        return await _databaseService.GetRecetaArticuloByRecetaAndArticuloAsync(idReceta, idArticulo);
    }

    public async Task<int> DeleteByRecetaAsync(int idReceta)
    {
        return await _databaseService.DeleteRecetaArticulosByRecetaAsync(idReceta);
    }
}