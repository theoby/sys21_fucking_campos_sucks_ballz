using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sys21_campos_zukarmex.Models
{
    public class SalidaRodenticida
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int IdTemporada { get; set; } = 0;
        public int IdCampo { get; set; } = 0;
        public DateTime Fecha { get; set; } = DateTime.Now;
        public int CantidadComederos { get; set; } = 0;
        public int CantidadPastillas { get; set; } = 0;
        public int CantidadConsumos { get; set; } = 0;
        public string Lng { get; set; } = string.Empty;
        public string Lat { get; set; } = string.Empty;
        public string Dispositivo { get; set; } = string.Empty;

        [Ignore]
        public string ZafraNombre { get; set; } = "N/D";

        [Ignore]
        public string CampoNombre { get; set; } = "N/D";
    }
}
