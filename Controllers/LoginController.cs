using Microsoft.AspNetCore.Mvc;
using ProyectoPrograVI.Models;
using Microsoft.AspNetCore.Http;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ProyectoPrograVI.Data;

namespace ProyectoPrograVI.Controllers
{
    [Route("[controller]/[action]")]
    public class LoginController : Controller
    {
        private readonly UsuarioADORepository _usuarioRepository;
        private readonly ILogger<LoginController> _logger;
        private readonly string _connectionString;

        public LoginController(IConfiguration configuration, ILogger<LoginController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _usuarioRepository = new UsuarioADORepository(_connectionString);
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated && string.IsNullOrEmpty(returnUrl))
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public IActionResult Index(LoginViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            int respuesta;
            var usuario = ValidarLogin(model.Email, model.Password, out respuesta);

            if (respuesta == 1 && usuario != null)
            {
                ConfigurarSesionUsuario(usuario, model.RememberMe);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.usuario_id.ToString()),
                    new Claim(ClaimTypes.Email, usuario.email),
                    new Claim("rol_id", usuario.rol_id.ToString()),
                    new Claim(ClaimTypes.Role, usuario.rol_nombre)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = model.RememberMe ? DateTime.UtcNow.AddDays(30) : null
                    }).Wait();

                return RedirectToLocal(returnUrl);
            }

            ModelState.AddModelError(string.Empty, GetMensajeError(respuesta));
            return View(model);
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Login");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Registrar()
        {
            var model = new Usuario
            {
                DocumentosIdentidad = _usuarioRepository.GetDocumentosIdentidad(),
                estado = "activo" // Valor por defecto
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public IActionResult Registrar(Usuario model)
        {
            try
            {
                // Cargar dropdowns antes de validar
                model.DocumentosIdentidad = _usuarioRepository.GetDocumentosIdentidad();

                // Limpiar validación para propiedades que no son del formulario
                ModelState.Remove("rol_nombre");
                ModelState.Remove("documento_nombre");
                ModelState.Remove("Roles");
                ModelState.Remove("Estados");
                ModelState.Remove("DocumentosIdentidad");

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMensaje"] = "Por favor corrija los errores en el formulario.";
                    return View(model);
                }

                // Asignar rol de cliente (5) por defecto
                model.rol_id = 5;
                model.estado = "activo";

                // Llamar al método Create del repositorio
                var resultado = _usuarioRepository.Create(model);

                if (resultado == 1)
                {
                    TempData["MensajeExito"] = "¡Registro exitoso! Por favor inicie sesión.";
                    return RedirectToAction("Index");
                }
                else if (resultado == -1)
                {
                    TempData["ErrorMensaje"] = "El correo electrónico ya está registrado.";
                }
                else
                {
                    TempData["ErrorMensaje"] = "Ocurrió un error al registrar. Por favor intente nuevamente.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar usuario");
                TempData["ErrorMensaje"] = "Ocurrió un error inesperado. Por favor intente más tarde.";
            }

            // Si llegamos aquí, algo salió mal
            model.DocumentosIdentidad = _usuarioRepository.GetDocumentosIdentidad();
            return View(model);
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        private Usuario ValidarLogin(string email, string contrasena, out int respuesta)
        {
            return _usuarioRepository.ValidarLogin(email, contrasena, out respuesta);
        }

        private void ConfigurarSesionUsuario(Usuario usuario, bool rememberMe)
        {
            HttpContext.Session.SetInt32("usuario_id", usuario.usuario_id);
            HttpContext.Session.SetInt32("rol_id", usuario.rol_id);
            HttpContext.Session.SetString("rol_nombre", usuario.rol_nombre);
            HttpContext.Session.SetString("nombre_completo", $"{usuario.nombre} {usuario.apellido1} {usuario.apellido2}");
            HttpContext.Session.SetString("email", usuario.email);
            HttpContext.Session.SetString("estado", usuario.estado);

            if (rememberMe)
            {
                HttpContext.Session.SetInt32("remember_me", 1);
            }
        }

        private string GetMensajeError(int codigoError)
        {
            return codigoError switch
            {
                -1 => "Credenciales incorrectas",
                -3 => "Usuario inactivo. Contacte al administrador",
                _ => "Error al iniciar sesión"
            };
        }
    }
}