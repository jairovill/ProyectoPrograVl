using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProyectoPrograVI.Models
{
    public class EditarPedidoViewModel
    {
        public int PedidoId { get; set; }

        [Display(Name = "Número de Pedido")]
        public string NumeroPedido { get; set; }

        [Display(Name = "Estado Actual")]
        public string EstadoActual { get; set; }

        [Required(ErrorMessage = "Seleccione un nuevo estado")]
        [Display(Name = "Nuevo Estado")]
        public string NuevoEstado { get; set; }

        public List<SelectListItem> EstadosDisponibles { get; set; }
    }
}