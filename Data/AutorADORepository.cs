using Microsoft.Data.SqlClient;
using System.Data;
using ProyectoPrograVI.Models;

namespace ProyectoPrograVI.Data
{
    public class AutorADORepository
    {
        private readonly string _connectionString = "Server=LAPTOP-G1D4D7M3\\SQLEXPRES;Database=ProyectoPrograVI;Trusted_Connection=True;TrustServerCertificate=True;";

        public List<Autor> GetAll()
        {
            var lista = new List<Autor>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_listar_autor", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    lista.Add(new Autor
                    {
                        autor_id = Convert.ToInt32(reader["autor_id"]),
                        persona_id = Convert.ToInt32(reader["persona_id"]),
                        fecha_defuncion = reader["fecha_defuncion"] == DBNull.Value
                            ? (DateTime?)null
                            : Convert.ToDateTime(reader["fecha_defuncion"]),
                        identificacion = reader["identificacion"].ToString(),
                        nombre = reader["nombre"].ToString(),
                        apellido1 = reader["apellido1"].ToString(),
                        apellido2 = reader["apellido2"].ToString(),
                        fecha_nacimiento = reader["fecha_nacimiento"] == DBNull.Value
                            ? (DateTime?)null
                            : Convert.ToDateTime(reader["fecha_nacimiento"])
                    });
                }
            }

            return lista;
        }

        public Autor GetById(int id)
        {
            Autor autor = null;
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_ObtenerAutorPorId", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    autor = new Autor
                    {
                        autor_id = (int)reader["autor_id"],
                        persona_id = (int)reader["persona_id"],
                        fecha_defuncion = reader["fecha_defuncion"] == DBNull.Value ? null : (DateTime?)reader["fecha_defuncion"],
                        identificacion = reader["identificacion"].ToString(),
                        nombre = reader["nombre"].ToString(),
                        apellido1 = reader["apellido1"].ToString(),
                        apellido2 = reader["apellido2"].ToString(),
                        fecha_nacimiento = reader["fecha_nacimiento"] == DBNull.Value ? null : (DateTime?)reader["fecha_nacimiento"]
                    };
                }
            }
            return autor;
        }

        public void Insert(Autor autor)
        {
            int respuesta = 0;
            int newId = 0; // Variable para capturar el ID

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_guardar_autor", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                // No enviar persona_id si es nuevo (0)
                cmd.Parameters.AddWithValue("@p_persona_id", autor.persona_id > 0 ? autor.persona_id : 0);
                cmd.Parameters.AddWithValue("@p_identificacion", autor.identificacion ?? string.Empty);
                cmd.Parameters.AddWithValue("@p_nombre", autor.nombre ?? string.Empty);
                cmd.Parameters.AddWithValue("@p_apellido1", autor.apellido1 ?? string.Empty);
                cmd.Parameters.AddWithValue("@p_apellido2", autor.apellido2 ?? string.Empty);
                cmd.Parameters.AddWithValue("@p_fecha_nacimiento", autor.fecha_nacimiento.HasValue ? (object)autor.fecha_nacimiento.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@p_fecha_defuncion", autor.fecha_defuncion.HasValue ? (object)autor.fecha_defuncion.Value : DBNull.Value);

                // Output
                SqlParameter pRespuesta = new SqlParameter("@p_respuesta", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(pRespuesta);

                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    respuesta = Convert.ToInt32(pRespuesta.Value);

                    // Obtenemos el ID del nuevo registro
                    if (respuesta == 1)
                    {
                        cmd.CommandText = "SELECT MAX(autor_id) FROM autor";
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.Clear();
                        newId = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
                catch (SqlException ex)
                {
                    // Registrar error detallado
                    Console.WriteLine($"Error SQL: {ex.Message}");
                    respuesta = 0;
                }
            }

            if (respuesta == 1 && newId > 0)
            {
                string detalle = $"Insertó autor: ID={newId}, Nombre={autor.nombre} {autor.apellido1} {autor.apellido2}, Identificación={autor.identificacion}";
                RegistrarBitacora("INSERT", "autor", newId, detalle);
            }
        }

        public void Update(Autor autor)
        {
            int respuesta = 0;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_actualizar_autor", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@p_autor_id", autor.autor_id);
                cmd.Parameters.AddWithValue("@p_persona_id", autor.persona_id);
                cmd.Parameters.AddWithValue("@p_identificacion", autor.identificacion);
                cmd.Parameters.AddWithValue("@p_nombre", autor.nombre);
                cmd.Parameters.AddWithValue("@p_apellido1", autor.apellido1);
                cmd.Parameters.AddWithValue("@p_apellido2", autor.apellido2);
                cmd.Parameters.AddWithValue("@p_fecha_nacimiento", (object)autor.fecha_nacimiento ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@p_fecha_defuncion", (object)autor.fecha_defuncion ?? DBNull.Value);

                // Output
                SqlParameter pRespuesta = new SqlParameter("@p_respuesta", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(pRespuesta);

                conn.Open();
                cmd.ExecuteNonQuery();

                respuesta = Convert.ToInt32(pRespuesta.Value);
            }

            if (respuesta == 1)
            {
                string detalle = $"Actualizó autor: ID={autor.autor_id}, Nombre={autor.nombre} {autor.apellido1} {autor.apellido2}, Identificación={autor.identificacion}";
                RegistrarBitacora("UPDATE", "autor", autor.autor_id, detalle);
            }
        }

        public void Delete(int id)
        {
            Autor autor = GetById(id);

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_EliminarAutor", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@autor_id", id);

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            string detalle = $"Eliminó autor: ID={autor.autor_id}, Nombre={autor.nombre} {autor.apellido1} {autor.apellido2}, Identificación={autor.identificacion}";
            RegistrarBitacora("DELETE", "autor", id, detalle);
        }

        private void RegistrarBitacora(string operacion, string tabla, int idRegistro, string detalle)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                var query = @"INSERT INTO BitacoraMovimientos 
                            (FechaMovimiento, TipoOperacion, NombreTabla, IdRegistroAfectado, Detalle)
                            VALUES 
                            (GETDATE(), @operacion, @tabla, @id, @detalle)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@operacion", operacion);
                    cmd.Parameters.AddWithValue("@tabla", tabla);
                    cmd.Parameters.AddWithValue("@id", idRegistro);
                    cmd.Parameters.AddWithValue("@detalle", detalle);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}