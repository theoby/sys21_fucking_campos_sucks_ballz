using sys21_campos_zukarmex.Models;

namespace sys21_campos_zukarmex.Services.Repositories;

public interface ICampoRepository : IRepository<Campo>
{
    Task<List<Campo>> GetByInspectorAsync(int idInspector);
    Task<List<Campo>> GetByEmpresaAsync(int idEmpresa);
    Task<Campo?> GetByNameAsync(string nombre);
}

public class CampoRepository : BaseRepository<Campo>, ICampoRepository
{
    public CampoRepository(DatabaseService databaseService) : base(databaseService)
    {
    }

    public async Task<List<Campo>> GetByInspectorAsync(int idInspector)
    {
        return await _databaseService.GetCamposByInspectorAsync(idInspector);
    }

    public async Task<List<Campo>> GetByEmpresaAsync(int idEmpresa)
    {
        return await _databaseService.GetCamposByEmpresaAsync(idEmpresa);
    }

    public async Task<Campo?> GetByNameAsync(string nombre)
    {
        return await _databaseService.GetCampoByNameAsync(nombre);
    }
}