using SQLite;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace sys21_campos_zukarmex.Models
{
    public class Receta
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// ID del campo desde la API
        /// </summary>
        public int IdCampo { get; set; }

        /// <summary>
        /// ID del almacén desde la API
        /// </summary>
        public int IdAlmacen { get; set; }
        
        /// <summary>
        /// Nombre de la receta desde la API
        /// </summary>
        public string NombreReceta { get; set; } = string.Empty;
        
        /// <summary>
        /// Tipo de receta desde la API
        /// </summary>
        public int TipoReceta { get; set; }
        
        /// <summary>
        /// ID único de la receta desde la API (usado para relacionar con RecetaArticulo)
        /// </summary>
        public int IdReceta { get; set; }
        
        // Propiedades de navegación (no se guardan en BD)
        [Ignore]
        public List<RecetaArticulo> Articulos { get; set; } = new();

        [JsonIgnore]
        public string AlmacenNombre { get; set; } = string.Empty;

        [JsonIgnore]
        public string CampoNombre { get; set; } = string.Empty;
        
        // Propiedad calculada para obtener cantidad de artículos
        [Ignore]
        public int ArticulosCount => Articulos?.Count ?? 0;
        
        // Método para agregar un artículo a la receta (usa IdReceta para relacionar)
        public void AgregarArticulo(RecetaArticulo articulo)
        {
            articulo.IdReceta = this.IdReceta; // Usar IdReceta en lugar de Id
            Articulos ??= new List<RecetaArticulo>();
            Articulos.Add(articulo);
        }
        
        // Método para remover un artículo de la receta
        public bool RemoverArticulo(int idArticulo)
        {
            if (Articulos == null) return false;
            var articuloToRemove = Articulos.FirstOrDefault(a => a.IdArticulo == idArticulo);
            if (articuloToRemove != null)
            {
                return Articulos.Remove(articuloToRemove);
            }
            return false;
        }

        // Propiedades obsoletas para compatibilidad temporal
        [Ignore]
        [JsonIgnore]
        [Obsolete("Use IdAlmacen instead")]
        public int Almacen 
        { 
            get => IdAlmacen; 
            set => IdAlmacen = value; 
        }
    }
}