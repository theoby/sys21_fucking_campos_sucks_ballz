using Newtonsoft.Json;

public class MachineryUsageApiRequest
{
    [JsonProperty("idGrupo")]
    public int IdGrupo { get; set; }

    [JsonProperty("idMaquinaria")]
    public int IdMaquinaria { get; set; }

    [JsonProperty("idCampo")]
    public int IdCampo { get; set; }

    [JsonProperty("horasTrabajadas")]
    public decimal HorasTrabajadas { get; set; }

    [JsonProperty("kilometrajeOdometro")]
    public decimal KilometrajeOdometro { get; set; }

    [JsonProperty("fecha")]
    public decimal Fecha { get; set; }

    [JsonProperty("lat")]
    public string Lat { get; set; }

    [JsonProperty("lng")]
    public string Lng { get; set; }
}