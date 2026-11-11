using Microsoft.AspNetCore.Mvc;
using ProyectoPrograVI.Data;
using ProyectoPrograVI.Models;
using System.Text;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace ProyectoPrograVI.Controllers
{
    [Authorize]
    public class CategoriaADOController : Controller
    {
        private readonly CategoriaADORepository _repo = new CategoriaADORepository();

        public IActionResult Index() => View(_repo.GetAll());

        public IActionResult Create() => View();

        [HttpPost]
        public IActionResult Create(Categoria categoria)
        {
            if (ModelState.IsValid)
            {
                _repo.Insert(categoria);
                return RedirectToAction("Index");
            }
            return View(categoria);
        }

        public IActionResult Edit(int id)
        {
            var cat = _repo.GetById(id);
            if (cat == null) return NotFound();
            return View(cat);
        }

        [HttpPost]
        public IActionResult Edit(Categoria categoria)
        {
            if (ModelState.IsValid)
            {
                _repo.Update(categoria);
                return RedirectToAction("Index");
            }
            return View(categoria);
        }

        public IActionResult Delete(int id)
        {
            var cat = _repo.GetById(id);
            if (cat == null) return NotFound();
            return View(cat);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            _repo.Delete(id);
            return RedirectToAction("Index");
        }

        // Método para exportar a CSV con punto y coma como delimitador
        public IActionResult ExportarCSV()
        {
            var categorias = _repo.GetAll();

            var csv = new StringBuilder();

            // Encabezados con punto y coma
            csv.AppendLine("ID Categoría;Nombre");

            foreach (var categoria in categorias)
            {
                csv.AppendLine(
                    $"{categoria.categoria_id};" +
                    $"\"{categoria.nombre.Replace("\"", "\"\"")}\""
                );
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", "categorias.csv");
        }

        // Método opcional con filtro
        public IActionResult ExportarCSVFiltrado(string filtroNombre = "")
        {
            var categorias = _repo.GetAll();

            if (!string.IsNullOrEmpty(filtroNombre))
            {
                categorias = categorias.Where(c =>
                    c.nombre.Contains(filtroNombre, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var csv = new StringBuilder();
            csv.AppendLine("ID Categoría;Nombre");

            foreach (var categoria in categorias)
            {
                csv.AppendLine(
                    $"{categoria.categoria_id};" +
                    $"\"{categoria.nombre.Replace("\"", "\"\"")}\""
                );
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"categorias_filtradas.csv");
        }
    }
}