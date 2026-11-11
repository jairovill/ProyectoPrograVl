using System.ComponentModel.DataAnnotations;

namespace ProyectoPrograVI.Models
{
    public class PagoTarjetaViewModel
    {
        public int PedidoId { get; set; }

        [Required(ErrorMessage = "El método de pago es requerido")]
        public string MetodoPago { get; set; }

        [Required(ErrorMessage = "El nombre del titular es requerido")]
        [Display(Name = "Nombre del Titular")]
        public string NombreTitular { get; set; }

        [Required(ErrorMessage = "El número de tarjeta es requerido")]
        [CreditCard(ErrorMessage = "Número de tarjeta no válido")]
        [Display(Name = "Número de Tarjeta")]
        public string NumeroTarjeta { get; set; }

        [Required(ErrorMessage = "El CVV es requerido")]
        [StringLength(4, MinimumLength = 3, ErrorMessage = "CVV debe tener 3 o 4 dígitos")]
        [RegularExpression(@"^\d+$", ErrorMessage = "CVV debe contener solo números")]
        [Display(Name = "Código de Seguridad (CVV)")]
        public string ICVV { get; set; }

        [Required(ErrorMessage = "La fecha de vencimiento es requerida")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/?([0-9]{2})$", ErrorMessage = "Formato debe ser MM/AA")]
        [Display(Name = "Fecha de Vencimiento (MM/AA)")]
        public string FechaVencimiento { get; set; }
    }
}