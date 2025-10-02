using Newtonsoft.Json;
using System.Collections.Generic;

namespace sys21_campos_zukarmex.Models.DTOs.Api
{
    // Clase para la respuesta completa de la API de detalles
    public class ValeDetalleApiResponse
    {
        [JsonProperty("estado")]
        public int Estado { get; set; }

        [JsonProperty("datos")]
        public List<ValeDetalleItemDto> Datos { get; set; } = new();

        [JsonProperty("totalDatos")]
        public int TotalDatos { get; set; }

        [JsonProperty("mensaje")]
        public string? Mensaje { get; set; }
    }

    // Clase para cada artículo en la lista de "datos"
    public class ValeDetalleItemDto
    {
        [JsonProperty("idValeSalida")]
        public int IdValeSalida { get; set; }

        [JsonProperty("articulo")]
        public string Articulo { get; set; } = string.Empty;

        [JsonProperty("unidad")]
        public string Unidad { get; set; } = string.Empty;

        [JsonProperty("centroCosto")]
        public string CentroCosto { get; set; } = string.Empty;

        [JsonProperty("concepto")]
        public string Concepto { get; set; } = string.Empty;

        [JsonProperty("cantidad")]
        public decimal Cantidad { get; set; }
    }
}