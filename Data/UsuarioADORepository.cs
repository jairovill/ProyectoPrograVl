using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using ProyectoPrograVI.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace ProyectoPrograVI.Data
{
    public class UsuarioADORepository
    {
        private readonly string _connectionString;

        public UsuarioADORepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<Usuario> GetAll()
        {
            var usuarios = new List<Usuario>();

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("sp_listar_usuario", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        usuarios.Add(new Usuario
                        {
                            usuario_id = Convert.ToInt32(reader["usuario_id"]),
                            rol_id = Convert.ToInt32(reader["rol_id"]),
                            rol_nombre = reader["rol_nombre"].ToString(),
                            persona_id = Convert.ToInt32(reader["persona_id"]),
                            documento_identidad_id = Convert.ToInt32(reader["documento_identidad_id"]),
                            documento_nombre = reader["documento_nombre"].ToString(),
                            email = reader["email"].ToString(),
                            estado = reader["estado"].ToString(),
                            identificacion = reader["identificacion"].ToString(),
                            nombre = reader["nombre"].ToString(),
                            apellido1 = reader["apellido1"].ToString(),
                            apellido2 = reader["apellido2"].ToString(),
                            fecha_nacimiento = reader["fecha_nacimiento"] == DBNull.Value ?
                                (DateTime?)null : Convert.ToDateTime(reader["fecha_nacimiento"])
                        });
                    }
                }
            }

            return usuarios;
        }

        public Usuario GetById(int id)
        {
            Usuario usuario = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("sp_ObtenerUsuarioCompletoPorId", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        usuario = new Usuario
                        {
                            usuario_id = Convert.ToInt32(reader["usuario_id"]),
                            rol_id = Convert.ToInt32(reader["rol_id"]),
                            rol_nombre = reader["rol_nombre"].ToString(),
                            persona_id = Convert.ToInt32(reader["persona_id"]),
                            documento_identidad_id = Convert.ToInt32(reader["documento_identidad_id"]),
                            documento_nombre = reader["documento_nombre"].ToString(),
                            email = reader["email"].ToString(),
                            contrasena = reader["contrasena"].ToString(),
                            estado = reader["estado"].ToString(),
                            identificacion = reader["identificacion"].ToString(),
                            nombre = reader["nombre"].ToString(),
                            apellido1 = reader["apellido1"].ToString(),
                            apellido2 = reader["apellido2"].ToString(),
                            fecha_nacimiento = reader["fecha_nacimiento"] == DBNull.Value ?
                                (DateTime?)null : Convert.ToDateTime(reader["fecha_nacimiento"])
                        };
                    }
                }
            }

            return usuario;
        }

        public int Create(Usuario usuario)
        {
            int respuesta = 0;
            int usuarioId = 0;

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("sp_guardar_usuario", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@p_rol_id", usuario.rol_id);
                cmd.Parameters.AddWithValue("@p_documento_identidad_id", usuario.documento_identidad_id);
                cmd.Parameters.AddWithValue("@p_email", usuario.email);
                cmd.Parameters.AddWithValue("@p_contrasena", usuario.contrasena);
                cmd.Parameters.AddWithValue("@p_estado", usuario.estado ?? "activo");
                cmd.Parameters.AddWithValue("@p_identificacion", usuario.identificacion);
                cmd.Parameters.AddWithValue("@p_nombre", usuario.nombre);
                cmd.Parameters.AddWithValue("@p_apellido1", usuario.apellido1);
                cmd.Parameters.AddWithValue("@p_apellido2", usuario.apellido2 ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p_fecha_nacimiento", usuario.fecha_nacimiento ?? (object)DBNull.Value);

                var respuestaParam = new SqlParameter("@p_respuesta", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(respuestaParam);

                var usuarioIdParam = new SqlParameter("@p_usuario_id", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(usuarioIdParam);

                conn.Open();
                cmd.ExecuteNonQuery();

                respuesta = (int)respuestaParam.Value;
                usuarioId = (int)usuarioIdParam.Value;

                if (respuesta == 1)
                {
                    string detalle = $"Nuevo usuario creado: ID={usuarioId}, Nombre={usuario.nombre} {usuario.apellido1}, Email={usuario.email}, Rol={usuario.rol_id}";
                    RegistrarBitacora("INSERT", "usuario", usuarioId, detalle);
                }
            }

            return respuesta;
        }

        public int Update(Usuario usuario)
        {
            int respuesta = 0;

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("sp_actualizar_usuario", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@p_usuario_id", usuario.usuario_id);
                cmd.Parameters.AddWithValue("@p_rol_id", usuario.rol_id);
                cmd.Parameters.AddWithValue("@p_persona_id", usuario.persona_id);
                cmd.Parameters.AddWithValue("@p_documento_identidad_id", usuario.documento_identidad_id);
                cmd.Parameters.AddWithValue("@p_email", usuario.email);
                cmd.Parameters.AddWithValue("@p_contrasena", usuario.contrasena); // "no-change" o la nueva contraseña
                cmd.Parameters.AddWithValue("@p_estado", usuario.estado);
                cmd.Parameters.AddWithValue("@p_identificacion", usuario.identificacion);
                cmd.Parameters.AddWithValue("@p_nombre", usuario.nombre);
                cmd.Parameters.AddWithValue("@p_apellido1", usuario.apellido1);
                cmd.Parameters.AddWithValue("@p_apellido2", usuario.apellido2 ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p_fecha_nacimiento", usuario.fecha_nacimiento ?? (object)DBNull.Value);

                var respuestaParam = new SqlParameter("@p_respuesta", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(respuestaParam);

                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    respuesta = (int)respuestaParam.Value;

                    if (respuesta == 1)
                    {
                        string detalle = $"Usuario actualizado: ID={usuario.usuario_id}, Nombre={usuario.nombre} {usuario.apellido1}, Cambios: Rol={usuario.rol_id}, Estado={usuario.estado}";
                        RegistrarBitacora("UPDATE", "usuario", usuario.usuario_id, detalle);
                    }
                }
                catch (SqlException ex)
                {
                    // Log del error
                    Console.WriteLine($"Error SQL al actualizar usuario: {ex.Message}");
                    respuesta = 0;
                }
            }

            return respuesta;
        }

        public bool Delete(int id)
        {
            Usuario usuario = GetById(id);
            if (usuario == null) return false;

            int respuesta = 0;

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("sp_EliminarUsuario", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@usuario_id", id);

                var respuestaParam = new SqlParameter("@p_respuesta", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(respuestaParam);

                conn.Open();
                cmd.ExecuteNonQuery();

                respuesta = (int)respuestaParam.Value;
            }

            if (respuesta == 1)
            {
                string detalle = $"Usuario eliminado: ID={usuario.usuario_id}, Nombre={usuario.nombre} {usuario.apellido1}, Email={usuario.email}";
                RegistrarBitacora("DELETE", "usuario", id, detalle);
                return true;
            }

            return false;
        }

        public IEnumerable<SelectListItem> GetRoles()
        {
            var roles = new List<SelectListItem>();

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("sp_listar_rol", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        roles.Add(new SelectListItem
                        {
                            Value = reader["rol_id"].ToString(),
                            Text = reader["nombre"].ToString()
                        });
                    }
                }
            }

            return roles;
        }

        public IEnumerable<SelectListItem> GetDocumentosIdentidad()
        {
            var documentos = new List<SelectListItem>();

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("sp_listar_documento_identidad", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        documentos.Add(new SelectListItem
                        {
                            Value = reader["documento_identidad_id"].ToString(),
                            Text = reader["nombre"].ToString()
                        });
                    }
                }
            }

            return documentos;
        }

        public IEnumerable<SelectListItem> GetEstados()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "activo", Text = "Activo" },
                new SelectListItem { Value = "inactivo", Text = "Inactivo" }
            };
        }

        private void RegistrarBitacora(string operacion, string tabla, int idRegistro, string detalle)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand(
                    @"INSERT INTO BitacoraMovimientos 
                    (FechaMovimiento, TipoOperacion, NombreTabla, IdRegistroAfectado, Detalle) 
                    VALUES 
                    (GETDATE(), @operacion, @tabla, @idRegistro, @detalle)", conn);

                cmd.Parameters.AddWithValue("@operacion", operacion);
                cmd.Parameters.AddWithValue("@tabla", tabla);
                cmd.Parameters.AddWithValue("@idRegistro", idRegistro);
                cmd.Parameters.AddWithValue("@detalle", detalle);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public Usuario ValidarLogin(string email, string contrasena, out int respuesta)
        {
            Usuario usuario = null;
            respuesta = -1;

            using (var conn = new SqlConnection(_connectionString))
            {
                // Validar credenciales
                var cmdValidar = new SqlCommand("SP_ValidarUsuario", conn);
                cmdValidar.CommandType = CommandType.StoredProcedure;

                cmdValidar.Parameters.AddWithValue("@Email", email);
                cmdValidar.Parameters.AddWithValue("@Contrasena", contrasena);

                conn.Open();
                using (var reader = cmdValidar.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        usuario = new Usuario
                        {
                            usuario_id = (int)reader["usuario_id"],
                            rol_id = (int)reader["rol_id"],
                            rol_nombre = reader["rol"].ToString(),
                            nombre = reader["nombre"].ToString(),
                            apellido1 = reader["apellido1"].ToString(),
                            apellido2 = reader["apellido2"] != DBNull.Value ? reader["apellido2"].ToString() : null,
                            email = reader["email"].ToString(),
                            estado = reader["estado"].ToString()
                        };

                        respuesta = usuario.estado == "activo" ? 1 : -3;

                        // Registrar acceso en bitácora solo si es exitoso
                        if (respuesta == 1)
                        {
                            string detalle = $"Inicio de sesión exitoso para el usuario: {usuario.nombre} {usuario.apellido1}";
                            RegistrarBitacora("LOGIN", "usuario", usuario.usuario_id, detalle);
                        }
                    }
                }

                // Obtener datos adicionales si el login fue exitoso
                if (usuario != null && respuesta == 1)
                {
                    var cmdDatos = new SqlCommand(@"
                        SELECT 
                            u.persona_id,
                            u.documento_identidad_id,
                            d.nombre AS documento_nombre,
                            p.identificacion,
                            p.fecha_nacimiento
                        FROM usuario u
                        INNER JOIN persona p ON u.persona_id = p.persona_id
                        INNER JOIN documento_identidad d ON u.documento_identidad_id = d.documento_identidad_id
                        WHERE u.usuario_id = @usuarioId", conn);

                    cmdDatos.Parameters.AddWithValue("@usuarioId", usuario.usuario_id);

                    using (var reader = cmdDatos.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            usuario.persona_id = (int)reader["persona_id"];
                            usuario.documento_identidad_id = (int)reader["documento_identidad_id"];
                            usuario.documento_nombre = reader["documento_nombre"].ToString();
                            usuario.identificacion = reader["identificacion"].ToString();
                            usuario.fecha_nacimiento = reader["fecha_nacimiento"] != DBNull.Value ?
                                (DateTime?)reader["fecha_nacimiento"] : null;
                        }
                    }
                }
            }

            return usuario;
        }
    }
}