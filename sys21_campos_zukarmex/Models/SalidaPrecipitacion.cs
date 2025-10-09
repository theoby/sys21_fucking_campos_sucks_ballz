using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace sys21_campos_zukarmex.Models;

 public class SalidaPrecipitacion
    {
    public int Id { get; set; }
    public int IdEmpresa { get; set; }
    public int IdPluviometro { get; set; } = 0;
    public DateTime Fecha { get; set; }
    public decimal Preciptacion { get; set; } = 0;
    public string Lat { get; set; } = "0";
    public string Lng { get; set; } = "0";
    public string Dispositivo { get; set; } = string.Empty;

}

