using Microsoft.Data.SqlClient;
using ProyectoPrograVI.Models;
using System.Data;
using System.Collections.Generic;

namespace ProyectoPrograVI.Data
{
    public class MetodoPagoADORepository
    {
        private readonly string _connectionString;

        public MetodoPagoADORepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<MetodoPago> GetAll(bool soloActivos = true)
        {
            var metodos = new List<MetodoPago>();

            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand("sp_listar_metodo_pago", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@solo_activos", soloActivos);

                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            metodos.Add(new MetodoPago
                            {
                                MetodoPagoId = Convert.ToInt32(reader["metodo_pago_id"]),
                                Nombre = reader["nombre"].ToString(),
                                Activo = Convert.ToBoolean(reader["activo"])
                            });
                        }
                    }
                }
            }

            return metodos;
        }

        public MetodoPago GetById(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand("sp_ObtenerMetodoPagoPorId", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@metodo_pago_id", id);

                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new MetodoPago
                            {
                                MetodoPagoId = Convert.ToInt32(reader["metodo_pago_id"]),
                                Nombre = reader["nombre"].ToString(),
                                Activo = Convert.ToBoolean(reader["activo"])
                            };
                        }
                    }
                }
            }

            return null;
        }

        public (bool success, int id, string message) Crear(MetodoPago metodoPago)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand("sp_guardar_metodo_pago", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@nombre", metodoPago.Nombre);
                    command.Parameters.AddWithValue("@activo", metodoPago.Activo);

                    var idParam = new SqlParameter("@metodo_pago_id", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(idParam);

                    var mensajeParam = new SqlParameter("@mensaje", SqlDbType.VarChar, 500)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(mensajeParam);

                    var errorParam = new SqlParameter("@error", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(errorParam);

                    connection.Open();
                    command.ExecuteNonQuery();

                    var success = Convert.ToInt32(errorParam.Value) == 0;
                    var message = mensajeParam.Value?.ToString() ?? string.Empty;
                    var id = success ? Convert.ToInt32(idParam.Value) : 0;

                    if (success)
                    {
                        string detalle = $"Insertó método de pago: ID={id}, Nombre={metodoPago.Nombre}, Estado={(metodoPago.Activo ? "Activo" : "Inactivo")}";
                        RegistrarBitacora("INSERT", "metodo_pago", id, detalle);
                    }

                    return (success, id, message);
                }
            }
        }

        public (bool success, string message) ActualizarEstado(int id, bool activo)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand("sp_actualizar_metodo_pago", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@metodo_pago_id", id);
                    command.Parameters.AddWithValue("@activo", activo);

                    var mensajeParam = new SqlParameter("@mensaje", SqlDbType.VarChar, 500)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(mensajeParam);

                    var errorParam = new SqlParameter("@error", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(errorParam);

                    connection.Open();
                    command.ExecuteNonQuery();

                    var success = Convert.ToInt32(errorParam.Value) == 0;
                    var message = mensajeParam.Value?.ToString() ?? string.Empty;

                    if (success)
                    {
                        string accion = activo ? "Activó" : "Desactivó";
                        string detalle = $"{accion} método de pago: ID={id}";
                        RegistrarBitacora("UPDATE", "metodo_pago", id, detalle);
                    }

                    return (success, message);
                }
            }
        }

        public void Eliminar(int id)
        {
            try
            {
                MetodoPago metodo = GetById(id);
                if (metodo == null)
                    throw new Exception("Método de pago no encontrado");

                if (VerificarUsoEnPedidos(id))
                {
                    // Lógica para desactivar en lugar de eliminar
                    var (success, message) = ActualizarEstado(id, false);
                    if (!success)
                        throw new Exception(message);
                }
                else
                {
                    using (var connection = new SqlConnection(_connectionString))
                    using (var command = new SqlCommand("sp_EliminarMetodoPago", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@metodo_pago_id", id);
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // Registra el error en bitácora si es necesario
                throw new Exception($"Error al eliminar método de pago: {ex.Message}", ex);
            }
        }

        public bool VerificarUsoEnPedidos(int metodoPagoId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand(
                    "SELECT COUNT(*) FROM pedido WHERE metodo_pago_id = @id", connection))
                {
                    command.Parameters.AddWithValue("@id", metodoPagoId);

                    connection.Open();
                    return (int)command.ExecuteScalar() > 0;
                }
            }
        }

        private void RegistrarBitacora(string operacion, string tabla, int idRegistro, string detalle)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(
                @"INSERT INTO BitacoraMovimientos
                (TipoOperacion, NombreTabla, IdRegistroAfectado, Detalle)
                VALUES (@operacion, @tabla, @id, @detalle)", connection))
            {
                command.Parameters.AddWithValue("@operacion", operacion);
                command.Parameters.AddWithValue("@tabla", tabla);
                command.Parameters.AddWithValue("@id", idRegistro);
                command.Parameters.AddWithValue("@detalle", detalle);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }
    }
}