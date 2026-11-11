namespace ProyectoPrograVI.Models
{
    public class CarritoItem
    {
        public int CarritoId { get; set; }
        public int LibroId { get; set; }
        public string Titulo { get; set; }
        public string Autor { get; set; }
        public string ImagenUrl { get; set; }
        public decimal PrecioUnitario { get; set; }
        public int Cantidad { get; set; }
    }
}
