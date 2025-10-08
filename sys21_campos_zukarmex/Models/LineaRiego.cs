using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sys21_campos_zukarmex.Models
{
    public class LineaRiego
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public int IdCampo { get; set; }
    }
}
