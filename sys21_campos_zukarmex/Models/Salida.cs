using SQLite;
using System.Text.Json.Serialization;

namespace sys21_campos_zukarmex.Models;

[Table("Salida")]
public class Salida
{
    [PrimaryKey,AutoIncrement]
    public int Id { get; set; } // ID local
    public string Folio { get; set; } //Folio desde Api
    public int IdCampo { get; set; }
    public int IdAlmacen { get; set; }
    public int IdLote { get; set; } // Nuevo campo para lote
    public int IdReceta { get; set; } // Nuevo campo para receta seleccionada
    public DateTime Fecha { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public bool Status { get; set; }
    public string? StatusText { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public bool? Autorizado { get; set; }
    public string TipoReceta { get; set; } = string.Empty; // Indica si es una receta
    public string? AutorizadoPor { get; set; }
    public DateTime? FechaAutorizacion { get; set; }

    [Ignore]
    public string CampoNombre { get; set; } = string.Empty;

    [Ignore]
    public string AlmacenNombre { get; set; } = string.Empty;
    [Ignore]
    public string LoteNombre { get; set; } = string.Empty;
    [Ignore]
    public string RecetaNombre { get; set; } = string.Empty;

    [Ignore]
    public List<SalidaDetalle> SalidaDetalle { get; set; } = new();
    
    [Ignore]
    public string StatusDisplay => Status ? "Sincronizado" : "Pendiente";
    
    [Ignore]
    public string AutorizacionDisplay => Autorizado.HasValue 
        ? (Autorizado.Value ? "Autorizado" : "Rechazado") 
        : "Pendiente";

    private Campo _campo;
    [Ignore]
    [JsonPropertyName("campo")]
    public Campo Campo
    {
        get => _campo;
        set
        {
            _campo = value;
            CampoNombre = value.Nombre;
            IdCampo = value.Id;
        }
    }

    private Almacen _almacen;
    [Ignore]
    [JsonPropertyName("almacen")]
    public Almacen Almacen
    {
        get => _almacen;
        set
        {
            _almacen = value;
            AlmacenNombre = value.Nombre;
            IdAlmacen = value.Id;
        }
    }

    private Lote _lote;
    [Ignore]
    [JsonPropertyName("lote")]
    public Lote Lote
    {
        get => _lote;
        set
        {
            _lote = value;
            LoteNombre = value.Nombre;
            IdLote = value.Id;
        }
    }

    private Receta _receta;
    [Ignore]
    [JsonPropertyName("receta")]
    public Receta Receta
    {
        get => _receta;
        set
        {
            _receta = value;
            RecetaNombre = value.NombreReceta;
            IdReceta = value.IdReceta;
        }
    }
}