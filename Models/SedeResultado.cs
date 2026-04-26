namespace SedesBarrioNorte.Models;

public sealed class SedeResultado
{
    public string filial { get; set; } = string.Empty;
    public decimal? importe_pagado { get; set; }
    public decimal? COMISION { get; set; }
}
