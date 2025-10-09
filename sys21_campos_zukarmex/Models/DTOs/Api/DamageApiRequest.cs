using Newtonsoft.Json;
using System;

namespace sys21_campos_zukarmex.Models.DTOs.Api
{
    public class DamageApiRequest
    {
        [JsonProperty("idTemporada")]
        public int IdTemporada { get; set; }

        [JsonProperty("idCampo")]
        public int IdCampo { get; set; }

        [JsonProperty("idCiclo")]
        public int IdCiclo { get; set; }

        [JsonProperty("fecha")]
        public DateTime Fecha { get; set; }

        [JsonProperty("numeroTallos")]
        public int NumeroTallos { get; set; }

        [JsonProperty("dañoViejo")]
        public int DanoViejo { get; set; }

        [JsonProperty("dañoNuevo")]
        public int DanoNuevo { get; set; }

        [JsonProperty("Lat")]
        public string Lat { get; set; }

        [JsonProperty("Lng")]
        public string Lng { get; set; }
    }
}