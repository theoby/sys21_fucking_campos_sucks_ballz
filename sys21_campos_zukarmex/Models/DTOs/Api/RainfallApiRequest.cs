using Newtonsoft.Json;

public class RainfallApiRequest
{
    [JsonProperty("fecha")]
    public DateTime Fecha { get; set; }
    [JsonProperty("idPluviometro")]
    public int IdPluviometro { get; set; }
    [JsonProperty("precipitacion")]
    public decimal Precipitacion { get; set; }
}