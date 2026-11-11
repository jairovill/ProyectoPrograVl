using Microsoft.AspNetCore.Mvc;
using ProyectoPrograVI.Data;
using ProyectoPrograVI.Models;
using System.Text;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace ProyectoPrograVI.Controllers
{
    [Authorize]
    public class AutorADOController : Controller
    {
        private readonly AutorADORepository _repo = new AutorADORepository();

        public IActionResult Index() => View(_repo.GetAll());

        public IActionResult Create() => View();

        [HttpPost]
        public IActionResult Create(Autor autor)
        {
            if (ModelState.IsValid)
            {
                _repo.Insert(autor);
                return RedirectToAction("Index");
            }
            return View(autor);
        }

        public IActionResult Edit(int id)
        {
            var est = _repo.GetById(id);
            if (est == null) return NotFound();
            return View(est);
        }

        [HttpPost]
        public IActionResult Edit(Autor autor)
        {
            if (ModelState.IsValid)
            {
                _repo.Update(autor);
                return RedirectToAction("Index");
            }
            return View(autor);
        }

        public IActionResult Delete(int id)
        {
            var est = _repo.GetById(id);
            if (est == null) return NotFound();
            return View(est);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            _repo.Delete(id);
            return RedirectToAction("Index");
        }

        // Nuevo método para exportar a CSV
        public IActionResult ExportarCSV()
        {
            var autores = _repo.GetAll();

            var csv = new StringBuilder();

            // Encabezados con punto y coma
            csv.AppendLine("ID Autor;ID Persona;Identificación;Nombre;Primer Apellido;Segundo Apellido;Fecha Nacimiento;Fecha Defunción");

            foreach (var autor in autores)
            {
                string fechaNacimiento = autor.fecha_nacimiento.HasValue
                    ? autor.fecha_nacimiento.Value.ToString("yyyy-MM-dd")
                    : "N/A";

                string fechaDefuncion = autor.fecha_defuncion.HasValue
                    ? autor.fecha_defuncion.Value.ToString("yyyy-MM-dd")
                    : "N/A";

                csv.AppendLine(
                    $"{autor.autor_id};" +
                    $"{autor.persona_id};" +
                    $"{autor.identificacion};" +
                    $"{autor.nombre};" +
                    $"{autor.apellido1};" +
                    $"{autor.apellido2};" +
                    $"{fechaNacimiento};" +
                    $"{fechaDefuncion}"
                );
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", "autores.csv");
        }

        // Método auxiliar para escapar valores en CSV
        private string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            // Si el valor contiene comas o comillas, lo encerramos en comillas y escapamos comillas existentes
            if (value.Contains(",") || value.Contains("\""))
            {
                return value.Replace("\"", "\"\"");
            }

            return value;
        }

        // Versión alternativa con filtros usando LINQ (opcional)
        public IActionResult ExportarCSVFiltrado(string letraInicial = "")
        {
            var autores = _repo.GetAll();

            // Aplicar filtro si se especificó una letra inicial
            if (!string.IsNullOrEmpty(letraInicial))
            {
                autores = autores.Where(a =>
                    !string.IsNullOrEmpty(a.nombre) &&
                    a.nombre.StartsWith(letraInicial, System.StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Resto del código igual que en ExportarCSV()
            var csv = new StringBuilder();
            csv.AppendLine("ID Autor,ID Persona,Identificación,Nombre,Primer Apellido,Segundo Apellido,Fecha Nacimiento,Fecha Defunción");

            foreach (var autor in autores)
            {
                string fechaNacimiento = autor.fecha_nacimiento.HasValue
                    ? autor.fecha_nacimiento.Value.ToString("yyyy-MM-dd")
                    : "N/A";

                string fechaDefuncion = autor.fecha_defuncion.HasValue
                    ? autor.fecha_defuncion.Value.ToString("yyyy-MM-dd")
                    : "N/A";

                csv.AppendLine($"\"{autor.autor_id}\"," +
                              $"\"{autor.persona_id}\"," +
                              $"\"{EscapeCsvValue(autor.identificacion)}\"," +
                              $"\"{EscapeCsvValue(autor.nombre)}\"," +
                              $"\"{EscapeCsvValue(autor.apellido1)}\"," +
                              $"\"{EscapeCsvValue(autor.apellido2)}\"," +
                              $"\"{fechaNacimiento}\"," +
                              $"\"{fechaDefuncion}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"autores_filtrados_{letraInicial}.csv");
        }
    }
}