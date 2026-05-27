using MiniExcelLibs.Attributes;

namespace Lauter_Fichaje.Models;

public class FichajeExportRow
{
    public string Empleado { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Fecha { get; set; } = string.Empty;
    public string Hora { get; set; } = string.Empty;
    public string Latitud { get; set; } = string.Empty;
    public string Longitud { get; set; } = string.Empty;

    [ExcelIgnore]
    public string FechaHora => $"{Fecha}  {Hora}";
}
