using SQLite;

namespace sys21_campos_zukarmex.Models;

[Table("RegistroEstimado")]
public class RegistroEstimado
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int IdCampo { get; set; }
    public int IdTemporada { get; set; }
    public int IdCiclo { get; set; }
}