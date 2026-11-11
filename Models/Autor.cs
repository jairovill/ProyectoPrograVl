namespace ProyectoPrograVI.Models
{
    public class Autor
    {

        public int autor_id { get; set; }
        public int persona_id { get; set; }
        public DateTime? fecha_defuncion { get; set; }

        // Datos relacionados de la persona
        public string nombre { get; set; }
        public string identificacion { get; set; }
        public string apellido1 { get; set; }
        public string apellido2 { get; set; }
        public DateTime? fecha_nacimiento { get; set; }

    }
}
