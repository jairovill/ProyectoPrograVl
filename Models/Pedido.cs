using System.ComponentModel.DataAnnotations;

namespace ProyectoPrograVI.Models
{
    public class Pedido
    {
        public int PedidoId { get; set; }
        public int UsuarioId { get; set; }
        public DateTime FechaPedido { get; set; }
        public DateTime? FechaActualizacion { get; set; }
        public string Estado { get; set; } = "pendiente"; // pendiente, procesando, enviado, entregado, cancelado
        public decimal Subtotal { get; set; }
        public decimal Impuestos { get; set; }
        public decimal Total { get; set; }
        public string DireccionEnvio { get; set; }
        public int MetodoPagoId { get; set; }
        public string NumeroSeguimiento { get; set; }
        public string EstadoPago { get; set; } = "pendiente"; // pendiente, completado, fallido, reembolsado
        public string TransaccionId { get; set; }
        public string DetallesPago { get; set; }
        public List<DetallePedido> Detalles { get; set; } = new List<DetallePedido>();
        public MetodoPago MetodoPago { get; set; }
        public Usuario Usuario { get; set; } = new Usuario();
    }
}
