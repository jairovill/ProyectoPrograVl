using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProyectoPrograVI.Models;
using System.Collections.Generic;
using System.Data;
using System.Text;

[Authorize]
public class BitacoraController : Controller
{
    private readonly string _connectionString;

    public BitacoraController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public IActionResult Index()
    {
        var lista = ObtenerBitacora();
        return View(lista);
    }

    public IActionResult ExportarCSV()
    {
        var registros = ObtenerBitacora();

        var csv = new StringBuilder();
        csv.AppendLine("ID;Fecha;Operación;Tabla;ID Registro;Detalle");

        foreach (var item in registros)
        {
            csv.AppendLine(
                $"{item.Id};" +
                $"\"{item.FechaMovimiento:dd/MM/yyyy HH:mm:ss}\";" +
                $"\"{item.TipoOperacion.Replace("\"", "\"\"")}\";" +
                $"\"{item.NombreTabla.Replace("\"", "\"\"")}\";" +
                $"{item.IdRegistroAfectado};" +
                $"\"{item.Detalle.Replace("\"", "\"\"")}\""
            );
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", "bitacora.csv");
    }

    private List<BitacoraMovimiento> ObtenerBitacora()
    {
        var lista = new List<BitacoraMovimiento>();

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            var cmd = new SqlCommand(
                "SELECT Id, FechaMovimiento, TipoOperacion, NombreTabla, IdRegistroAfectado, Detalle " +
                "FROM BitacoraMovimientos ORDER BY FechaMovimiento DESC", conn);

            conn.Open();
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    lista.Add(new BitacoraMovimiento
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        FechaMovimiento = reader.GetDateTime(reader.GetOrdinal("FechaMovimiento")),
                        TipoOperacion = reader.GetString(reader.GetOrdinal("TipoOperacion")),
                        NombreTabla = reader.GetString(reader.GetOrdinal("NombreTabla")),
                        IdRegistroAfectado = reader.GetInt32(reader.GetOrdinal("IdRegistroAfectado")),
                        Detalle = reader.GetString(reader.GetOrdinal("Detalle"))
                    });
                }
            }
        }

        return lista;
    }
}