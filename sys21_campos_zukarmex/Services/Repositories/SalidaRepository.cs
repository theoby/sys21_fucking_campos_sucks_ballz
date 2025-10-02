using sys21_campos_zukarmex.Models;

namespace sys21_campos_zukarmex.Services.Repositories;

public interface ISalidaRepository : IRepository<Salida>
{
    Task<List<Salida>> GetByUserAsync(string username);
    Task<List<Salida>> GetByStatusAsync(string status);
    Task<List<Salida>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate);
    Task<List<Salida>> GetPendingAuthorizationAsync();
    Task<Salida?> GetWithDetailsAsync(int id);
    Task<int> SaveWithDetailsAsync(Salida salida, List<SalidaDetalle> detalles);
}

public class SalidaRepository : BaseRepository<Salida>, ISalidaRepository
{
    private readonly ISalidaDetalleRepository _detalleRepository;

    public SalidaRepository(DatabaseService databaseService, ISalidaDetalleRepository detalleRepository) 
        : base(databaseService)
    {
        _detalleRepository = detalleRepository;
    }

    public async Task<List<Salida>> GetByUserAsync(string username)
    {
        return await _databaseService.GetValesByUserAsync(username);
    }

    public async Task<List<Salida>> GetByStatusAsync(string status)
    {
        return await _databaseService.GetValesByStatusAsync(status);
    }

    public async Task<List<Salida>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        return await _databaseService.GetValesByDateRangeAsync(fromDate, toDate);
    }

    public async Task<List<Salida>> GetPendingAuthorizationAsync()
    {
        return await _databaseService.GetValesPendingAuthorizationAsync();
    }

    public async Task<Salida?> GetWithDetailsAsync(int id)
    {
        var salida = await GetByIdAsync(id);
        if (salida != null)
        {
            // Note: In a real implementation, you might want to add a property to Salida to hold the details
            // For now, you would need to call the detail repository separately
        }
        return salida;
    }

    public async Task<int> SaveWithDetailsAsync(Salida salida, List<SalidaDetalle> detalles)
    {
        var result = 0;

        await _databaseService.ExecuteInTransactionAsync(tran =>
        {
            // Save the main Salida record
            result = _databaseService.SaveAsync(salida).Result;

            if (result > 0)
            {
                // Delete existing details if updating
                if (salida.Id > 0)
                {
                    _databaseService.DeleteDetallesBySalidaAsync(salida.Id).Wait();
                }

                // Set the SalidaId for all details
                foreach (var detalle in detalles)
                {
                    detalle.IdSalida = salida.Id;
                }

                // Save the details
                _detalleRepository.SaveAllAsync(detalles).Wait();
            }
        });

        return result;
    }
}

public interface ISalidaDetalleRepository : IRepository<SalidaDetalle>
{
    Task<List<SalidaDetalle>> GetBySalidaAsync(int idSalida);
    Task<int> DeleteBySalidaAsync(int idSalida);
}

public class SalidaDetalleRepository : BaseRepository<SalidaDetalle>, ISalidaDetalleRepository
{
    public SalidaDetalleRepository(DatabaseService databaseService) : base(databaseService)
    {
    }

    public async Task<List<SalidaDetalle>> GetBySalidaAsync(int idSalida)
    {
        return await _databaseService.GetDetallesBySalidaAsync(idSalida);
    }

    public async Task<int> DeleteBySalidaAsync(int idSalida)
    {
        return await _databaseService.DeleteDetallesBySalidaAsync(idSalida);
    }
}