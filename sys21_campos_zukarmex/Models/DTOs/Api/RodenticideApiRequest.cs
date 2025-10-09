// EN: Models/DTOs/Api/RodenticideApiRequest.cs
using Newtonsoft.Json;
using System;

namespace sys21_campos_zukarmex.Models.DTOs.Api
{
    public class RodenticideApiRequest
    {
        [JsonProperty("idTemporada")]
        public int IdTemporada { get; set; }

        [JsonProperty("idCampo")]
        public int IdCampo { get; set; }

        [JsonProperty("fecha")]
        public DateTime Fecha { get; set; }

        [JsonProperty("cantidadComederos")]
        public int CantidadComederos { get; set; }

        [JsonProperty("cantidadPastillas")]
        public int CantidadPastillas { get; set; }

        [JsonProperty("cantidadConsumo")]
        public int CantidadConsumo { get; set; }

        [JsonProperty("lat")]
        public string Lat { get; set; }

        [JsonProperty("lng")]
        public string Lng { get; set; }
    }
}