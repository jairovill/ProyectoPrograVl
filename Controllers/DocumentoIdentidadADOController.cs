using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoPrograVI.Data;
using ProyectoPrograVI.Models;

namespace ProyectoPrograVI.Controllers
{
    [Authorize]
    public class DocumentoIdentidadADOController : Controller
    {
        private readonly DocumentoIdentidadADORepository _repo = new DocumentoIdentidadADORepository();

        public IActionResult Index() => View(_repo.GetAll());

        public IActionResult Create() => View();

        [HttpPost]
        public IActionResult Create(DocumentoIdentidad documentoidentidad)
        {
            if (ModelState.IsValid)
            {
                _repo.Insert(documentoidentidad);
                return RedirectToAction("Index");
            }
            return View(documentoidentidad);
        }

        public IActionResult Edit(int id)
        {
            var documentoidentidad = _repo.GetById(id);
            if (documentoidentidad == null) return NotFound();
            return View(documentoidentidad);
        }

        [HttpPost]
        public IActionResult Edit(DocumentoIdentidad documentoidentidad)
        {
            if (ModelState.IsValid)
            {
                _repo.Update(documentoidentidad);
                return RedirectToAction("Index");
            }
            return View(documentoidentidad);
        }

        // GET: Mostrar vista de confirmación de eliminación
        public IActionResult Delete(int id)
        {
            var documentoidentidad = _repo.GetById(id);
            if (documentoidentidad == null) return NotFound();
            return View(documentoidentidad);
        }

        // POST: Eliminar rol de la base de datos
        [HttpPost]
        public IActionResult DeleteConfirmed(int documento_identidad_id)
        {
            try
            {
                _repo.Delete(documento_identidad_id);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMensaje"] = ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}
