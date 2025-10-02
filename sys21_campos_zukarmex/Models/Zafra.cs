using SQLite;
using System.Text.Json.Serialization;

namespace sys21_campos_zukarmex.Models;

[Table("Zafra")]
public class Zafra
{
    [PrimaryKey]
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string NombreZoca { get; set; } = string.Empty;
    public string NombrePlanta { get; set; } = string.Empty;
    public DateTime FechaInicial { get; set; }
    public DateTime FechaFinal { get; set; }
    public DateTime FechaInicialCosecha { get; set; }
    public DateTime FechaFinalCosecha { get; set; }
    public bool Activa { get; set; }
    public bool IsActive { get; set; }

    [Ignore]
    public string DisplayText => $"{Nombre} ({FechaInicial:yyyy} - {FechaFinal:yyyy})";

    [Ignore]
    public bool IsCurrentActive => Activa || IsActive;
}