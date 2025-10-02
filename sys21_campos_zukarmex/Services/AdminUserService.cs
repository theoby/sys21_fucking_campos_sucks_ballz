using sys21_campos_zukarmex.Models;

namespace sys21_campos_zukarmex.Services;

public static class AdminUserService
{
    public const string ADMIN_USERNAME = "adminSys21";
    public const string ADMIN_PASSWORD = "S1s212020";
    
    public static User CreateAdminUser()
    {
        return new User
        {
            Id = -1, // ID especial para admin
            Username = ADMIN_USERNAME,
            NombreUsuario = ADMIN_USERNAME,
            NombreCompleto = "Administrador del Sistema",
            Password = ADMIN_PASSWORD,
            IdApp = 0,
            IdEmpresa = 0, // No requiere empresa
            Tipo = 0, // Tipo especial para admin
            TipoUsuario = 0,
            IdInspector = 0,
            IsActive = true
        };
    }
    
    public static bool IsAdminUser(string username, string password)
    {
        return username?.Trim().Equals(ADMIN_USERNAME, StringComparison.OrdinalIgnoreCase) == true &&
               password?.Trim().Equals(ADMIN_PASSWORD) == true;
    }
    
    public static bool IsAdminUser(User user)
    {
        return user?.Username?.Trim().Equals(ADMIN_USERNAME, StringComparison.OrdinalIgnoreCase) == true;
    }
}