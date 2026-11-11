using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using ProyectoPrograVI.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace ProyectoPrograVI.Data
{
    public class LibroADORepository
    {
        private readonly string _connectionString;

        public LibroADORepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<Libro> BuscarLibros(string termino = null, int? categoriaId = null,
            int? autorId = null, decimal? precioMin = null, decimal? precioMax = null,
            int pagina = 1, int registrosPorPagina = 10)
        {
            var libros = new List<Libro>();

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("sp_BusquedaLibro", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@termino_busqueda", termino ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@categoria_id", categoriaId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@autor_id", autorId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@precio_min", precioMin ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@precio_max", precioMax ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@pagina", pagina);
                cmd.Parameters.AddWithValue("@registros_por_pagina", registrosPorPagina);

                var totalParam = new SqlParameter("@total_registros", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(totalParam);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        libros.Add(new Libro
                        {
                            LibroId = Convert.ToInt32(reader["libro_id"]),
                            Titulo = reader["titulo"].ToString(),
                            CategoriaId = Convert.ToInt32(reader["categoria_id"]),
                            CategoriaNombre = reader["categoria_nombre"].ToString(),
                            AutorId = Convert.ToInt32(reader["autor_id"]),
                            AutorNombre = reader["autor_nombre"].ToString(),
                            UnidadesDisponibles = Convert.ToInt32(reader["unidades_disponibles"]),
                            Precio = Convert.ToDecimal(reader["precio"]),
                            Descripcion = reader["descripcion_corta"].ToString(),
                            ImagenUrl = reader["imagen_url"].ToString(),
                            ISBN = reader["isbn"].ToString(),
                            Editorial = reader["editorial"].ToString(),
                            Estado = reader["estado"].ToString()
                        });
                    }
                }
            }

            return libros;
        }

        public Libro ObtenerPorId(int id, bool incluirInactivos = false)
        {
            Libro libro = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("sp_ObtenerLibroPorId", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@libro_id", id);
                cmd.Parameters.AddWithValue("@incluir_inactivos", incluirInactivos);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        libro = new Libro
                        {
                            LibroId = Convert.ToInt32(reader["libro_id"]),
                            Titulo = reader["titulo"].ToString(),
                            CategoriaId = Convert.ToInt32(reader["categoria_id"]),
                            CategoriaNombre = reader["categoria_nombre"].ToString(),
                            AutorId = Convert.ToInt32(reader["autor_id"]),
                            AutorNombre = reader["autor_nombre"].ToString(),
                            UnidadesDisponibles = Convert.ToInt32(reader["unidades_disponibles"]),
                            Precio = Convert.ToDecimal(reader["precio"]),
                            Descripcion = reader["descripcion"].ToString(),
                            ImagenUrl = reader["imagen_url"].ToString(),
                            ISBN = reader["isbn"].ToString(),
                            Editorial = reader["editorial"].ToString(),
                            Estado = reader["estado"].ToString()
                        };
                    }
                }
            }

            return libro;
        }

        public int Insertar(Libro libro, out string mensaje)
        {
            int libroId = 0;
            mensaje = string.Empty;
            int error = 0;

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("sp_Insertar_Libro", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@titulo", libro.Titulo);
                cmd.Parameters.AddWithValue("@categoria_id", libro.CategoriaId);
                cmd.Parameters.AddWithValue("@autor_id", libro.AutorId);
                cmd.Parameters.AddWithValue("@unidades_disponibles", libro.UnidadesDisponibles);
                cmd.Parameters.AddWithValue("@precio", libro.Precio);
                cmd.Parameters.AddWithValue("@descripcion", libro.Descripcion ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@imagen_url", libro.ImagenUrl ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@isbn", libro.ISBN ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@editorial", libro.Editorial ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@estado", libro.Estado);

                var libroIdParam = new SqlParameter("@libro_id", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(libroIdParam);

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

                libroId = (int)libroIdParam.Value;
                mensaje = mensajeParam.Value.ToString();
                error = (int)errorParam.Value;

                if (error == 0)
                {
                    RegistrarBitacora("INSERT", "libro", libroId,
                        $"Nuevo libro creado: {libro.Titulo} (ID: {libroId})");
                }
            }

            return error == 0 ? libroId : 0;
        }

        public bool Actualizar(Libro libro, out string mensaje)
        {
            bool resultado = false;
            mensaje = string.Empty;
            int error = 0;

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("sp_actualizar_libro", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@libro_id", libro.LibroId);
                cmd.Parameters.AddWithValue("@titulo", libro.Titulo);
                cmd.Parameters.AddWithValue("@categoria_id", libro.CategoriaId);
                cmd.Parameters.AddWithValue("@autor_id", libro.AutorId);
                cmd.Parameters.AddWithValue("@unidades_disponibles", libro.UnidadesDisponibles);
                cmd.Parameters.AddWithValue("@precio", libro.Precio);
                cmd.Parameters.AddWithValue("@descripcion", libro.Descripcion ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@imagen_url", libro.ImagenUrl ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@isbn", libro.ISBN ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@editorial", libro.Editorial ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@estado", libro.Estado);

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

                mensaje = mensajeParam.Value.ToString();
                error = (int)errorParam.Value;
                resultado = error == 0;

                if (resultado)
                {
                    RegistrarBitacora("UPDATE", "libro", libro.LibroId,
                        $"Libro actualizado: {libro.Titulo} (ID: {libro.LibroId})");
                }
            }

            return resultado;
        }

        public bool ActualizarEstado(int libroId, string estado, out string mensaje)
        {
            bool resultado = false;
            mensaje = string.Empty;
            int error = 0;

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("sp_ActualizarEstadoLibro", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@libro_id", libroId);
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

                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();

                    mensaje = mensajeParam.Value?.ToString() ?? "Estado actualizado";
                    error = (int)errorParam.Value;
                    resultado = error == 0;

                    if (resultado)
                    {
                        // Obtener el título del libro para el registro en bitácora
                        var libro = ObtenerPorId(libroId);
                        string tituloLibro = libro?.Titulo ?? "Libro desconocido";

                        RegistrarBitacora("UPDATE", "libro", libroId,
                            $"Estado del libro cambiado a '{estado}': {tituloLibro} (ID: {libroId})");
                    }
                }
                catch (SqlException ex)
                {
                    mensaje = $"Error de base de datos: {ex.Message}";
                    resultado = false;
                }
                catch (Exception ex)
                {
                    mensaje = $"Error inesperado: {ex.Message}";
                    resultado = false;
                }
            }

            return resultado;
        }

        public IEnumerable<SelectListItem> ObtenerCategorias()
        {
            var categorias = new List<SelectListItem>();

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("sp_ListarCategorias", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        categorias.Add(new SelectListItem
                        {
                            Value = reader["categoria_id"].ToString(),
                            Text = reader["nombre"].ToString()
                        });
                    }
                }
            }

            return categorias;
        }

        public IEnumerable<SelectListItem> ObtenerAutores()
        {
            var autores = new List<SelectListItem>();

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("sp_ListarAutores", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        autores.Add(new SelectListItem
                        {
                            Value = reader["autor_id"].ToString(),
                            Text = reader["nombre_completo"].ToString()
                        });
                    }
                }
            }

            return autores;
        }

        private void RegistrarBitacora(string operacion, string tabla, int idRegistro, string detalle)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand(
                    "INSERT INTO BitacoraMovimientos (TipoOperacion, NombreTabla, IdRegistroAfectado, Detalle) " +
                    "VALUES (@operacion, @tabla, @idRegistro, @detalle)", conn);

                cmd.Parameters.AddWithValue("@operacion", operacion);
                cmd.Parameters.AddWithValue("@tabla", tabla);
                cmd.Parameters.AddWithValue("@idRegistro", idRegistro);
                cmd.Parameters.AddWithValue("@detalle", detalle);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
        public IEnumerable<Libro> ObtenerTodosParaExportar()
        {
            var libros = new List<Libro>();

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("sp_ObtenerTodosLibrosParaExportar", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        libros.Add(new Libro
                        {
                            LibroId = reader.GetInt32(reader.GetOrdinal("libro_id")),
                            Titulo = reader.GetString(reader.GetOrdinal("titulo")),
                            CategoriaNombre = reader.GetString(reader.GetOrdinal("categoria_nombre")),
                            AutorNombre = reader.GetString(reader.GetOrdinal("autor_nombre")),
                            UnidadesDisponibles = reader.GetInt32(reader.GetOrdinal("unidades_disponibles")),
                            Precio = reader.GetDecimal(reader.GetOrdinal("precio")),
                            Descripcion = reader.IsDBNull(reader.GetOrdinal("descripcion")) ? null : reader.GetString(reader.GetOrdinal("descripcion")),
                            ImagenUrl = reader.IsDBNull(reader.GetOrdinal("imagen_url")) ? null : reader.GetString(reader.GetOrdinal("imagen_url")),
                            ISBN = reader.IsDBNull(reader.GetOrdinal("isbn")) ? null : reader.GetString(reader.GetOrdinal("isbn")),
                            Editorial = reader.IsDBNull(reader.GetOrdinal("editorial")) ? null : reader.GetString(reader.GetOrdinal("editorial")),
                            Estado = reader.GetString(reader.GetOrdinal("estado"))
                        });
                    }
                }
            }

            return libros;
        }
    }
}