using ClosedXML.Excel;
using SedesBarrioNorte.Models;

namespace SedesBarrioNorte.Services;

public sealed class ExcelExportService
{
    public byte[] ExportarAlumnos(IReadOnlyList<AlumnoResultado> filas)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Alumnos");

        worksheet.Cell(1, 1).Value = "codigo_alumno";
        worksheet.Cell(1, 2).Value = "filial";
        worksheet.Cell(1, 3).Value = "alumno";
        worksheet.Cell(1, 4).Value = "mail";
        worksheet.Cell(1, 5).Value = "estado";
        worksheet.Cell(1, 6).Value = "carrera";
        worksheet.Cell(1, 7).Value = "importe_pagado";
        worksheet.Cell(1, 8).Value = "cantidad_total_cuotas";
        worksheet.Cell(1, 9).Value = "PORCENTAJE";
        worksheet.Cell(1, 10).Value = "COMISION";

        for (var i = 0; i < filas.Count; i++)
        {
            var fila = filas[i];
            var row = i + 2;

            worksheet.Cell(row, 1).Value = fila.codigo_alumno;
            worksheet.Cell(row, 2).Value = fila.filial;
            worksheet.Cell(row, 3).Value = fila.alumno;
            worksheet.Cell(row, 4).Value = fila.mail;
            worksheet.Cell(row, 5).Value = fila.estado;
            worksheet.Cell(row, 6).Value = fila.carrera;
            worksheet.Cell(row, 7).Value = fila.importe_pagado;
            worksheet.Cell(row, 8).Value = fila.cantidad_total_cuotas;
            worksheet.Cell(row, 9).Value = fila.PORCENTAJE;
            worksheet.Cell(row, 10).Value = fila.COMISION;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportarSedes(IReadOnlyList<SedeResultado> filas)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sedes");

        worksheet.Cell(1, 1).Value = "filial";
        worksheet.Cell(1, 2).Value = "importe_pagado";
        worksheet.Cell(1, 3).Value = "COMISION";

        for (var i = 0; i < filas.Count; i++)
        {
            var fila = filas[i];
            var row = i + 2;

            worksheet.Cell(row, 1).Value = fila.filial;
            worksheet.Cell(row, 2).Value = fila.importe_pagado;
            worksheet.Cell(row, 3).Value = fila.COMISION;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
