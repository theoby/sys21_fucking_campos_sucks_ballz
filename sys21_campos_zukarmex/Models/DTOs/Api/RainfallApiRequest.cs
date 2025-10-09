using Newtonsoft.Json;
using System;

namespace sys21_campos_zukarmex.Models.DTOs.Api
{
    public class RainfallApiRequest
    {
        [JsonProperty("fecha")]
        public DateTime Fecha { get; set; }

        [JsonProperty("idPluviometro")]
        public int IdPluviometro { get; set; }

        [JsonProperty("precipitacion")]
        public decimal Precipitacion { get; set; }

        [JsonProperty("lat")]
        public string Lat { get; set; }

        [JsonProperty("lng")]
        public string Lng { get; set; }
    }
}