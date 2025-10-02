using SQLite;

namespace sys21_campos_zukarmex.Models;

[Table("Familia")]
public class Familia
{
    [PrimaryKey]
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool RequiereAutorizacion { get; set; }
    public bool UsaMaquinaria { get; set; }
}