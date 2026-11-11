using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProyectoPrograVI.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProyectoPrograVI.Data
{
    public class PedidoADORepository
    {
        private readonly string _connectionString;

        public PedidoADORepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<(bool success, int pedidoId, string message)> CrearPedido(Pedido pedido)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("sp_guardar_pedido", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@usuario_id", pedido.UsuarioId);
                cmd.Parameters.AddWithValue("@direccion_envio", pedido.DireccionEnvio);
                cmd.Parameters.AddWithValue("@metodo_pago_id", pedido.MetodoPagoId);
                cmd.Parameters.AddWithValue("@subtotal", pedido.Subtotal);
                cmd.Parameters.AddWithValue("@impuestos", pedido.Impuestos);
                cmd.Parameters.AddWithValue("@total", pedido.Total);
                cmd.Parameters.AddWithValue("@estado_pago", pedido.EstadoPago);
                cmd.Parameters.AddWithValue("@transaccion_id", pedido.TransaccionId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@detalles_pago", pedido.DetallesPago ?? (object)DBNull.Value);

                var pedidoIdParam = new SqlParameter("@pedido_id", SqlDbType.Int) { Direction = ParameterDirection.Output };
                var mensajeParam = new SqlParameter("@mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
                var errorParam = new SqlParameter("@error", SqlDbType.Int) { Direction = ParameterDirection.Output };

                cmd.Parameters.Add(pedidoIdParam);
                cmd.Parameters.Add(mensajeParam);
                cmd.Parameters.Add(errorParam);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                var success = (int)errorParam.Value == 0;
                var message = mensajeParam.Value.ToString();
                var id = success ? Convert.ToInt32(pedidoIdParam.Value) : 0;

                return (success, id, message);
            }
            catch (Exception ex)
            {
                return (false, 0, $"Error al crear pedido: {ex.Message}");
            }
        }

        public async Task<bool> AgregarDetallePedido(DetallePedido detalle)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("sp_guardar_detalle_pedido", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@pedido_id", detalle.PedidoId);
                cmd.Parameters.AddWithValue("@libro_id", detalle.LibroId);
                cmd.Parameters.AddWithValue("@cantidad", detalle.Cantidad);
                cmd.Parameters.AddWithValue("@precio_unitario", detalle.PrecioUnitario);
                cmd.Parameters.AddWithValue("@subtotal", detalle.Subtotal);

                var mensajeParam = new SqlParameter("@mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
                var errorParam = new SqlParameter("@error", SqlDbType.Int) { Direction = ParameterDirection.Output };

                cmd.Parameters.Add(mensajeParam);
                cmd.Parameters.Add(errorParam);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                return (int)errorParam.Value == 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Pedido> ObtenerPedidoPorId(int pedidoId, int usuarioId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("sp_ObtenerPedidoCompleto", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@pedido_id", pedidoId);
                cmd.Parameters.AddWithValue("@usuario_id", usuarioId);

                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                Pedido pedido = null;

                if (await reader.ReadAsync())
                {
                    pedido = new Pedido
                    {
                        PedidoId = reader.GetInt32(reader.GetOrdinal("pedido_id")),
                        UsuarioId = reader.GetInt32(reader.GetOrdinal("usuario_id")),
                        FechaPedido = reader.GetDateTime(reader.GetOrdinal("fecha_pedido")),
                        FechaActualizacion = reader.IsDBNull(reader.GetOrdinal("fecha_actualizacion")) ?
                            null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("fecha_actualizacion")),
                        Estado = reader.GetString(reader.GetOrdinal("estado")),
                        Subtotal = reader.GetDecimal(reader.GetOrdinal("subtotal")),
                        Impuestos = reader.GetDecimal(reader.GetOrdinal("impuestos")),
                        Total = reader.GetDecimal(reader.GetOrdinal("total")),
                        DireccionEnvio = reader.GetString(reader.GetOrdinal("direccion_envio")),
                        MetodoPagoId = reader.GetInt32(reader.GetOrdinal("metodo_pago_id")),
                        NumeroSeguimiento = reader.GetString(reader.GetOrdinal("numero_seguimiento")),
                        EstadoPago = reader.GetString(reader.GetOrdinal("estado_pago")),
                        TransaccionId = reader.IsDBNull(reader.GetOrdinal("transaccion_id")) ?
                            null : reader.GetString(reader.GetOrdinal("transaccion_id")),
                        DetallesPago = reader.IsDBNull(reader.GetOrdinal("detalles_pago")) ?
                            null : reader.GetString(reader.GetOrdinal("detalles_pago")),
                        MetodoPago = new MetodoPago
                        {
                            MetodoPagoId = reader.GetInt32(reader.GetOrdinal("metodo_pago_id")),
                            Nombre = reader.GetString(reader.GetOrdinal("metodo_pago_nombre"))
                        }
                    };
                }

                if (pedido != null && await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        pedido.Detalles.Add(new DetallePedido
                        {
                            DetalleId = reader.GetInt32(reader.GetOrdinal("detalle_id")),
                            PedidoId = reader.GetInt32(reader.GetOrdinal("pedido_id")),
                            LibroId = reader.GetInt32(reader.GetOrdinal("libro_id")),
                            Cantidad = reader.GetInt32(reader.GetOrdinal("cantidad")),
                            PrecioUnitario = reader.GetDecimal(reader.GetOrdinal("precio_unitario")),
                            Subtotal = reader.GetDecimal(reader.GetOrdinal("subtotal")),
                            Libro = new Libro
                            {
                                LibroId = reader.GetInt32(reader.GetOrdinal("libro_id")),
                                Titulo = reader.GetString(reader.GetOrdinal("titulo")),
                                ImagenUrl = reader.IsDBNull(reader.GetOrdinal("imagen_url")) ?
                                    null : reader.GetString(reader.GetOrdinal("imagen_url"))
                            }
                        });
                    }
                }

                return pedido;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<Pedido>> ObtenerPedidosPorUsuario(int usuarioId, string estado = null, string estadoPago = null)
        {
            var pedidos = new List<Pedido>();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("sp_ObtenerPedidoPorIdUsuario", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@usuario_id", usuarioId);
                cmd.Parameters.AddWithValue("@estado", estado ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@estado_pago", estadoPago ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@pagina", 1);
                cmd.Parameters.AddWithValue("@registros_por_pagina", 100);

                var totalParam = new SqlParameter("@total_registros", SqlDbType.Int) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(totalParam);

                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    pedidos.Add(new Pedido
                    {
                        PedidoId = reader.GetInt32(reader.GetOrdinal("pedido_id")),
                        FechaPedido = reader.GetDateTime(reader.GetOrdinal("fecha_pedido")),
                        Estado = reader.GetString(reader.GetOrdinal("estado")),
                        Total = reader.GetDecimal(reader.GetOrdinal("total")),
                        NumeroSeguimiento = reader.GetString(reader.GetOrdinal("numero_seguimiento")),
                        EstadoPago = reader.GetString(reader.GetOrdinal("estado_pago")),
                        MetodoPago = new MetodoPago
                        {
                            Nombre = reader.GetString(reader.GetOrdinal("metodo_pago"))
                        }
                    });
                }
            }
            catch
            {
                // Log error
            }

            return pedidos;
        }

        public async Task<(bool success, string message)> ActualizarEstadoPago(int pedidoId, string nuevoEstado, string transaccionId = null, string detallesPago = null)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("sp_Pedido_ActualizarEstadoPago", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@pedido_id", pedidoId);
                cmd.Parameters.AddWithValue("@nuevo_estado_pago", nuevoEstado);
                cmd.Parameters.AddWithValue("@transaccion_id", transaccionId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@detalles_pago", detallesPago ?? (object)DBNull.Value);

                var mensajeParam = new SqlParameter("@mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
                var errorParam = new SqlParameter("@error", SqlDbType.Int) { Direction = ParameterDirection.Output };

                cmd.Parameters.Add(mensajeParam);
                cmd.Parameters.Add(errorParam);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                var success = (int)errorParam.Value == 0;
                var message = mensajeParam.Value.ToString();

                return (success, message);
            }
            catch (Exception ex)
            {
                return (false, $"Error al actualizar estado de pago: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> ProcesarPagoTarjeta(int pedidoId, string numeroTarjeta, string icvv, string fechaVencimiento, decimal monto, string nombreCliente)
        {
            try
            {
                // Preparar el objeto de pago según lo espera el API
                var pagoData = new
                {
                    nombre = nombreCliente,
                    numeroTarjeta = numeroTarjeta.Replace(" ", ""), // Eliminar espacios
                    icvv = icvv,
                    fechaVencimiento = fechaVencimiento,
                    monto = monto
                };

                using var client = new HttpClient();
                client.BaseAddress = new Uri("https://pagosapi-b8op.onrender.com/");

                // Configurar el timeout (opcional)
                client.Timeout = TimeSpan.FromSeconds(30);

                // Serializar el objeto con las opciones correctas
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };

                var json = JsonSerializer.Serialize(pagoData, jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Agregar headers si son necesarios
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.PostAsync("pago", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();

                    // El API parece devolver un mensaje simple, puedes ajustar esto según la respuesta real
                    return (true, "Pago procesado exitosamente");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();

                    try
                    {
                        // Intentar parsear el error como JSON
                        var errorObj = JsonSerializer.Deserialize<JsonElement>(errorContent);
                        if (errorObj.TryGetProperty("mensaje", out var mensajeProp))
                        {
                            return (false, mensajeProp.GetString());
                        }
                        return (false, errorContent);
                    }
                    catch
                    {
                        return (false, errorContent);
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                return (false, $"Error de conexión con el servicio de pagos: {httpEx.Message}");
            }
            catch (TaskCanceledException)
            {
                return (false, "El tiempo de espera para el pago ha expirado");
            }
            catch (Exception ex)
            {
                return (false, $"Error al procesar el pago: {ex.Message}");
            }
        }
        public async Task<(bool success, string message)> EliminarPedido(int pedidoId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("sp_EliminarPedido", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@pedido_id", pedidoId);

                var errorParam = new SqlParameter("@error", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };

                var mensajeParam = new SqlParameter("@mensaje", SqlDbType.VarChar, 500)
                {
                    Direction = ParameterDirection.Output
                };

                cmd.Parameters.Add(errorParam);
                cmd.Parameters.Add(mensajeParam);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                bool success = (int)errorParam.Value == 0;
                string message = mensajeParam.Value?.ToString();

                return (success, message);
            }
            catch (Exception ex)
            {
                // Loggear el error si tienes un sistema de logging
                return (false, $"Error al eliminar el pedido: {ex.Message}");
            }
        }
        public async Task<(bool success, string message)> ActualizarEstadoPedido(int pedidoId, string nuevoEstado)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("sp_ActualizarEstadoPedido", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@pedido_id", pedidoId);
                cmd.Parameters.AddWithValue("@nuevo_estado", nuevoEstado);

                var mensajeParam = new SqlParameter("@mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
                var errorParam = new SqlParameter("@error", SqlDbType.Int) { Direction = ParameterDirection.Output };

                cmd.Parameters.Add(mensajeParam);
                cmd.Parameters.Add(errorParam);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                var success = (int)errorParam.Value == 0;
                var message = mensajeParam.Value.ToString();

                return (success, message);
            }
            catch (Exception ex)
            {
                return (false, $"Error al actualizar estado del pedido: {ex.Message}");
            }
        }
        public async Task<(IEnumerable<PedidoAdminViewModel> pedidos, int totalRegistros)> ObtenerPedidosAdmin(
    string estado = null,
    string estadoPago = null,
    DateTime? fechaDesde = null,
    DateTime? fechaHasta = null,
    int pagina = 1,
    int registrosPorPagina = 10)
        {
            var pedidos = new List<PedidoAdminViewModel>();
            int totalRegistros = 0;

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("sp_ObtenerPedidosAdmin", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@estado", estado ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@estado_pago", estadoPago ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@fecha_desde", fechaDesde?.Date ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@fecha_hasta", fechaHasta?.Date ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@pagina", pagina);
                cmd.Parameters.AddWithValue("@registros_por_pagina", registrosPorPagina);

                var totalParam = new SqlParameter("@total_registros", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(totalParam);

                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    pedidos.Add(new PedidoAdminViewModel
                    {
                        PedidoId = reader.GetInt32(reader.GetOrdinal("pedido_id")),
                        UsuarioId = reader.GetInt32(reader.GetOrdinal("usuario_id")),
                        Cliente = reader.GetString(reader.GetOrdinal("cliente")), // Ahora muestra el email
                        FechaPedido = reader.GetDateTime(reader.GetOrdinal("fecha_pedido")),
                        FechaActualizacion = reader.IsDBNull(reader.GetOrdinal("fecha_actualizacion")) ?
                            null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("fecha_actualizacion")),
                        Estado = reader.GetString(reader.GetOrdinal("estado")),
                        Subtotal = reader.GetDecimal(reader.GetOrdinal("subtotal")),
                        Impuestos = reader.GetDecimal(reader.GetOrdinal("impuestos")),
                        Total = reader.GetDecimal(reader.GetOrdinal("total")),
                        DireccionEnvio = reader.GetString(reader.GetOrdinal("direccion_envio")),
                        MetodoPago = reader.GetString(reader.GetOrdinal("metodo_pago")),
                        NumeroSeguimiento = reader.GetString(reader.GetOrdinal("numero_seguimiento")),
                        EstadoPago = reader.GetString(reader.GetOrdinal("estado_pago")),
                        TransaccionId = reader.IsDBNull(reader.GetOrdinal("transaccion_id")) ?
                            null : reader.GetString(reader.GetOrdinal("transaccion_id"))
                    });
                }

                await reader.CloseAsync();
                totalRegistros = Convert.ToInt32(totalParam.Value);
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error al obtener pedidos: {ex.Message}");
            }

            return (pedidos, totalRegistros);
        }
        public async Task<PedidoAdminViewModel> ObtenerPedidoPorIdAdmin(int pedidoId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("sp_ObtenerPedidoAdminPorId", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@pedido_id", pedidoId);

                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new PedidoAdminViewModel
                    {
                        PedidoId = reader.GetInt32(reader.GetOrdinal("pedido_id")),
                        Estado = reader.GetString(reader.GetOrdinal("estado")),
                        NumeroSeguimiento = reader.GetString(reader.GetOrdinal("numero_seguimiento")),
                        // Asegúrate que estos campos existan en tu SP
                        FechaPedido = reader.GetDateTime(reader.GetOrdinal("fecha_pedido")),
                        UsuarioId = reader.GetInt32(reader.GetOrdinal("usuario_id"))
                    };
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

    }
}