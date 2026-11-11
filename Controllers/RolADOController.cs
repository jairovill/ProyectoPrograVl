using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProyectoPrograVI.Data;
using ProyectoPrograVI.Models;
using System.Data;

namespace ProyectoPrograVI.Controllers
{

    [Authorize]
    public class RolADOController : Controller
    {
        private readonly RolADORepository _repo = new RolADORepository();

        public IActionResult Index() => View(_repo.GetAll());

        public IActionResult Create() => View();

        [HttpPost]
        public IActionResult Create(Rol rol)
        {
            if (ModelState.IsValid)
            {
                _repo.Insert(rol);
                return RedirectToAction("Index");
            }
            return View(rol);
        }

        public IActionResult Edit(int id)
        {
            var rol = _repo.GetById(id);
            if (rol == null) return NotFound();
            return View(rol);
        }

        [HttpPost]
        public IActionResult Edit(Rol rol)
        {
            if (ModelState.IsValid)
            {
                _repo.Update(rol);
                return RedirectToAction("Index");
            }
            return View(rol);
        }

        // GET: Mostrar vista de confirmación de eliminación
        public IActionResult Delete(int id)
        {
            var rol = _repo.GetById(id);
            if (rol == null) return NotFound();
            return View(rol);
        }

        // POST: Eliminar rol de la base de datos
        [HttpPost]
        public IActionResult DeleteConfirmed(int rol_id)
        {
            _repo.Delete(rol_id);
            return RedirectToAction("Index");
        }

    }
}
