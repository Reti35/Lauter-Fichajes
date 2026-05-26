using Lauter_Fichaje.Models;
using MiniExcelLibs;

namespace Lauter_Fichaje.Services;

public class ExportService : IExportService
{
    public async Task<string> GenerateExcelAsync(List<FichajeExportRow> rows, DateTime from, DateTime to)
    {
        var fileName = $"fichajes_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx";
        var path = Path.Combine(FileSystem.CacheDirectory, fileName);
        await MiniExcel.SaveAsAsync(path, rows, overwriteFile: true);
        return path;
    }
}
