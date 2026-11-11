using Microsoft.Data.SqlClient;
using ProyectoPrograVI.Models;
using System.Data;



namespace ProyectoPrograVI.Data
{
    public class CategoriaADORepository
    {
        private readonly string _connectionString = "Server=LAPTOP-G1D4D7M3\\SQLEXPRES;Database=ProyectoPrograVI;Trusted_Connection=True;TrustServerCertificate=True;";

        public List<Categoria> GetAll()
        {
            var lista = new List<Categoria>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_listar_categoria", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    lista.Add(new Categoria
                    {
                        categoria_id = Convert.ToInt32(reader["categoria_id"]),
                        nombre = reader["nombre"].ToString()
                    });
                }
            }

            return lista;
        }

        public Categoria GetById(int id)
        {
            Categoria categoria = null;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_ObtenerCategoriaPorId", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    categoria = new Categoria
                    {
                        categoria_id = (int)reader["categoria_id"],
                        nombre = reader["nombre"].ToString()
                    };
                }
            }

            return categoria;
        }

        public void Insert(Categoria categoria)
        {
            int respuesta = 0;
            int newId = 0; // Variable para capturar el ID

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_guardar_categoria", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@p_nombre", categoria.nombre);

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
                    cmd.CommandText = "SELECT MAX(categoria_id) FROM categoria";
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.Clear();
                    newId = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }

            if (respuesta == 1 && newId > 0)
            {
                string detalle = $"Insertó categoría: ID={newId}, Nombre={categoria.nombre}";
                RegistrarBitacora("INSERT", "categoria", newId, detalle);
            }
        }

        public void Update(Categoria categoria)
        {
            int respuesta = 0;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_actualizar_categoria", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@p_categoria_id", categoria.categoria_id);
                cmd.Parameters.AddWithValue("@p_nombre", categoria.nombre);

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
                string detalle = $"Actualizó categoría: ID={categoria.categoria_id}, Nombre={categoria.nombre}";
                RegistrarBitacora("UPDATE", "categoria", categoria.categoria_id, detalle);
            }
        }


        public void Delete(int id)
        {
            Categoria categoria = GetById(id);

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_EliminarCategoria", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@categoria_id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            string detalle = $"Eliminó categoría: ID={categoria.categoria_id}, Nombre={categoria.nombre}";
            RegistrarBitacora("DELETE", "categoria", id, detalle);
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
