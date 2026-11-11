using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ProyectoPrograVI.Models
{
    public class Libro
    {
        public int LibroId { get; set; }

        [Required(ErrorMessage = "El título es requerido")]
        [StringLength(100, ErrorMessage = "El título no puede exceder 100 caracteres")]
        public string Titulo { get; set; }

        [Required(ErrorMessage = "La categoría es requerida")]
        public int CategoriaId { get; set; }
        public string CategoriaNombre { get; set; }

        [Required(ErrorMessage = "El autor es requerido")]
        public int AutorId { get; set; }
        public string AutorNombre { get; set; }

        [Required(ErrorMessage = "Las unidades disponibles son requeridas")]
        [Range(0, int.MaxValue, ErrorMessage = "Las unidades no pueden ser negativas")]
        public int UnidadesDisponibles { get; set; }

        [Required(ErrorMessage = "El precio es requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor que cero")]
        public decimal Precio { get; set; }

        public string Descripcion { get; set; }

        [Url(ErrorMessage = "Debe ser una URL válida")]
        public string ImagenUrl { get; set; }

        [StringLength(20, ErrorMessage = "El ISBN no puede exceder 20 caracteres")]
        public string ISBN { get; set; }

        [StringLength(100, ErrorMessage = "La editorial no puede exceder 100 caracteres")]
        public string Editorial { get; set; }

        [Required(ErrorMessage = "El estado es requerido")]
        public string Estado { get; set; } = "activo";

        // Para dropdowns
        public IEnumerable<SelectListItem> Categorias { get; set; }
        public IEnumerable<SelectListItem> Autores { get; set; }
    }
}