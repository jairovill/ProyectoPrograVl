using Microsoft.Data.SqlClient;
using ProyectoPrograVI.Models;
using System.Data;

namespace ProyectoPrograVI.Data
{
    public class RolADORepository
    {
        private readonly string _connectionString = "Server=LAPTOP-G1D4D7M3\\SQLEXPRES;Database=ProyectoPrograVI;Trusted_Connection=True;TrustServerCertificate=True;";

        public List<Rol> GetAll()
        {
            var lista = new List<Rol>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_listar_rol", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    lista.Add(new Rol
                    {
                        rol_id = Convert.ToInt32(reader["rol_id"]),
                        nombre = reader["nombre"].ToString()
                    });
                }
            }

            return lista;
        }

        public Rol GetById(int id)
        {
            Rol rol = null;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_obtener_rol_por_id", conn); // Corregí la coma que había después del nombre
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    rol = new Rol
                    {
                        rol_id = Convert.ToInt32(reader["rol_id"]),
                        nombre = reader["nombre"].ToString()
                    };
                }
            }

            return rol;
        }

        public void Insert(Rol rol)
        {
            int respuesta = 0;
            int newId = 0; // Variable para capturar el ID

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_guardar_rol", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@p_nombre", rol.nombre);

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
                    cmd.CommandText = "SELECT MAX(rol_id) FROM rol";
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.Clear();
                    newId = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }

            if (respuesta == 1 && newId > 0)
            {
                string detalle = $"Insertó rol: ID={newId}, Nombre={rol.nombre}";
                RegistrarBitacora("INSERT", "rol", newId, detalle);
            }
        }

        public void Update(Rol rol)
        {
            int respuesta = 0;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_actualizar_rol", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@p_rol_id", rol.rol_id);
                cmd.Parameters.AddWithValue("@p_nombre", rol.nombre);

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
                string detalle = $"Actualizó rol: ID={rol.rol_id}, Nombre={rol.nombre}";
                RegistrarBitacora("UPDATE", "rol", rol.rol_id, detalle);
            }
        }

        public void Delete(int id)
        {
            Rol rol = GetById(id);

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_EliminarRol", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@rol_id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            string detalle = $"Eliminó rol: ID={rol.rol_id}, Nombre={rol.nombre}";
            RegistrarBitacora("DELETE", "rol", id, detalle);
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