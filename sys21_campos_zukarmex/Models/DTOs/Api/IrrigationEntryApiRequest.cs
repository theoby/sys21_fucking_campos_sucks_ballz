using Newtonsoft.Json;
using System;

namespace sys21_campos_zukarmex.Models.DTOs.Api
{
    public class IrrigationEntryApiRequest
    {
        [JsonProperty("idCampo")]
        public int IdCampo { get; set; }

        [JsonProperty("idLineaRiego")]
        public int IdLineaRiego { get; set; }

        [JsonProperty("fecha")]
        public DateTime Fecha { get; set; }

        [JsonProperty("equiposBombeoOperando")]
        public int EquiposBombeoOperando { get; set; }

        [JsonProperty("observaciones")]
        public string Observaciones { get; set; } = string.Empty;

        [JsonProperty("Lat")]
        public string Lat { get; set; }

        [JsonProperty("Lng")]
        public string Lng { get; set; }
    }
}