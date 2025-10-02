using Newtonsoft.Json;
using System.Collections.Generic;

namespace sys21_campos_zukarmex.Models.DTOs.Api
{

    public class SaldoApiResponse
    {
        [JsonProperty("estado")]
        public int Estado { get; set; }

        [JsonProperty("datos")]
        public SaldoData? Datos { get; set; }

        [JsonProperty("totalDatos")]
        public int TotalDatos { get; set; }

        [JsonProperty("mensaje")]
        public string? Mensaje { get; set; }
    }

    public class SaldoData
    {
        [JsonProperty("almacen")]
        public AlmacenInfo? Almacen { get; set; }

        [JsonProperty("articulo")]
        public ArticuloInfo? Articulo { get; set; }

        [JsonProperty("cantidad")]
        public decimal Cantidad { get; set; } 
    }

    public class AlmacenInfo
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("nombre")]
        public string? Nombre { get; set; }
    }

    public class ArticuloInfo
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("nombre")]
        public string? Nombre { get; set; }

        [JsonProperty("unidad")]
        public string? Unidad { get; set; }
    }
}