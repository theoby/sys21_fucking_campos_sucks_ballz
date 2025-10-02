using SQLite;
using System.Text.Json.Serialization;


namespace sys21_campos_zukarmex.Models;

[Table("Almacen")]
public class Almacen
{
    [PrimaryKey]
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int IdCampo { get; set; }

    private Campo _campo;

    [Ignore]
    [JsonPropertyName("campo")]
    public Campo Campo
    {
        get => _campo;
        set
        {
            _campo = value;
            IdCampo = value?.Id ?? 0;
        }
    }
}