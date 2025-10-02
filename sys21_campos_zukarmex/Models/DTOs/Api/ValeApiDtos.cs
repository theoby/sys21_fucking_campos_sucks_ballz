using Newtonsoft.Json;

namespace sys21_campos_zukarmex.Models.DTOs.Api;

/// <summary>
/// DTO para el env�o de vales a la API
/// </summary>
public class ValeApiRequest
{
    [JsonProperty("campo")]
    public int Campo { get; set; }

    [JsonProperty("almacen")]
    public int Almacen { get; set; }

    [JsonProperty("fecha")]
    public DateTime Fecha { get; set; }
    
    [JsonProperty("concepto")]
    public string Concepto { get; set; } = string.Empty;

    [JsonProperty("id_receta")]
    public int Id_Receta { get; set; }

    [JsonProperty("articulos")]
    public List<ArticuloApiRequest> Articulos { get; set; } = new();
}

/// <summary>
/// DTO para los art�culos dentro del vale
/// </summary>
public class ArticuloApiRequest
{
    [JsonProperty("familia")]
    public int Familia { get; set; }
    
    [JsonProperty("subfamilia")]
    public int SubFamilia { get; set; }
    
    [JsonProperty("articulo")]
    public int Articulo { get; set; }
    
    [JsonProperty("cantidad")]
    public decimal Cantidad { get; set; }
    
    [JsonProperty("concepto")]
    public string Concepto { get; set; } = string.Empty;
    
    [JsonProperty("centro_costo")]
    public int CentroCosto { get; set; }

    [JsonProperty("idMaquinaria")]
    public int IdMaquinaria { get; set; }
    
    [JsonProperty("idGrupo")]
    public int IdGrupo { get; set; }
}