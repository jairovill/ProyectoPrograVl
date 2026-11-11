using Microsoft.Data.SqlClient;
using ProyectoPrograVI.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace ProyectoPrograVI.Data
{
    public class CarritoADORepository
    {
        private readonly string _connectionString;

        public CarritoADORepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public bool AgregarItem(int usuarioId, int libroId, int cantidad, out string mensaje)
        {
            bool resultado = false;
            mensaje = string.Empty;

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var cmd = new SqlCommand("sp_guardar_item_carrito", conn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@usuario_id", usuarioId);
                    cmd.Parameters.AddWithValue("@libro_id", libroId);
                    cmd.Parameters.AddWithValue("@cantidad", cantidad);
                    // No enviar @estado ya que tiene valor por defecto en el SP

                    var mensajeParam = new SqlParameter("@mensaje", SqlDbType.VarChar, 500)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(mensajeParam);

                    var errorParam = new SqlParameter("@error", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(errorParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    resultado = (int)errorParam.Value == 0;
                    mensaje = mensajeParam.Value.ToString();
                }
            }
            catch (Exception ex)
            {
                mensaje = $"Error al agregar item al carrito: {ex.Message}";
                resultado = false;
            }

            return resultado;
        }

        public IEnumerable<ItemCarrito> ObtenerItems(int usuarioId)
        {
            var items = new List<ItemCarrito>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var cmd = new SqlCommand("sp_ObtenerCarritoUsuario", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@usuario_id", usuarioId);

                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
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
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener items del carrito: {ex.Message}");
            }

            return items;
        }

        public decimal CalcularTotal(int usuarioId)
        {
            decimal total = 0;

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var cmd = new SqlCommand("sp_CalcularTotalCarrito", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@usuario_id", usuarioId);

                    var totalParam = new SqlParameter("@total", SqlDbType.Decimal)
                    {
                        Precision = 10,
                        Scale = 2,
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(totalParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    total = Convert.ToDecimal(totalParam.Value);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al calcular total del carrito: {ex.Message}");
            }

            return total;
        }

        public bool ActualizarEstado(int carritoId, string estado, out string mensaje)
        {
            bool resultado = false;
            mensaje = string.Empty;

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var cmd = new SqlCommand("sp_ActualizarEstadoCarrito", conn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@carrito_id", carritoId);
                    cmd.Parameters.AddWithValue("@estado", estado);

                    var mensajeParam = new SqlParameter("@mensaje", SqlDbType.VarChar, 500)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(mensajeParam);

                    var errorParam = new SqlParameter("@error", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(errorParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    resultado = (int)errorParam.Value == 0;
                    mensaje = mensajeParam.Value.ToString();
                }
            }
            catch (Exception ex)
            {
                mensaje = $"Error al actualizar estado: {ex.Message}";
                resultado = false;
            }

            return resultado;
        }

        public bool EliminarItem(int carritoId, out string mensaje)
        {
            bool resultado = false;
            mensaje = string.Empty;

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var cmd = new SqlCommand("DELETE FROM carrito_compra WHERE carrito_id = @carrito_id", conn);
                    cmd.Parameters.AddWithValue("@carrito_id", carritoId);

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    resultado = rowsAffected > 0;
                    mensaje = resultado ? "Item eliminado correctamente" : "No se encontró el item especificado";
                }
            }
            catch (Exception ex)
            {
                mensaje = $"Error al eliminar item: {ex.Message}";
                resultado = false;
            }

            return resultado;
        }

        public ItemCarrito ObtenerItemCarrito(int carritoId, int usuarioId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("sp_ObtenerItemCarritoPorId", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@carrito_id", carritoId);
                    cmd.Parameters.AddWithValue("@usuario_id", usuarioId);

                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
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
            }
            catch (Exception ex)
            {
                // Log error
                return null;
            }

            return null;
        }

        public (bool success, string message) ActualizarCantidad(int carritoId, int cantidad, int usuarioId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("sp_actualizar_carrito", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@carrito_id", carritoId);
                    cmd.Parameters.AddWithValue("@nueva_cantidad", cantidad);
                    cmd.Parameters.AddWithValue("@usuario_id", usuarioId);

                    var mensajeParam = new SqlParameter("@mensaje", SqlDbType.VarChar, 500)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(mensajeParam);

                    var errorParam = new SqlParameter("@error", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(errorParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    bool success = (int)errorParam.Value == 0;
                    string message = mensajeParam.Value?.ToString();

                    return (success, message);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error al actualizar cantidad: {ex.Message}");
            }
        }

        public bool VerificarDisponibilidad(int libroId, int cantidadDeseada, out string mensaje)
        {
            bool disponible = false;
            mensaje = string.Empty;

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var cmd = new SqlCommand("SELECT unidades_disponibles FROM libro WHERE libro_id = @libro_id", conn);
                    cmd.Parameters.AddWithValue("@libro_id", libroId);

                    conn.Open();
                    var unidades = (int)cmd.ExecuteScalar();

                    disponible = unidades >= cantidadDeseada;
                    mensaje = disponible ? "Disponible" : $"Solo quedan {unidades} unidades disponibles";
                }
            }
            catch (Exception ex)
            {
                mensaje = $"Error al verificar disponibilidad: {ex.Message}";
                disponible = false;
            }

            return disponible;
        }
    }
}