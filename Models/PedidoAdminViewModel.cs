namespace ProyectoPrograVI.Models
{
    public class PedidoAdminViewModel
    {
        public int PedidoId { get; set; }
        public int UsuarioId { get; set; }
        public string Cliente { get; set; }
        public DateTime FechaPedido { get; set; }
        public DateTime? FechaActualizacion { get; set; }
        public string Estado { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Impuestos { get; set; }
        public decimal Total { get; set; }
        public string DireccionEnvio { get; set; }
        public string MetodoPago { get; set; }
        public string NumeroSeguimiento { get; set; }
        public string EstadoPago { get; set; }
        public string TransaccionId { get; set; }
    }
}