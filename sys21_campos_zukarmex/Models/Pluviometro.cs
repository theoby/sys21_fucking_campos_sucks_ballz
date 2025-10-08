using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace sys21_campos_zukarmex.Models;

[Table("Pluviometro")]
public class Pluviometro
    {
    public int Id { get; set; } = 0;
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaBaja { get; set; }
}

