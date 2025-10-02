using SQLite;
using System.Text.Json.Serialization;

namespace sys21_campos_zukarmex.Models;

[Table("Articulo")]
public class Articulo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Unidad { get; set; } = string.Empty;
    public int IdSubFamilia { get; set; }
    public int IdFamilia { get; set; }


    private SubFamilia _subfamilia;

    [Ignore]
    [JsonPropertyName("subfamilia")]
    public SubFamilia SubFamilia
    {
        get => _subfamilia;
        set
        {
            _subfamilia = value;
            IdSubFamilia = value?.Id ?? 0;

        }
    }

}