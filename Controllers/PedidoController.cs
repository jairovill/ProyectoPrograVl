using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoPrograVI.Data;
using ProyectoPrograVI.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;
using iTextSharp.text.pdf;
using iTextSharp.text;
using System.IO;

namespace ProyectoPrograVI.Controllers
{
    [Authorize(Roles = "Cliente,Admin")]
    public class PedidoController : Controller
    {
        private readonly PedidoADORepository _pedidoRepository;
        private readonly CarritoADORepository _carritoRepository;
        private readonly MetodoPagoADORepository _metodoPagoRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PedidoController(
            PedidoADORepository pedidoRepository,
            CarritoADORepository carritoRepository,
            MetodoPagoADORepository metodoPagoRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _pedidoRepository = pedidoRepository;
            _carritoRepository = carritoRepository;
            _metodoPagoRepository = metodoPagoRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        private int ObtenerUsuarioId()
        {
            return int.Parse(_httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value);
        }

        public async Task<IActionResult> Index()
        {
            var usuarioId = ObtenerUsuarioId();
            var pedidos = await _pedidoRepository.ObtenerPedidosPorUsuario(usuarioId);
            return View(pedidos);
        }

        public async Task<IActionResult> Details(int id)
        {
            var usuarioId = ObtenerUsuarioId();
            var pedido = await _pedidoRepository.ObtenerPedidoPorId(id, usuarioId);

            if (pedido == null)
            {
                TempData["ErrorMessage"] = "Pedido no encontrado";
                return RedirectToAction(nameof(Index));
            }

            return View(pedido);
        }

        [HttpGet]
        [Authorize(Roles = "Cliente,Admin")]
        public IActionResult Checkout()
        {
            try
            {
                var usuarioId = ObtenerUsuarioId();
                if (usuarioId == 0)
                {
                    TempData["ErrorMessage"] = "Debe iniciar sesión para continuar";
                    return RedirectToAction("Login", "Account");
                }

                var itemsCarrito = _carritoRepository.ObtenerItems(usuarioId).ToList();

                if (!itemsCarrito.Any())
                {
                    TempData["ErrorMessage"] = "No hay items en el carrito";
                    return RedirectToAction("Index", "Carrito");
                }

                // Calcular totales
                decimal subtotal = itemsCarrito.Sum(i => i.Precio * i.Cantidad);
                decimal impuestos = subtotal * 0.13m;
                decimal total = subtotal + impuestos;

                ViewBag.Subtotal = subtotal;
                ViewBag.Impuestos = impuestos;
                ViewBag.Total = total;
                ViewBag.MetodosPago = _metodoPagoRepository.GetAll(true);
                ViewBag.ItemsCarrito = itemsCarrito;

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ocurrió un error al procesar su solicitud";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Cliente,Admin")]
        public async Task<IActionResult> Checkout(string direccionEnvio, int metodoPagoId)
        {
            try
            {
                var usuarioId = ObtenerUsuarioId();
                var itemsCarrito = _carritoRepository.ObtenerItems(usuarioId).ToList();

                if (!itemsCarrito.Any())
                {
                    TempData["ErrorMessage"] = "No hay items en el carrito";
                    return RedirectToAction("Index", "Carrito");
                }

                // Validaciones
                if (string.IsNullOrWhiteSpace(direccionEnvio))
                {
                    TempData["ErrorMessage"] = "Debe proporcionar una dirección de envío válida";
                    return RedirectToAction(nameof(Checkout));
                }

                var metodoPago = _metodoPagoRepository.GetById(metodoPagoId);
                if (metodoPago == null || !metodoPago.Activo)
                {
                    TempData["ErrorMessage"] = "Método de pago no válido o inactivo";
                    return RedirectToAction(nameof(Checkout));
                }

                // Cálculos
                decimal subtotal = itemsCarrito.Sum(i => i.Precio * i.Cantidad);
                decimal impuestos = subtotal * 0.13m;
                decimal total = subtotal + impuestos;

                // Crear pedido
                var pedido = new Pedido
                {
                    UsuarioId = usuarioId,
                    DireccionEnvio = direccionEnvio.Trim(),
                    MetodoPagoId = metodoPagoId,
                    Subtotal = subtotal,
                    Impuestos = impuestos,
                    Total = total,
                    Estado = "pendiente",
                    EstadoPago = "pendiente",
                    FechaPedido = DateTime.Now
                };

                var (success, pedidoId, message) = await _pedidoRepository.CrearPedido(pedido);
                if (!success || pedidoId == 0)
                {
                    TempData["ErrorMessage"] = message ?? "Error al crear el pedido";
                    return RedirectToAction(nameof(Checkout));
                }

                // Agregar detalles
                bool todosDetallesAgregados = true;
                var detallesFallidos = new List<string>();

                foreach (var item in itemsCarrito)
                {
                    var detalle = new DetallePedido
                    {
                        PedidoId = pedidoId,
                        LibroId = item.LibroId,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.Precio,
                        Subtotal = item.Precio * item.Cantidad
                    };

                    var resultadoDetalle = await _pedidoRepository.AgregarDetallePedido(detalle);
                    if (!resultadoDetalle)
                    {
                        todosDetallesAgregados = false;
                        detallesFallidos.Add(item.Titulo);
                    }
                }

                if (!todosDetallesAgregados)
                {
                    // Usar el nuevo método EliminarPedido que devuelve tupla (bool, string)
                    var (deleteSuccess, deleteMessage) = await _pedidoRepository.EliminarPedido(pedidoId);

                    TempData["ErrorMessage"] = deleteSuccess ?
                        $"Error al agregar los siguientes items: {string.Join(", ", detallesFallidos)}. El pedido fue cancelado." :
                        $"Error al agregar items y al cancelar el pedido: {deleteMessage}";

                    return RedirectToAction(nameof(Checkout));
                }

                // Redirigir según método de pago
                if (metodoPago.Nombre.ToLower().Contains("tarjeta"))
                {
                    return RedirectToAction("PagoTarjeta", new { pedidoId });
                }

                // Para otros métodos de pago
                await _pedidoRepository.ActualizarEstadoPago(
                    pedidoId,
                    "completado",
                    Guid.NewGuid().ToString(),
                    $"Pago con {metodoPago.Nombre}");

                // Limpiar carrito
                foreach (var item in itemsCarrito)
                {
                    _carritoRepository.EliminarItem(item.CarritoId, out _);
                }

                TempData["SuccessMessage"] = $"Pedido #{pedidoId} creado exitosamente!";
                return RedirectToAction("Details", new { id = pedidoId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error inesperado al procesar su pedido";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> PagoTarjeta(int pedidoId)
        {
            var usuarioId = ObtenerUsuarioId();
            var pedido = await _pedidoRepository.ObtenerPedidoPorId(pedidoId, usuarioId);

            if (pedido == null || pedido.UsuarioId != usuarioId)
            {
                TempData["ErrorMessage"] = "Pedido no encontrado";
                return RedirectToAction(nameof(Index));
            }

            if (pedido.EstadoPago != "pendiente")
            {
                TempData["ErrorMessage"] = "El pago de este pedido ya fue procesado";
                return RedirectToAction("Details", new { id = pedidoId });
            }

            var model = new PagoTarjetaViewModel
            {
                PedidoId = pedidoId,
                MetodoPago = "Tarjeta"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PagoTarjeta(PagoTarjetaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuarioId = ObtenerUsuarioId();
            var pedido = await _pedidoRepository.ObtenerPedidoPorId(model.PedidoId, usuarioId);

            if (pedido == null || pedido.UsuarioId != usuarioId)
            {
                TempData["ErrorMessage"] = "Pedido no encontrado";
                return RedirectToAction(nameof(Index));
            }

            if (pedido.EstadoPago != "pendiente")
            {
                TempData["ErrorMessage"] = "El pago de este pedido ya fue procesado";
                return RedirectToAction("Details", new { id = model.PedidoId });
            }

            // Limpiar formato de tarjeta (eliminar espacios)
            var numeroTarjetaLimpio = model.NumeroTarjeta?.Replace(" ", "") ?? "";

            // Procesar pago con la pasarela
            var (success, message) = await _pedidoRepository.ProcesarPagoTarjeta(
                model.PedidoId,
                numeroTarjetaLimpio,
                model.ICVV,
                model.FechaVencimiento,
                pedido.Total,
                model.NombreTitular);

            if (success)
            {
                // Actualizar estado del pago
                await _pedidoRepository.ActualizarEstadoPago(
                    model.PedidoId,
                    "completado",
                    Guid.NewGuid().ToString(),
                    $"Pago con tarjeta aprobado. Ref: {message}");

                // Limpiar carrito
                var itemsCarrito = _carritoRepository.ObtenerItems(usuarioId).ToList();
                foreach (var item in itemsCarrito)
                {
                    _carritoRepository.EliminarItem(item.CarritoId, out _);
                }

                TempData["SuccessMessage"] = "¡Pago procesado exitosamente! Su pedido ha sido confirmado.";
                return RedirectToAction("Details", new { id = model.PedidoId });
            }
            else
            {
                // Registrar fallo en el pago
                await _pedidoRepository.ActualizarEstadoPago(
                    model.PedidoId,
                    "fallido",
                    null,
                    $"Error en pago con tarjeta: {message}");

                // Mostrar mensaje de error más amigable
                var errorMessage = message.Contains("Datos incorrectos") ?
                    "Los datos de la tarjeta son incorrectos. Por favor verifique e intente nuevamente." :
                    $"Error al procesar el pago: {message}";

                TempData["ErrorMessage"] = errorMessage;

                // Mantener los valores ingresados para que el usuario no tenga que volver a escribirlos
                model.NumeroTarjeta = model.NumeroTarjeta; // Mantener el formato con espacios
                return View(model);
            }
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var pedido = await _pedidoRepository.ObtenerPedidoPorIdAdmin(id);

                if (pedido == null)
                {
                    TempData["ErrorMessage"] = "Pedido no encontrado";
                    return RedirectToAction(nameof(Admin));
                }

                var model = new EditarPedidoViewModel
                {
                    PedidoId = pedido.PedidoId,
                    NumeroPedido = pedido.NumeroSeguimiento,
                    EstadoActual = pedido.Estado,
                    EstadosDisponibles = ObtenerEstadosPedido(),
                    NuevoEstado = pedido.Estado // Establecer estado actual como seleccionado
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar el pedido: {ex.Message}";
                return RedirectToAction(nameof(Admin));
            }
        }

        // POST: Pedido/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditarPedidoViewModel model)
        {
            model.EstadosDisponibles = ObtenerEstadosPedido();

            if (string.IsNullOrEmpty(model.NuevoEstado) ||
                !model.EstadosDisponibles.Any(e => e.Value == model.NuevoEstado))
            {
                ModelState.AddModelError("NuevoEstado", "Por favor seleccione un estado válido");
            }

            if (!ModelState.IsValid)
            {
                var pedido = await _pedidoRepository.ObtenerPedidoPorIdAdmin(model.PedidoId);
                if (pedido != null)
                {
                    model.NumeroPedido = pedido.NumeroSeguimiento;
                    model.EstadoActual = pedido.Estado;
                }
                return View(model);
            }

            try
            {
                var (success, message) = await _pedidoRepository.ActualizarEstadoPedido(
                    model.PedidoId,
                    model.NuevoEstado);

                if (success)
                    TempData["SuccessMessage"] = message;
                else
                    TempData["ErrorMessage"] = message;

                return RedirectToAction(nameof(Admin));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error inesperado: {ex.Message}";
                return RedirectToAction(nameof(Admin));
            }
        }

        // Método auxiliar para obtener los estados
        private List<SelectListItem> ObtenerEstadosPedido()
        {
            return new List<SelectListItem>
    {
        new SelectListItem { Value = "pendiente", Text = "Pendiente" },
        new SelectListItem { Value = "procesando", Text = "Procesando" },
        new SelectListItem { Value = "enviado", Text = "Enviado" },
        new SelectListItem { Value = "entregado", Text = "Entregado" },
        new SelectListItem { Value = "cancelado", Text = "Cancelado" }
    };
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Admin(FiltroPedidosAdminViewModel filtro)
        {
            try
            {
                var (pedidos, totalRegistros) = await _pedidoRepository.ObtenerPedidosAdmin(
                    filtro.Estado,
                    filtro.EstadoPago,
                    filtro.FechaDesde,
                    filtro.FechaHasta,
                    filtro.Pagina,
                    filtro.RegistrosPorPagina);

                var model = new FiltroPedidosAdminViewModel
                {
                    Estado = filtro.Estado,
                    EstadoPago = filtro.EstadoPago,
                    FechaDesde = filtro.FechaDesde,
                    FechaHasta = filtro.FechaHasta,
                    Pagina = filtro.Pagina,
                    RegistrosPorPagina = filtro.RegistrosPorPagina,
                    TotalRegistros = totalRegistros,
                    Pedidos = pedidos
                };

                ViewBag.EstadosPedido = new List<SelectListItem>
        {
            new SelectListItem { Value = "", Text = "Todos" },
            new SelectListItem { Value = "pendiente", Text = "Pendiente" },
            new SelectListItem { Value = "procesando", Text = "Procesando" },
            new SelectListItem { Value = "enviado", Text = "Enviado" },
            new SelectListItem { Value = "entregado", Text = "Entregado" },
            new SelectListItem { Value = "cancelado", Text = "Cancelado" }
        };

                ViewBag.EstadosPago = new List<SelectListItem>
        {
            new SelectListItem { Value = "", Text = "Todos" },
            new SelectListItem { Value = "pendiente", Text = "Pendiente" },
            new SelectListItem { Value = "completado", Text = "Completado" },
            new SelectListItem { Value = "fallido", Text = "Fallido" },
            new SelectListItem { Value = "reembolsado", Text = "Reembolsado" }
        };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar los pedidos: {ex.Message}";
                return View(new FiltroPedidosAdminViewModel());
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportarCSV(FiltroPedidosAdminViewModel filtro)
        {
            // Obtener todos los pedidos sin paginación para el CSV
            var (pedidos, _) = await _pedidoRepository.ObtenerPedidosAdmin(
                filtro.Estado,
                filtro.EstadoPago,
                filtro.FechaDesde,
                filtro.FechaHasta,
                1, // Página 1
                int.MaxValue); // Todos los registros

            var csv = new StringBuilder();
            // Encabezados
            csv.AppendLine("N° Pedido;Cliente;Fecha;Total;Estado;Estado Pago;Método de Pago;Dirección Envío");

            foreach (var pedido in pedidos)
            {
                csv.AppendLine(
                    $"{pedido.NumeroSeguimiento};" +
                    $"{pedido.Cliente};" +
                    $"{pedido.FechaPedido:dd/MM/yyyy HH:mm};" +
                    $"{pedido.Total.ToString("F2")};" +
                    $"{pedido.Estado};" +
                    $"{pedido.EstadoPago};" +
                    $"{pedido.MetodoPago};" +
                    $"{pedido.DireccionEnvio?.Replace(";", ",")}"
                );
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"pedidos_{DateTime.Now:yyyyMMddHHmmss}.csv");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportarPDF(FiltroPedidosAdminViewModel filtro)
        {
            // Obtener todos los pedidos sin paginación para el PDF
            var (pedidos, _) = await _pedidoRepository.ObtenerPedidosAdmin(
                filtro.Estado,
                filtro.EstadoPago,
                filtro.FechaDesde,
                filtro.FechaHasta,
                1, // Página 1
                int.MaxValue); // Todos los registros

            using (MemoryStream ms = new MemoryStream())
            {
                // Tamaño de página horizontal (Landscape)
                Document document = new Document(PageSize.A4.Rotate(), 20, 20, 30, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);

                document.Open();

                // Encabezado del reporte
                var titulo = new Paragraph("Reporte de Pedidos",
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18));
                titulo.Alignment = Element.ALIGN_CENTER;
                document.Add(titulo);

                // Fecha de generación
                var fechaGeneracion = new Paragraph($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}",
                    FontFactory.GetFont(FontFactory.HELVETICA, 10));
                fechaGeneracion.Alignment = Element.ALIGN_RIGHT;
                document.Add(fechaGeneracion);

                document.Add(new Paragraph(" ")); // Espacio en blanco

                // Crear tabla con 7 columnas
                PdfPTable table = new PdfPTable(7);
                table.WidthPercentage = 100; // Ancho completo de la página
                table.SetWidths(new float[] { 1.5f, 2f, 1.5f, 1f, 1f, 1f, 1.5f }); // Anchuras relativas

                // Encabezados de tabla
                AddHeaderCell(table, "N° Pedido");
                AddHeaderCell(table, "Cliente");
                AddHeaderCell(table, "Fecha");
                AddHeaderCell(table, "Total");
                AddHeaderCell(table, "Estado");
                AddHeaderCell(table, "Pago");
                AddHeaderCell(table, "Método");

                // Llenar la tabla con datos
                foreach (var pedido in pedidos)
                {
                    table.AddCell(new Phrase(pedido.NumeroSeguimiento ?? "N/A"));
                    table.AddCell(new Phrase(pedido.Cliente ?? "N/A"));
                    table.AddCell(new Phrase(pedido.FechaPedido.ToString("dd/MM/yyyy HH:mm")));
                    table.AddCell(new Phrase(pedido.Total.ToString("C")));

                    // Celda de estado con color
                    PdfPCell estadoCell = new PdfPCell(new Phrase(pedido.Estado ?? "N/A"));
                    estadoCell.BackgroundColor = GetEstadoColor(pedido.Estado);
                    table.AddCell(estadoCell);

                    // Celda de estado de pago con color
                    PdfPCell pagoCell = new PdfPCell(new Phrase(pedido.EstadoPago ?? "N/A"));
                    pagoCell.BackgroundColor = GetEstadoPagoColor(pedido.EstadoPago);
                    table.AddCell(pagoCell);

                    table.AddCell(new Phrase(pedido.MetodoPago ?? "N/A"));
                }

                document.Add(table);
                document.Close();

                return File(ms.ToArray(), "application/pdf", $"pedidos_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }
        }

        // Método auxiliar para celdas de encabezado
        private void AddHeaderCell(PdfPTable table, string text)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, FontFactory.GetFont(FontFactory.HELVETICA_BOLD)));
            cell.BackgroundColor = new BaseColor(200, 200, 200); // Gris claro
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            table.AddCell(cell);
        }

        // Métodos auxiliares para colores de estado - VERSIÓN CORREGIDA
        private BaseColor GetEstadoColor(string estado)
        {
            if (string.IsNullOrEmpty(estado))
                return new BaseColor(255, 255, 255); // Blanco en RGB

            return estado.ToLower() switch
            {
                "pendiente" => new BaseColor(108, 117, 125), // Gris
                "procesando" => new BaseColor(13, 110, 253), // Azul
                "enviado" => new BaseColor(13, 202, 240),    // Celeste
                "entregado" => new BaseColor(25, 135, 84),   // Verde
                "cancelado" => new BaseColor(220, 53, 69),   // Rojo
                _ => new BaseColor(255, 255, 255)            // Blanco en RGB
            };
        }

        private BaseColor GetEstadoPagoColor(string estadoPago)
        {
            if (string.IsNullOrEmpty(estadoPago))
                return new BaseColor(255, 255, 255); // Blanco en RGB

            return estadoPago.ToLower() switch
            {
                "pendiente" => new BaseColor(108, 117, 125), // Gris
                "completado" => new BaseColor(25, 135, 84),  // Verde
                "fallido" => new BaseColor(220, 53, 69),     // Rojo
                "reembolsado" => new BaseColor(255, 193, 7), // Amarillo (RGB: 255,193,7)
                _ => new BaseColor(255, 255, 255)            // Blanco en RGB
            };
        }

    }
}