using sys21_campos_zukarmex.Models;

namespace sys21_campos_zukarmex.Services.Repositories;

public interface IConfiguracionRepository : IRepository<Configuracion>
{
    Task<Configuracion?> GetConfiguracionActivaAsync();
    Task<List<Configuracion>> GetConfiguracionesByDispositivoAsync(string dispositivo);
    Task<Configuracion?> GetUltimaConfiguracionAsync();
}

public class ConfiguracionRepository : BaseRepository<Configuracion>, IConfiguracionRepository
{
    public ConfiguracionRepository(DatabaseService databaseService) : base(databaseService)
    {
    }

    public async Task<Configuracion?> GetConfiguracionActivaAsync()
    {
        var configuraciones = await _databaseService.GetAllAsync<Configuracion>();
        return configuraciones.OrderByDescending(c => c.Fecha).FirstOrDefault();
    }

    public override async Task<int> SaveAsync(Configuracion entity)
    {
      
        var existingConfig = await GetConfiguracionActivaAsync();

        if (existingConfig != null)
        {
            entity.Id = existingConfig.Id;
            return await _databaseService.SaveAsync(entity);
        }
        else
        {
            return await _databaseService.SaveAsync(entity);
        }
    }

    public async Task<List<Configuracion>> GetConfiguracionesByDispositivoAsync(string dispositivo)
    {
        return await GetWhereAsync(c => c.Dispositivo == dispositivo);
    }

    public async Task<Configuracion?> GetUltimaConfiguracionAsync()
    {
        return await GetConfiguracionActivaAsync();
    }
}