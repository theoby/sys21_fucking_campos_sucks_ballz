using SQLite;

namespace sys21_campos_zukarmex.Models;

[Table("User")]
public class User
{
    [PrimaryKey]
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int IdApp { get; set; }
    public int IdEmpresa { get; set; }
    public int Tipo { get; set; }
    public int TipoUsuario { get; set; }
    public int IdInspector { get; set; }
    public bool IsActive { get; set; } = true;
}