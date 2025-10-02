using SQLite;
using System.Text.Json.Serialization;

namespace sys21_campos_zukarmex.Models;

[Table("Ciclo")]
public class Ciclo
{
    [PrimaryKey]
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}