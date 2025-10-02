using SQLite;

namespace sys21_campos_zukarmex.Models;

[Table("Maquinaria")]
public class Maquinaria
{
    [PrimaryKey]
    public int IdPk { get; set; }
    public int IdMaquinaria { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int IdGrupo { get; set; }
    public string NombreGrupo { get; set; } = string.Empty;
}