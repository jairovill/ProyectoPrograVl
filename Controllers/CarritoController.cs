using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ProyectoPrograVI.Data;
using ProyectoPrograVI.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Security.Claims;

namespace ProyectoPrograVI.Controllers
{
    [Authorize(Roles = "Cliente,Admin")]
    public class CarritoController : Controller
    {
        private readonly string _connectionString;
        private readonly CarritoADORepository _carritoRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CarritoController(IConfiguration configuration, CarritoADORepository carritoRepository,
                               IHttpContextAccessor httpContextAccessor)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _carritoRepository = carritoRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public IActionResult Index()
        {
            int usuarioId = ObtenerUsuarioId();

            if (usuarioId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            List<ItemCarrito> carrito = ObtenerCarritoUsuario(usuarioId);
            return View(carrito);
        }

        // GET: Carrito/Edit/5
        public IActionResult Edit(int id)
        {
            int usuarioId = ObtenerUsuarioId();
            if (usuarioId == 0) return RedirectToAction("Login", "Usuario");

            var item = _carritoRepository.ObtenerItemCarrito(id, usuarioId);
            if (item == null)
            {
                TempData["ErrorMessage"] = "El ítem no existe o no pertenece a tu carrito";
                return RedirectToAction(nameof(Index));
            }

            // Obtener stock disponible
            int stockDisponible = 0;
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand(
                    "SELECT unidades_disponibles FROM libro WHERE libro_id = @libroId AND estado = 'activo'",
                    conn);
                cmd.Parameters.AddWithValue("@libroId", item.LibroId);
                conn.Open();
                var result = cmd.ExecuteScalar();
                stockDisponible = result != DBNull.Value ? Convert.ToInt32(result) : 0;
            }

            ViewBag.StockDisponible = stockDisponible;
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, ItemCarrito model)
        {
            if (id != model.CarritoId)
            {
                TempData["ErrorMessage"] = "ID de carrito no coincide";
                return RedirectToAction(nameof(Index));
            }

            int usuarioId = ObtenerUsuarioId();
            if (usuarioId == 0) return RedirectToAction("Login", "Usuario");

            // Validar stock
            int stockDisponible = 0;
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand(
                    "SELECT unidades_disponibles FROM libro WHERE libro_id = @libroId AND estado = 'activo'",
                    conn);
                cmd.Parameters.AddWithValue("@libroId", model.LibroId);
                conn.Open();
                var result = cmd.ExecuteScalar();
                stockDisponible = result != DBNull.Value ? Convert.ToInt32(result) : 0;
            }

            if (model.Cantidad > stockDisponible)
            {
                TempData["ErrorMessage"] = $"No hay suficiente stock. Máximo disponible: {stockDisponible}";
                return RedirectToAction(nameof(Edit), new { id = model.CarritoId });
            }

            // Actualizar
            var (success, message) = _carritoRepository.ActualizarCantidad(
                model.CarritoId,
                model.Cantidad,
                usuarioId);

            if (success)
            {
                TempData["SuccessMessage"] = message;
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = message;
            return RedirectToAction(nameof(Edit), new { id = model.CarritoId });
        }

        private int ObtenerUsuarioId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            bool eliminado = EliminarItemCarrito(id);
            if (eliminado)
            {
                TempData["SuccessMessage"] = "Item eliminado del carrito";
            }
            else
            {
                TempData["ErrorMessage"] = "Error al eliminar el item";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int libroId, int cantidad)
        {
            int usuarioId = ObtenerUsuarioId();
            if (usuarioId == 0)
                return RedirectToAction("Login", "Account");

            bool resultado = _carritoRepository.AgregarItem(usuarioId, libroId, cantidad, out string mensaje);

            if (resultado)
            {
                TempData["SuccessMessage"] = mensaje;
            }
            else
            {
                TempData["ErrorMessage"] = mensaje;
            }

            return RedirectToAction(nameof(Index));
        }
        #region Métodos privados



        private List<ItemCarrito> ObtenerCarritoUsuario(int usuarioId)
        {
            var items = new List<ItemCarrito>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("sp_ObtenerCarritoUsuario", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@usuario_id", usuarioId);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new ItemCarrito
                        {
                            CarritoId = Convert.ToInt32(reader["carrito_id"]),
                            LibroId = Convert.ToInt32(reader["libro_id"]),
                            Titulo = reader["titulo"].ToString(),
                            Autor = reader["autor"].ToString(),
                            ImagenUrl = reader["imagen_url"]?.ToString(),
                            Precio = Convert.ToDecimal(reader["precio_unitario"]),
                            Cantidad = Convert.ToInt32(reader["cantidad"]),
                            FechaAgregado = Convert.ToDateTime(reader["fecha_agregado"])
                        });
                    }
                }
            }

            return items;
        }

        private ItemCarrito ObtenerItemPorId(int carritoId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("sp_ObtenerItemCarritoPorId", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@carrito_id", carritoId);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new ItemCarrito
                        {
                            CarritoId = Convert.ToInt32(reader["carrito_id"]),
                            LibroId = Convert.ToInt32(reader["libro_id"]),
                            Titulo = reader["titulo"].ToString(),
                            Autor = reader["autor"].ToString(),
                            ImagenUrl = reader["imagen_url"]?.ToString(),
                            Precio = Convert.ToDecimal(reader["precio_unitario"]),
                            Cantidad = Convert.ToInt32(reader["cantidad"]),
                            FechaAgregado = Convert.ToDateTime(reader["fecha_agregado"])
                        };
                    }
                }
            }

            return null;
        }

        private bool ActualizarCantidadItem(ItemCarrito item)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand("sp_actualizar_carrito", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@carrito_id", item.CarritoId);
                    cmd.Parameters.AddWithValue("@nueva_cantidad", item.Cantidad);
                    cmd.Parameters.AddWithValue("@usuario_id", ObtenerUsuarioId());

                    // Parámetros de salida
                    SqlParameter mensajeParam = new SqlParameter("@mensaje", SqlDbType.VarChar, 500)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(mensajeParam);

                    SqlParameter errorParam = new SqlParameter("@error", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(errorParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    bool resultado = (int)errorParam.Value == 0;
                    string mensaje = mensajeParam.Value?.ToString();

                    if (!resultado)
                    {
                        TempData["ErrorMessage"] = mensaje;
                    }

                    return resultado;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al actualizar cantidad: {ex.Message}";
                return false;
            }
        }

        private bool EliminarItemCarrito(int carritoId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand("sp_EliminarItemCarrito", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@carrito_id", carritoId);

                    // Parámetros de salida
                    SqlParameter mensajeParam = new SqlParameter("@mensaje", SqlDbType.VarChar, 500)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(mensajeParam);

                    SqlParameter errorParam = new SqlParameter("@error", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(errorParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    bool resultado = (int)errorParam.Value == 0;
                    string mensaje = mensajeParam.Value?.ToString();

                    if (!resultado)
                    {
                        TempData["ErrorMessage"] = mensaje;
                    }

                    return resultado;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al eliminar item: {ex.Message}";
                return false;
            }
        }

        private bool AgregarItemAlCarrito(int usuarioId, int libroId, int cantidad, out string mensaje)
        {
            mensaje = string.Empty;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand("sp_AgregarAlCarrito", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@usuario_id", usuarioId);
                    cmd.Parameters.AddWithValue("@libro_id", libroId);
                    cmd.Parameters.AddWithValue("@cantidad", cantidad);

                    // Parámetros de salida
                    SqlParameter mensajeParam = new SqlParameter("@mensaje", SqlDbType.VarChar, 500)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(mensajeParam);

                    SqlParameter errorParam = new SqlParameter("@error", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(errorParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    bool resultado = (int)errorParam.Value == 0;
                    mensaje = mensajeParam.Value?.ToString();

                    return resultado;
                }
            }
            catch (Exception ex)
            {
                mensaje = $"Error al agregar al carrito: {ex.Message}";
                return false;
            }
        }

        #endregion
    }
}