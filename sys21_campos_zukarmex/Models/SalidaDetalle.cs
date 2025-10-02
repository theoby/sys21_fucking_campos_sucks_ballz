using SQLite;
using System.Text.Json.Serialization;

namespace sys21_campos_zukarmex.Models;

[Table("SalidaDetalle")]
public class SalidaDetalle
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int IdSalida { get; set; }
    public int SalidaId { get; set; }
    public int IdFamilia { get; set; }
    public string FamiliaNombre { get; set; } = string.Empty; //nuevo Nombre
    public int IdSubFamilia { get; set; }
    public string SubFamiliaNombre { get; set; } = string.Empty; //nuevo Nombre
    public int IdArticulo { get; set; }
    public string ArticuloNombre { get; set; } = string.Empty; // nuevo Nombre
    public decimal Cantidad { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public string ConceptoDeterminado { get; set; } = string.Empty;
    public string Unidad { get; set; } = string.Empty;
    public int IdLote { get; set; } // Nuevo campo para lote
    public string LoteNombre { get; set; } = string.Empty; // Nuevo campo para lote
    public decimal LoteHectarea { get; set; }
    public int IdMaquinaria { get; set; }
    public string MaquinariaNombre { get; set; } = string.Empty; //nuevo nombre
    public int IdGrupoMaquinaria { get; set; }
    public string MaquinariaNombreGrupo { get; set; } = string.Empty; //nuevo nombre
    public string FolioSalida { get; set; } = string.Empty;
    public int OrdenSalida { get; set; }

    #region Relations

    //Tablas a relacionar
    private Salida? _salida;
    private Familia? _familia;
    private SubFamilia? _subfamilia;
    private Articulo? _articulo;
    private Maquinaria? _maquinaria;
    private Lote? _lote;

    //Propiedades relacionadas
    [Ignore]
    [JsonPropertyName("salida")]
    public Salida? Salida
    {
        get => _salida;
        set
        {
            _salida = value;
            SalidaId = value?.Id ?? 0;
        }
    }

    [Ignore]
    [JsonPropertyName("familia")]
    public Familia? Familia
    {
        get => _familia;
        set
        {
            _familia = value;
            IdFamilia = value?.Id ?? 0;
            FamiliaNombre = value?.Nombre ?? string.Empty;
        }
    }

    [Ignore]
    [JsonPropertyName("subfamilia")]
    public SubFamilia? SubFamilia
    {
        get => _subfamilia;
        set
        {
            _subfamilia = value;
            IdSubFamilia = value?.Id ?? 0;
            SubFamiliaNombre = value?.Nombre ?? string.Empty;
        }
    }

    [Ignore]
    [JsonPropertyName("articulo")]
    public Articulo? Articulo
    {
        get => _articulo;
        set
        {
            _articulo = value;
            IdArticulo = value?.Id ?? 0;
            ArticuloNombre = value?.Nombre ?? string.Empty;
        }
    }

    [Ignore]
    [JsonPropertyName("maquinaria")]
    public Maquinaria? Maquinaria   
    {
        get => _maquinaria;
        set
        {
            _maquinaria = value;
            IdMaquinaria = value?.IdPk ?? 0;
            IdGrupoMaquinaria = value?.IdGrupo ?? 0;
            MaquinariaNombre = value?.Nombre ?? string.Empty;
            MaquinariaNombreGrupo = value?.NombreGrupo ?? string.Empty;
        }
    }

    [Ignore]
    [JsonPropertyName("lote")]
    public Lote? Lote
    {
        get => _lote;
        set
        {
            _lote = value;
            IdLote = value?.Id ?? 0;
            LoteNombre = value?.Nombre ?? string.Empty;
            LoteHectarea = value?.Hectareas ?? 0;
        }
    }
    #endregion

}