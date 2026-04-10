using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
namespace sys21_campos_zukarmex.Models;

public class LineaDeRiego
{
    [PrimaryKey]
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int CantidadEquiposBombeo { get; set; } = 0;
    public decimal CantidadLaminaRiego { get; set; } = 0;
    public int IdCampo { get; set; } 
}