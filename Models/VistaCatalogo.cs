using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace ProyectoPrograVI.Models
{
    public class VistaCatalogo
    {
        public IEnumerable<Libro> Libros { get; set; }
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
        public int TotalRegistros { get; set; }
        public string TerminoBusqueda { get; set; }
        public int? CategoriaId { get; set; }
        public int? AutorId { get; set; }
        public decimal? PrecioMin { get; set; }
        public decimal? PrecioMax { get; set; }

        // Para filtros
        public IEnumerable<SelectListItem> Categorias { get; set; }
        public IEnumerable<SelectListItem> Autores { get; set; }
    }
}