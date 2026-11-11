using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProyectoPrograVI.Data;
using ProyectoPrograVI.Models;
using System;
using System.Linq;
using System.Text;

namespace ProyectoPrograVI.Controllers
{
    public class UsuarioADOController : Controller
    {
        private readonly UsuarioADORepository _usuarioRepository;

        public UsuarioADOController(IConfiguration configuration)
        {
            _usuarioRepository = new UsuarioADORepository(configuration.GetConnectionString("DefaultConnection"));
        }

        public IActionResult Index()
        {
            var usuarios = _usuarioRepository.GetAll();
            return View(usuarios);
        }

        public IActionResult Create()
        {
            var usuario = new Usuario
            {
                Roles = _usuarioRepository.GetRoles(),
                DocumentosIdentidad = _usuarioRepository.GetDocumentosIdentidad(),
                Estados = _usuarioRepository.GetEstados()
            };

            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Usuario usuario)
        {
            // 1. Cargar los dropdowns ANTES de la validación
            usuario.Roles = _usuarioRepository.GetRoles();
            usuario.DocumentosIdentidad = _usuarioRepository.GetDocumentosIdentidad();
            usuario.Estados = _usuarioRepository.GetEstados();

            // 2. Limpiar la validación para propiedades que no son del formulario
            ModelState.Remove("rol_nombre");
            ModelState.Remove("documento_nombre");
            ModelState.Remove("Roles");
            ModelState.Remove("DocumentosIdentidad");
            ModelState.Remove("Estados");

            // 3. Validar solo el modelo
            if (ModelState.IsValid)
            {
                var resultado = _usuarioRepository.Create(usuario);
                if (resultado == 1)
                {
                    TempData["Mensaje"] = "Usuario creado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                else if (resultado == -1)
                {
                    ModelState.AddModelError("email", "El correo electrónico ya está registrado");
                }
                else
                {
                    ModelState.AddModelError("", "Error al crear el usuario");
                }
            }

            // 4. Si hay errores, mantener los dropdowns cargados
            return View(usuario);
        }

        public IActionResult Edit(int id)
        {
            var usuario = _usuarioRepository.GetById(id);
            if (usuario == null)
            {
                return NotFound();
            }

            usuario.Roles = _usuarioRepository.GetRoles();
            usuario.DocumentosIdentidad = _usuarioRepository.GetDocumentosIdentidad();
            usuario.Estados = _usuarioRepository.GetEstados();

            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Usuario usuario)
        {
            if (id != usuario.usuario_id)
            {
                return NotFound();
            }

            // Cargar dropdowns
            usuario.Roles = _usuarioRepository.GetRoles();
            usuario.DocumentosIdentidad = _usuarioRepository.GetDocumentosIdentidad();
            usuario.Estados = _usuarioRepository.GetEstados();

            // Limpiar validación para campos no relevantes
            ModelState.Remove("rol_nombre");
            ModelState.Remove("documento_nombre");
            ModelState.Remove("Roles");
            ModelState.Remove("DocumentosIdentidad");
            ModelState.Remove("Estados");

            if (ModelState.IsValid)
            {
                try
                {
                    // Si la contraseña está vacía, establecer el valor especial
                    if (string.IsNullOrEmpty(usuario.contrasena))
                    {
                        usuario.contrasena = "no-change";
                    }

                    var resultado = _usuarioRepository.Update(usuario);

                    if (resultado == 1)
                    {
                        TempData["Mensaje"] = "Usuario actualizado exitosamente";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError("", "Error al actualizar el usuario. No se realizaron cambios.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error inesperado: {ex.Message}");
                }
            }

            return View(usuario);
        }



        [HttpPost]
        public IActionResult ToggleStatus(int id)
        {
            var usuario = _usuarioRepository.GetById(id);
            if (usuario == null)
            {
                return NotFound();
            }

            usuario.estado = usuario.estado == "activo" ? "inactivo" : "activo";
            _usuarioRepository.Update(usuario);

            TempData["Mensaje"] = "Estado cambiado exitosamente";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult ExportarCSV()
        {
            var usuarios = _usuarioRepository.GetAll();

            var csv = new StringBuilder();

         
            csv.AppendLine("ID;Identificación;Nombre Completo;Email;Rol;Tipo Documento;Estado");

            foreach (var usuario in usuarios)
            {
               
                var nombreCompleto = $"{usuario.nombre} {usuario.apellido1}";
                if (!string.IsNullOrEmpty(usuario.apellido2))
                {
                    nombreCompleto += $" {usuario.apellido2}";
                }

             
                nombreCompleto = nombreCompleto.Replace("\"", "\"\"");
                var identificacion = usuario.identificacion.Replace("\"", "\"\"");
                var email = usuario.email.Replace("\"", "\"\"");
                var rolNombre = usuario.rol_nombre.Replace("\"", "\"\"");
                var docNombre = usuario.documento_nombre.Replace("\"", "\"\"");

                csv.AppendLine(
                    $"{usuario.usuario_id};" +
                    $"\"{identificacion}\";" +
                    $"\"{nombreCompleto}\";" +
                    $"\"{email}\";" +
                    $"\"{rolNombre}\";" +
                    $"\"{docNombre}\";" +
                    $"\"{usuario.estado}\""
                );
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", "usuarios.csv");
        }


    }
}