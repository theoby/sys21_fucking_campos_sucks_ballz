using sys21_campos_zukarmex.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace sys21_campos_zukarmex.Services
{
    public class ValeNavigationService
    {
        public SalidaDetalle? NuevoDetalle { get; private set; }

        public List<SalidaDetalle> DetallesActuales { get; private set; } = new();


        public void ColocarNuevoDetalle(SalidaDetalle detalle)
        {
            NuevoDetalle = detalle;
        }


        public SalidaDetalle? RecogerNuevoDetalle()
        {
            var detalle = NuevoDetalle;
            NuevoDetalle = null; // Limpiar el buzón
            return detalle;
        }

        public void ColocarDetallesActuales(ObservableCollection<SalidaDetalle> detalles)
        {
            DetallesActuales = new List<SalidaDetalle>(detalles);
        }
        public List<SalidaDetalle> RecogerDetallesActuales()
        {
            var detalles = DetallesActuales;
            DetallesActuales = new List<SalidaDetalle>(); // Limpiar el "buzón" para la próxima vez
            return detalles;
        }

    }
}
// Y en tu ServiceRegistration.cs o MauiProgram.cs
// services.AddSingleton<ValeNavigationService>();