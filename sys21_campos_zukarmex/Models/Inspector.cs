using SQLite;

namespace sys21_campos_zukarmex.Models;

[Table("Inspector")]
public class Inspector
{
    [PrimaryKey]
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}