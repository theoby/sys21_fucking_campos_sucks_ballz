using System.Linq.Expressions;

namespace sys21_campos_zukarmex.Services.Repositories;

public interface IRepository<T> where T : class
{
    Task<List<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<List<T>> GetWhereAsync(Expression<Func<T, bool>> predicate);
    Task<int> CountAsync();
    Task<int> CountWhereAsync(Expression<Func<T, bool>> predicate);
    Task<int> CreateAsync(T entity);
    Task<int> UpdateAsync(T entity);
    Task<int> SaveAsync(T entity);
    Task<int> SaveAllAsync(List<T> entities);
    Task<int> DeleteAsync(T entity);
    Task<int> DeleteByIdAsync(int id);
    Task<int> DeleteWhereAsync(Expression<Func<T, bool>> predicate);
    Task<int> ClearAllAsync();
    Task<bool> ExistsAsync(int id);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
}

public class BaseRepository<T> : IRepository<T> where T : class, new()
{
    protected readonly DatabaseService _databaseService;

    public BaseRepository(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public virtual async Task<List<T>> GetAllAsync()
    {
        return await _databaseService.GetAllAsync<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _databaseService.GetByIdAsync<T>(id);
    }

    public virtual async Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        return await _databaseService.GetFirstOrDefaultAsync<T>(predicate);
    }

    public virtual async Task<List<T>> GetWhereAsync(Expression<Func<T, bool>> predicate)
    {
        return await _databaseService.GetWhereAsync<T>(predicate);
    }

    public virtual async Task<int> CountAsync()
    {
        return await _databaseService.CountAsync<T>();
    }

    public virtual async Task<int> CountWhereAsync(Expression<Func<T, bool>> predicate)
    {
        return await _databaseService.CountWhereAsync<T>(predicate);
    }

    public virtual async Task<int> CreateAsync(T entity)
    {
        // Ensure ID is 0 for new entities
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty != null)
        {
            idProperty.SetValue(entity, 0);
        }
        return await _databaseService.SaveAsync(entity);
    }

    public virtual async Task<int> UpdateAsync(T entity)
    {
        return await _databaseService.SaveAsync(entity);
    }

    public virtual async Task<int> SaveAsync(T entity)
    {
        return await _databaseService.SaveAsync(entity);
    }

    public virtual async Task<int> SaveAllAsync(List<T> entities)
    {
        return await _databaseService.SaveAllAsync(entities);
    }

    public virtual async Task<int> DeleteAsync(T entity)
    {
        return await _databaseService.DeleteAsync(entity);
    }

    public virtual async Task<int> DeleteByIdAsync(int id)
    {
        return await _databaseService.DeleteByIdAsync<T>(id);
    }

    public virtual async Task<int> DeleteWhereAsync(Expression<Func<T, bool>> predicate)
    {
        return await _databaseService.DeleteWhereAsync<T>(predicate);
    }

    public virtual async Task<int> ClearAllAsync()
    {
        return await _databaseService.ClearTableAsync<T>();
    }

    public virtual async Task<bool> ExistsAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        return entity != null;
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        var entity = await GetFirstOrDefaultAsync(predicate);
        return entity != null;
    }
}