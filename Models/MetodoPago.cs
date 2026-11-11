using System;

namespace ProyectoPrograVI.Models
{
    public class MetodoPago
    {
        public int MetodoPagoId { get; set; }

        public string Nombre { get; set; }

        public bool Activo { get; set; }

        // Enums para los tipos de método de pago permitidos
        public enum TiposMetodoPago
        {
            transferencia,
            PayPal,
            tarjeta
        }
    }
}