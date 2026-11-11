using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ProyectoPrograVI.Models
{
    public class Usuario
    {
        // Propiedades principales (se guardan en BD)
        public int usuario_id { get; set; }

        [Required(ErrorMessage = "El rol es requerido")]
        public int rol_id { get; set; }

        public string rol_nombre { get; set; } // Solo para visualización

        public int persona_id { get; set; }

        [Required(ErrorMessage = "El tipo de documento es requerido")]
        public int documento_identidad_id { get; set; }

        public string documento_nombre { get; set; } // Solo para visualización

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "El email no tiene un formato válido")]
        public string email { get; set; }

        [Required(ErrorMessage = "La contraseña es requerida")]
        public string contrasena { get; set; }

        public string estado { get; set; } = "activo"; // Valor por defecto

        // Datos de persona
        [Required(ErrorMessage = "La identificación es requerida")]
        public string identificacion { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        public string nombre { get; set; }

        [Required(ErrorMessage = "El primer apellido es requerido")]
        public string apellido1 { get; set; }

        public string apellido2 { get; set; }

        public DateTime? fecha_nacimiento { get; set; }

        // Propiedades para los dropdowns (NO son parte del modelo de datos)
        [BindNever] // Esto evita que se validen o binding
        public IEnumerable<SelectListItem> Roles { get; set; }

        [BindNever]
        public IEnumerable<SelectListItem> DocumentosIdentidad { get; set; }

        [BindNever]
        public IEnumerable<SelectListItem> Estados { get; set; }


    }
}