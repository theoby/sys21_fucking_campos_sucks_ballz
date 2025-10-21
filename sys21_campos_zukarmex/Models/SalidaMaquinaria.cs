using SQLite;

namespace sys21_campos_zukarmex.Models;

public class SalidaMaquinaria
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int? IdGrupo { get; set; }

    public int IdMaquinaria { get; set; } = 0;
    public int IdCampo { get; set; } = 0;
    public decimal? HorasTrabajadas { get; set; }
    public DateTime? Fecha { get; set; }

    public decimal? KilometrajeOdometro { get; set; }
    public string? Lng { get; set; } = string.Empty;
    public string? Lat { get; set; } = string.Empty;

    [Ignore]
    public string CampoNombre { get; set; } = "N/D";

    [Ignore]
    public string MaquinariaNombre { get; set; } = "N/D";

    [Ignore]
    public string EmpresaNombre { get; set; } = "N/D";
}