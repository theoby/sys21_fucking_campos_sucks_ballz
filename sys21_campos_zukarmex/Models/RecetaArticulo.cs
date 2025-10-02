using SQLite;

namespace sys21_campos_zukarmex.Models
{
    public class RecetaArticulo
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        public int IdReceta { get; set; }
        
        public int IdArticulo { get; set; }
        
        public decimal Dosis { get; set; }
        
        public decimal Total { get; set; }
        
        public int IdSubFamilia { get; set; }
        
        public int IdFamilia { get; set; }
        
        // Propiedades de navegación (nombres - no se guardan en BD)
        [Ignore]
        public string ArticuloNombre { get; set; } = string.Empty;
        
        [Ignore] 
        public string SubFamiliaNombre { get; set; } = string.Empty;
        
        [Ignore]
        public string FamiliaNombre { get; set; } = string.Empty;
        
        [Ignore]
        public string Unidad { get; set; } = string.Empty;
    }
}