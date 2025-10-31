using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SQLite;

namespace sys21_campos_zukarmex.Models
{
    public class SalidaMuestroDaños
    {
        [PrimaryKey, AutoIncrement]
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("idTemporada")]
        public int IdTemporada { get; set; } = 0;

        [JsonProperty("idCampo")]
        public int IdCampo { get; set; } = 0;

        [JsonProperty("idCiclo")]
        public int IdCiclo { get; set; } = 0;

        [JsonProperty("idLote")]
        public int? IdLote { get; set; } = 0;

        [JsonProperty("fecha")]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [JsonProperty("numeroTallos")]
        public int NumeroTallos { get; set; } = 0;

        [JsonProperty("dañoViejo")]
        public int DañoViejo { get; set; } = 0;

        [JsonProperty("dañoNuevo")]
        public int DañoNuevo { get; set; } = 0;

        [JsonProperty("lng")]
        public string Lng { get; set; } = string.Empty;

        [JsonProperty("lat")]
        public string Lat { get; set; } = string.Empty;

        [JsonProperty("dispositivo")]
        public string Dispositivo { get; set; } = string.Empty;

        [Ignore]
        [JsonProperty("usuario")]
        public string Usuario { get; set; } = string.Empty;

        [Ignore]
        [JsonProperty("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [Ignore]
        [JsonProperty("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [Ignore]
        [JsonProperty("modifiedBy")]
        public string ModifiedBy { get; set; } = string.Empty;

        [Ignore]
        public string ZafraNombre { get; set; } = "N/D";

        [Ignore]
        public string CampoNombre { get; set; } = "N/D";

        [Ignore]
        public string CicloNombre { get; set; } = "N/D";

    }
}
