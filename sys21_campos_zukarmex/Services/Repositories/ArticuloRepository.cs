using sys21_campos_zukarmex.Models;

namespace sys21_campos_zukarmex.Services.Repositories;

public interface IArticuloRepository : IRepository<Articulo>
{
    Task<List<Articulo>> GetByFamiliaAsync(int idFamilia);
    Task<List<Articulo>> GetBySubFamiliaAsync(int idSubFamilia);
    Task<Articulo?> GetByNameAsync(string nombre);
    Task<List<Articulo>> SearchByNameAsync(string searchTerm);
}

public class ArticuloRepository : BaseRepository<Articulo>, IArticuloRepository
{
    public ArticuloRepository(DatabaseService databaseService) : base(databaseService)
    {
    }

    public async Task<List<Articulo>> GetByFamiliaAsync(int idFamilia)
    {
        return await _databaseService.GetArticulosByFamiliaAsync(idFamilia);
    }

    public async Task<List<Articulo>> GetBySubFamiliaAsync(int idSubFamilia)
    {
        return await _databaseService.GetArticulosBySubFamiliaAsync(idSubFamilia);
    }

    public async Task<Articulo?> GetByNameAsync(string nombre)
    {
        return await _databaseService.GetArticuloByNameAsync(nombre);
    }

    public async Task<List<Articulo>> SearchByNameAsync(string searchTerm)
    {
        return await GetWhereAsync(a => a.Nombre.Contains(searchTerm));
    }
}