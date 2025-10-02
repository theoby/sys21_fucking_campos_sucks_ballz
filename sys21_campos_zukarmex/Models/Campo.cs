using SQLite;
using System.Text.Json.Serialization;

namespace sys21_campos_zukarmex.Models;

[Table("Campo")]
public class Campo
{
    [PrimaryKey]
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int IdInspector { get; set; }
    public string NombreInspector { get; set; } = string.Empty;
    public int IdEmpresa { get; set; }
    public int IdPredio { get; set; }

    private Inspector _inspector;

    [Ignore]
    [JsonPropertyName("inspector")]
    public Inspector Inspector
    {
        get => _inspector;
        set
        {
            _inspector = value;
            IdInspector = value?.Id ?? 0;
            NombreInspector = value.Nombre;
        }
    }

}