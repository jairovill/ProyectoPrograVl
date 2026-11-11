namespace ProyectoPrograVI.Models
{
    public class FiltroPedidosAdminViewModel
    {
        public string Estado { get; set; }
        public string EstadoPago { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public int Pagina { get; set; } = 1;
        public int RegistrosPorPagina { get; set; } = 10;
        public int TotalRegistros { get; set; }
        public IEnumerable<PedidoAdminViewModel> Pedidos { get; set; }
    }
}