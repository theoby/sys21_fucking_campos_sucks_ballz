using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;


namespace sys21_campos_zukarmex.Models;

internal class SalidaLineaDeRiego
{
    public int Id { get; set; }
    public int IdCampo { get; set; } = 0;
    public int IdLineaRiego { get; set; } = 0;
    public DateTime Fecha { get; set; } = DateTime.Now;
    public int EquiposBombeoOperando { get; set; } = 0;
    public string Observaciones { get; set; } = string.Empty;
    public string Lng { get; set; } = string.Empty;
    public string Lat { get; set; } = string.Empty;
    public string Dispositivo { get; set; } = string.Empty;

}
