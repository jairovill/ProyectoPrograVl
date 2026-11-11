using Microsoft.Data.SqlClient;
using ProyectoPrograVI.Models;
using System.Data;

namespace ProyectoPrograVI.Data
{
    public class DocumentoIdentidadADORepository
    {
        private readonly string _connectionString = "Server=LAPTOP-G1D4D7M3\\SQLEXPRES;Database=ProyectoPrograVI;Trusted_Connection=True;TrustServerCertificate=True;";

        public List<DocumentoIdentidad> GetAll()
        {
            var lista = new List<DocumentoIdentidad>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_listar_documento_identidad", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    lista.Add(new DocumentoIdentidad
                    {
                        documento_identidad_id = Convert.ToInt32(reader["documento_identidad_id"]),
                        nombre = reader["nombre"].ToString()
                    });
                }
            }

            return lista;
        }

        public DocumentoIdentidad GetById(int id)
        {
            DocumentoIdentidad documentoidentidad = null;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_ObtenerDocumentoIdentidadPorId", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    documentoidentidad = new DocumentoIdentidad
                    {
                        documento_identidad_id = Convert.ToInt32(reader["documento_identidad_id"]),
                        nombre = reader["nombre"].ToString()
                    };
                }
            }

            return documentoidentidad;
        }

        public void Insert(DocumentoIdentidad documentoidentidad)
        {
            int respuesta = 0;
            int newId = 0; // Variable para capturar el ID

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_guardar_documento_identidad", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@p_nombre", documentoidentidad.nombre);

                SqlParameter pRespuesta = new SqlParameter("@p_respuesta", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(pRespuesta);

                conn.Open();
                cmd.ExecuteNonQuery();

                respuesta = Convert.ToInt32(pRespuesta.Value);

                // Obtenemos el ID del nuevo registro
                if (respuesta == 1)
                {
                    cmd.CommandText = "SELECT MAX(documento_identidad_id) FROM documento_identidad";
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.Clear();
                    newId = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }

            if (respuesta == 1 && newId > 0)
            {
                string detalle = $"Insertó documento identidad: ID={newId}, Nombre={documentoidentidad.nombre}";
                RegistrarBitacora("INSERT", "documento_identidad", newId, detalle);
            }
        }

        public void Update(DocumentoIdentidad documentoidentidad)
        {
            int respuesta = 0;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_actualizar_documento_identidad", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@p_documento_identidad_id", documentoidentidad.documento_identidad_id);
                cmd.Parameters.AddWithValue("@p_nombre", documentoidentidad.nombre);

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
                string detalle = $"Actualizó documento identidad: ID={documentoidentidad.documento_identidad_id}, Nombre={documentoidentidad.nombre}";
                RegistrarBitacora("UPDATE", "documento_identidad", documentoidentidad.documento_identidad_id, detalle);
            }
        }

        private bool VerificarSiDocumentoEsUsado(int documentoId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT COUNT(*) FROM usuario WHERE documento_identidad_id = @id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", documentoId);

                conn.Open();
                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }

        public void Delete(int id)
        {
            DocumentoIdentidad documento = GetById(id);

            // Verificar si el documento está en uso por usuarios
            if (VerificarSiDocumentoEsUsado(id))
            {
                throw new Exception("No se puede eliminar el documento porque está en uso por uno o más usuarios.");
            }

            // Si no está en uso, proceder a eliminar
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_EliminarDocumentoIdentidad", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@documento_identidad_id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            string detalle = $"Eliminó documento identidad: ID={id}, Nombre={documento.nombre}";
            RegistrarBitacora("DELETE", "documento_identidad", id, detalle);
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