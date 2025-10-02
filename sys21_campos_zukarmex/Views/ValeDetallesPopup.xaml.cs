using CommunityToolkit.Maui.Views;
using System.Collections.Generic;
using sys21_campos_zukarmex.Models.DTOs.Api;

namespace sys21_campos_zukarmex.Views;

public partial class ValeDetallesPopup : Popup
{
    // Una propiedad simple para pasar la lista de detalles al BindingContext
    public List<ValeDetalleItemDto> Detalles { get; }

    public ValeDetallesPopup(List<ValeDetalleItemDto> detalles)
    {
        InitializeComponent();
        Detalles = detalles;
        BindingContext = this; // La vista se enlaza a sí misma para acceder a la propiedad "Detalles"
    }

    // Método para cerrar el popup cuando se presiona el botón
    private void OnCloseButtonClicked(object sender, EventArgs e)
    {
        Close();
    }
}