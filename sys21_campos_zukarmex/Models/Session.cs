using SQLite;

namespace sys21_campos_zukarmex.Models;

[Table("Session")]
public class Session
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public int IdEmpresa { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime ExpirationDate { get; set; }
    public bool IsActive { get; set; }
    public int TipoUsuario { get; set; }
    public int IdInspector { get; set; }
    public bool IsPromotora { get; set; }
    
    /// <summary>
    /// Determina si el sistema debe funcionar en modo online (true) o offline (false)
    /// Por defecto es true (online)
    /// </summary>
    public bool IsOnline { get; set; } = true;
}