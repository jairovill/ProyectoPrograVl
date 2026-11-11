using Microsoft.AspNetCore.Mvc;
using ProyectoPrograVI.Models;
using ProyectoPrograVI.Data;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace ProyectoPrograVI.Controllers
{

    [Authorize]
    public class MetodoPagoADOController : Controller
    {
        private readonly MetodoPagoADORepository _repository;

        public MetodoPagoADOController(MetodoPagoADORepository repository)
        {
            _repository = repository;
        }

        public IActionResult Index(bool soloActivos = false) // Cambiado a false por defecto
        {
            try
            {
                var metodos = _repository.GetAll(soloActivos);
                return View(metodos);
            }
            catch (Exception ex)
            {
                TempData["ErrorMensaje"] = $"Error al obtener métodos de pago: {ex.Message}";
                return View(new List<MetodoPago>());
            }
        }

        public IActionResult Create()
        {
            ViewBag.TiposMetodoPago = Enum.GetValues(typeof(MetodoPago.TiposMetodoPago));
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(MetodoPago metodoPago)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var (success, id, message) = _repository.Crear(metodoPago);
                    if (success)
                    {
                        TempData["SuccessMensaje"] = message;
                        return RedirectToAction("Index");
                    }
                    TempData["ErrorMensaje"] = message;
                }
                catch (Exception ex)
                {
                    TempData["ErrorMensaje"] = $"Error al crear método de pago: {ex.Message}";
                }
            }

            ViewBag.TiposMetodoPago = Enum.GetValues(typeof(MetodoPago.TiposMetodoPago));
            return View(metodoPago);
        }

        public IActionResult Edit(int id)
        {
            try
            {
                var metodoPago = _repository.GetById(id);
                if (metodoPago == null)
                {
                    TempData["ErrorMensaje"] = "Método de pago no encontrado";
                    return RedirectToAction("Index");
                }

                ViewBag.TiposMetodoPago = Enum.GetValues(typeof(MetodoPago.TiposMetodoPago));
                return View(metodoPago);
            }
            catch (Exception ex)
            {
                TempData["ErrorMensaje"] = $"Error al cargar método de pago: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, MetodoPago metodoPago)
        {
            if (id != metodoPago.MetodoPagoId)
            {
                TempData["ErrorMensaje"] = "ID del método de pago no coincide";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var (success, message) = _repository.ActualizarEstado(id, metodoPago.Activo);
                    if (success)
                    {
                        TempData["SuccessMensaje"] = message;
                        return RedirectToAction("Index");
                    }
                    TempData["ErrorMensaje"] = message;
                }
                catch (Exception ex)
                {
                    TempData["ErrorMensaje"] = $"Error al actualizar método de pago: {ex.Message}";
                }
            }

            ViewBag.TiposMetodoPago = Enum.GetValues(typeof(MetodoPago.TiposMetodoPago));
            return View(metodoPago);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            try
            {
                var metodoPago = _repository.GetById(id);
                if (metodoPago == null)
                {
                    TempData["ErrorMessage"] = "Método de pago no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                return View(metodoPago);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el método de pago: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                var metodoPago = _repository.GetById(id);
                if (metodoPago == null)
                {
                    TempData["ErrorMessage"] = "Método de pago no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                _repository.Eliminar(id);
                TempData["SuccessMessage"] = $"Método de pago {metodoPago.Nombre} eliminado correctamente";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al eliminar el método de pago: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}