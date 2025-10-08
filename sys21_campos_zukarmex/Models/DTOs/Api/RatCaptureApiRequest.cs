using Newtonsoft.Json;

namespace sys21_campos_zukarmex.Models.DTOs.Api
{
    public class RatCaptureApiRequest
    {
        [JsonProperty("idTemporada")]
        public int IdTemporada { get; set; }

        [JsonProperty("idCampo")]
        public int IdCampo { get; set; }

        [JsonProperty("fecha")]
        public DateTime Fecha { get; set; }

        [JsonProperty("cantidadTrampas")]
        public int CantidadTrampas { get; set; }

        [JsonProperty("cantidadMachos")]
        public int CantidadMachos { get; set; }

        [JsonProperty("cantidadHembras")]
        public int CantidadHembras { get; set; }
        [JsonProperty("lat")]
        public string Lat { get; set; }

        [JsonProperty("lng")]
        public string Lng { get; set; }
    }
}