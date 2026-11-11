namespace ProyectoPrograVI.Models
{
    public class BitacoraMovimiento
    {
        public int Id { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public string TipoOperacion { get; set; }
        public string NombreTabla { get; set; }
        public int IdRegistroAfectado { get; set; }
        public string Detalle { get; set; }
    }
}