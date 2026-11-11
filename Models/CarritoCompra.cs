using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.ComponentModel.DataAnnotations;

namespace ProyectoPrograVI.Models
{
    public class CarritoCompra
    {
        public int CarritoId { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        [Required]
        public int LibroId { get; set; }
        public string TituloLibro { get; set; }
        public string ImagenUrl { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser al menos 1")]
        public int Cantidad { get; set; } = 1;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor que cero")]
        public decimal PrecioUnitario { get; set; }

        public DateTime FechaAgregado { get; set; } = DateTime.Now;

        [Required]
        public string Estado { get; set; } = "Pendiente Pago"; // Cambiado a los valores requeridos

        // Para dropdown de estados
        public IEnumerable<SelectListItem> Estados { get; set; }
    }
}