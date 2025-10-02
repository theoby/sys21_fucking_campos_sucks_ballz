using sys21_campos_zukarmex.Models;

namespace sys21_campos_zukarmex.Services.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<List<User>> GetByTipoAsync(int tipo);
    Task<bool> ValidateCredentialsAsync(string username, string password);
}

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(DatabaseService databaseService) : base(databaseService)
    {
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _databaseService.GetUserByUsernameAsync(username);
    }

    public async Task<List<User>> GetByTipoAsync(int tipo)
    {
        return await _databaseService.GetUsersByTipoAsync(tipo);
    }

    public async Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        var user = await GetByUsernameAsync(username);
        return user != null && user.Password == password; // Note: In production, use proper password hashing
    }
}

public interface ISessionRepository : IRepository<Session>
{
    Task<Session?> GetActiveAsync();
    Task<int> ClearActiveSessionsAsync();
    Task<int> CreateSessionAsync(User user, string token);
}

public class SessionRepository : BaseRepository<Session>, ISessionRepository
{
    public SessionRepository(DatabaseService databaseService) : base(databaseService)
    {
    }

    public async Task<Session?> GetActiveAsync()
    {
        return await _databaseService.GetActiveSessionAsync();
    }

    public async Task<int> ClearActiveSessionsAsync()
    {
        return await _databaseService.ClearActiveSessionsAsync();
    }

    public async Task<int> CreateSessionAsync(User user, string token)
    {
        // Clear any existing active sessions
        await ClearActiveSessionsAsync();

        // Create new session
        var session = new Session
        {
            UserId = user.Id,
            Username = user.Username,
            Token = token,
            IsActive = true,
            CreatedAt = DateTime.Now,
            ExpiresAt = DateTime.Now.AddHours(8) // 8 hour session
        };

        return await CreateAsync(session);
    }
}