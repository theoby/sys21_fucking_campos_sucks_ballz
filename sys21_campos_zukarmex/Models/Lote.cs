using SQLite;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sys21_campos_zukarmex.Models
{
    [Table("Lote")]
    public class Lote
    {
        [PrimaryKey]
        public int Id { get; set; }
        
        public string Nombre { get; set; } = string.Empty;
        
        public decimal Hectareas { get; set; }
        
        public int IdCampo { get; set; }
        
        // Propiedades de navegación (no se guardan en BD)
        [Ignore]
        public string CampoNombre { get; set; } = string.Empty;

        private Campo _campo;
        [Ignore]
        [JsonPropertyName("campo")]
        public Campo Campo
        {
            get => _campo;
            set
            {
                _campo = value;
                CampoNombre = value.Nombre;
                IdCampo = value.Id;
            }
        }
    }
}
