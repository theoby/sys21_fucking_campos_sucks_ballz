using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace sys21_campos_zukarmex.Models;

public class LineaDeRiego
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int CantidadEquiposBombeo { get; set; } = 0;
    public int CantidadLaminaRiego { get; set; } = 0;

}
