using System;
using System.Collections.Generic;

namespace sys21_campos_zukarmex.Models.DTOs.Api
{
    /// <summary>
    /// DTO para la respuesta de la API de historial de vales
    /// </summary>
    public class HistorialValesResponse
    {
        public int Estado { get; set; }
        public List<HistorialValeItem> Datos { get; set; } = new List<HistorialValeItem>();
        public int TotalDatos { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        
        public bool Success => Estado == 200;
    }

    /// <summary>
    /// Elemento individual del historial de vales
    /// </summary>
    public class HistorialValeItem
    {
        public int Id { get; set; }
        public string Predio { get; set; } = string.Empty;
        public string Almacen { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public bool Estatus { get; set; }
        public bool Autorizado { get; set; }
        
        /// <summary>
        /// Texto descriptivo del estatus para mostrar en la UI
        /// Si estatus es true, es que esta "activo", si es false es que esta "cancelado"
        /// </summary>
        public string EstatusTexto => Estatus ? "Activo" : "Cancelado";
        
        /// <summary>
        /// Texto descriptivo de autorizaci�n para mostrar en la UI
        /// Si Autorizado es false, es "pendiente por autorizar", si es true, es "Autorizado"
        /// </summary>
        public string AutorizadoTexto => Autorizado ? "Autorizado" : "Pendiente por autorizar";
        
        /// <summary>
        /// Color para el estatus basado en el estado
        /// </summary>
        public string EstatusColor => Estatus ? "Green" : "Orange";
        
        /// <summary>
        /// Color para la autorizaci�n basado en el estado
        /// </summary>
        public string AutorizadoColor => Autorizado ? "Green" : "Red";
        
        /// <summary>
        /// Fecha formateada para mostrar en la UI
        /// </summary>
        public string FechaFormateada => Fecha.ToString("dd/MM/yyyy HH:mm");
    }
}