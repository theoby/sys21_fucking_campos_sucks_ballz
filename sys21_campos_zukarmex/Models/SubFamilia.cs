using SQLite;
using System.Text.Json.Serialization;

namespace sys21_campos_zukarmex.Models;

[Table("SubFamilia")]
public class SubFamilia
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int IdFamilia { get; set; }

    private Familia _familia;

    [Ignore]
    [JsonPropertyName("familia")]
    public Familia Familia
    {
        get => _familia;
        set
        {
            _familia = value;
            IdFamilia = value?.Id ?? 0;
        }
    }
}