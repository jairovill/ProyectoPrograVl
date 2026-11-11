namespace ProyectoPrograVI.Models
{
    public class ItemCarrito
    {
        public int CarritoId { get; set; }
        public int LibroId { get; set; }
        public string Titulo { get; set; }
        public string Autor { get; set; }
        public string ImagenUrl { get; set; }
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
        public DateTime FechaAgregado { get; set; }
        public decimal Subtotal => Precio * Cantidad;
    }
}