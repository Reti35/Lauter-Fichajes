using Lauter_Fichaje.Models;

namespace Lauter_Fichaje.Services;

public interface IExportService
{
    Task<string> GenerateExcelAsync(List<FichajeExportRow> rows, DateTime from, DateTime to);
}
