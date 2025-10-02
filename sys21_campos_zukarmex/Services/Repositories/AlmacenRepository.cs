using sys21_campos_zukarmex.Models;

namespace sys21_campos_zukarmex.Services.Repositories;

public interface IAlmacenRepository : IRepository<Almacen>
{
    Task<List<Almacen>> GetByCampoAsync(int idCampo);
    Task<Almacen?> GetByNameAsync(string nombre);
}

public class AlmacenRepository : BaseRepository<Almacen>, IAlmacenRepository
{
    public AlmacenRepository(DatabaseService databaseService) : base(databaseService)
    {
    }

    public async Task<List<Almacen>> GetByCampoAsync(int idCampo)
    {
        return await _databaseService.GetAlmacenesByCampoAsync(idCampo);
    }

    public async Task<Almacen?> GetByNameAsync(string nombre)
    {
        return await _databaseService.GetAlmacenByNameAsync(nombre);
    }
}