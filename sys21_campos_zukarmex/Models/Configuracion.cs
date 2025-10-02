using SQLite;

namespace sys21_campos_zukarmex.Models;

[Table("Configuracion")]
public class Configuracion
{
    [PrimaryKey]
    public int Id { get; set; }
    
    [Column("Ruta")]
    public string Ruta { get; set; } = string.Empty;
    
    public string Dispositivo { get; set; } = string.Empty;
    
    public DateTime Fecha { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Token de autenticación almacenado para verificar validez de sesión
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// Fecha de expiración del token
    /// </summary>
    public DateTime? TokenExpiration { get; set; }
    
    [Ignore]
    public string FechaDisplay => Fecha.ToString("dd/MM/yyyy HH:mm");
    
    [Ignore]
    public bool HasValidToken => !string.IsNullOrEmpty(Token) && 
                                (TokenExpiration == null || TokenExpiration > DateTime.Now);
}