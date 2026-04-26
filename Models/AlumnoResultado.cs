namespace SedesBarrioNorte.Models;

public sealed class AlumnoResultado
{
    public int codigo_alumno { get; set; }
    public string filial { get; set; } = string.Empty;
    public string alumno { get; set; } = string.Empty;
    public string mail { get; set; } = string.Empty;
    public string estado { get; set; } = string.Empty;
    public string carrera { get; set; } = string.Empty;
    public decimal? importe_pagado { get; set; }
    public int? cantidad_total_cuotas { get; set; }
    public decimal? PORCENTAJE { get; set; }
    public decimal? COMISION { get; set; }
}
