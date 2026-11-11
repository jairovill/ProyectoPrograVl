using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProyectoPrograVI.Data;
using ProyectoPrograVI.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace ProyectoPrograVI.Controllers
{
    public class LibroADOController : Controller
    {
        private readonly LibroADORepository _libroRepository;
        private readonly CarritoADORepository _carritoRepository;

        public LibroADOController(IConfiguration configuration)
        {
            _libroRepository = new LibroADORepository(configuration.GetConnectionString("DefaultConnection"));
            _carritoRepository = new CarritoADORepository(configuration.GetConnectionString("DefaultConnection"));
        }


        public IActionResult Index(string search, int? categoriaId, int? autorId,
            decimal? precioMin, decimal? precioMax, int page = 1)
        {
            const int pageSize = 10;
            var libros = _libroRepository.BuscarLibros(search, categoriaId, autorId, precioMin, precioMax, page, pageSize);

            var model = new VistaCatalogo
            {
                Libros = libros,
                PaginaActual = page,
                TerminoBusqueda = search,
                CategoriaId = categoriaId,
                AutorId = autorId,
                PrecioMin = precioMin,
                PrecioMax = precioMax,
                Categorias = _libroRepository.ObtenerCategorias(),
                Autores = _libroRepository.ObtenerAutores()
            };

            return View(model);
        }


        public IActionResult Details(int id)
        {
            var libro = _libroRepository.ObtenerPorId(id, true);

            if (libro == null)
            {
                return NotFound();
            }


            if (libro.Estado == "inactivo")
            {
                TempData["MensajeError"] = "Este libro se encuentra actualmente inactivo y no está disponible para visualización.";
                return RedirectToAction(nameof(Index));
            }

            return View(libro);
        }


        public IActionResult Create()
        {
            var libro = new Libro
            {
                Estado = "activo",
                Categorias = _libroRepository.ObtenerCategorias(),
                Autores = _libroRepository.ObtenerAutores()
            };

            return View(libro);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Libro libro)
        {

            libro.Categorias = _libroRepository.ObtenerCategorias();
            libro.Autores = _libroRepository.ObtenerAutores();


            ModelState.Remove("CategoriaNombre");
            ModelState.Remove("AutorNombre");
            ModelState.Remove("Categorias");
            ModelState.Remove("Autores");

            if (ModelState.IsValid)
            {

                var resultado = _libroRepository.Insertar(libro, out string mensaje);

                if (resultado > 0)
                {
                    TempData["Mensaje"] = "Libro creado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", mensaje);
                }
            }

            return View(libro);
        }

        // GET: LibroADO/Edit/5
        public IActionResult Edit(int id)
        {
            var libro = _libroRepository.ObtenerPorId(id, true); // Incluir inactivos
            if (libro == null)
            {
                return NotFound();
            }

            libro.Categorias = _libroRepository.ObtenerCategorias();
            libro.Autores = _libroRepository.ObtenerAutores();

            return View(libro);
        }

        // POST: LibroADO/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Libro libro)
        {
            if (id != libro.LibroId)
            {
                return NotFound();
            }

            // Cargar dropdowns
            libro.Categorias = _libroRepository.ObtenerCategorias();
            libro.Autores = _libroRepository.ObtenerAutores();

            // Limpiar validación para propiedades de soporte
            ModelState.Remove("CategoriaNombre");
            ModelState.Remove("AutorNombre");
            ModelState.Remove("Categorias");
            ModelState.Remove("Autores");

            if (ModelState.IsValid)
            {
                var resultado = _libroRepository.Actualizar(libro, out string mensaje);

                if (resultado)
                {
                    TempData["Mensaje"] = "Libro actualizado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", mensaje);
                }
            }

            return View(libro);
        }


        [HttpPost]
        public IActionResult ToggleStatus(int id)
        {
            var libro = _libroRepository.ObtenerPorId(id, true);
            if (libro == null)
            {
                return NotFound();
            }

            libro.Estado = libro.Estado == "activo" ? "inactivo" : "activo";


            var resultado = _libroRepository.ActualizarEstado(id, libro.Estado, out string mensaje);

            if (resultado)
            {
                TempData["Mensaje"] = $"Libro marcado como {libro.Estado} correctamente";
            }
            else
            {
                TempData["Error"] = mensaje;
            }

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int libroId, int cantidad = 1)
        {
            int usuarioId = ObtenerUsuarioId();

            if (usuarioId == 0)
            {
                TempData["Error"] = "Debe iniciar sesión para agregar al carrito.";
                return RedirectToAction("Index", "Login");
            }

            bool resultado = _carritoRepository.AgregarItem(usuarioId, libroId, cantidad, out string mensaje);

            if (resultado)
            {
                TempData["Mensaje"] = mensaje;
            }
            else
            {
                TempData["Error"] = mensaje;
            }

            return RedirectToAction(nameof(Details), new { id = libroId });
        }

        private int ObtenerUsuarioId()
        {
            if (User.Identity.IsAuthenticated)
            {
                var claim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (claim != null && int.TryParse(claim.Value, out int usuarioId))
                {
                    return usuarioId;
                }
            }
            return 0;
        }


        public IActionResult ExportarCSV()
        {
            var libros = _libroRepository.ObtenerTodosParaExportar();

            var csv = new StringBuilder();


            csv.AppendLine("ID;Título;Autor;Categoría;Precio;Unidades;ISBN;Editorial;Estado;Descripción");

            foreach (var libro in libros)
            {
                csv.AppendLine(
                    $"{libro.LibroId};" +
                    $"\"{libro.Titulo.Replace("\"", "\"\"")}\";" +
                    $"\"{libro.AutorNombre.Replace("\"", "\"\"")}\";" +
                    $"\"{libro.CategoriaNombre.Replace("\"", "\"\"")}\";" +
                    $"{libro.Precio.ToString("0.00").Replace(",", ".")};" +
                    $"{libro.UnidadesDisponibles};" +
                    $"\"{libro.ISBN?.Replace("\"", "\"\"")}\";" +
                    $"\"{libro.Editorial?.Replace("\"", "\"\"")}\";" +
                    $"\"{libro.Estado}\";" +
                    $"\"{libro.Descripcion?.Replace("\"", "\"\"").Replace("\r\n", " ").Replace("\n", " ")}\""
                );
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", "libros.csv");
        }


        public IActionResult ExportarCSVFiltrado(string search, int? categoriaId, int? autorId,
            decimal? precioMin, decimal? precioMax)
        {
            var libros = _libroRepository.BuscarLibros(search, categoriaId, autorId, precioMin, precioMax, 1, int.MaxValue);

            var csv = new StringBuilder();
            csv.AppendLine("ID;Título;Autor;Categoría;Precio;Unidades;ISBN;Editorial;Estado;Descripción");

            foreach (var libro in libros)
            {
                csv.AppendLine(
                    $"{libro.LibroId};" +
                    $"\"{libro.Titulo.Replace("\"", "\"\"")}\";" +
                    $"\"{libro.AutorNombre.Replace("\"", "\"\"")}\";" +
                    $"\"{libro.CategoriaNombre.Replace("\"", "\"\"")}\";" +
                    $"{libro.Precio.ToString("0.00").Replace(",", ".")};" +
                    $"{libro.UnidadesDisponibles};" +
                    $"\"{libro.ISBN?.Replace("\"", "\"\"")}\";" +
                    $"\"{libro.Editorial?.Replace("\"", "\"\"")}\";" +
                    $"\"{libro.Estado}\";" +
                    $"\"{libro.Descripcion?.Replace("\"", "\"\"").Replace("\r\n", " ").Replace("\n", " ")}\""
                );
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"libros_filtrados.csv");
        }
    }
}

