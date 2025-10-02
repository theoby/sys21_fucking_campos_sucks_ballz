using SQLite;

namespace sys21_campos_zukarmex.Models;

[Table("Empresa")]
public class Empresa
{
    [PrimaryKey]
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool IsPromotora { get; set; } 
}