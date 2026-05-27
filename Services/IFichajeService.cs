using Lauter_Fichaje.Models;

namespace Lauter_Fichaje.Services;

public interface IFichajeService
{
    Task<Fichaje?> GetLastFichajeAsync(string userId);
    Task<Fichaje> RegisterAsync(string userId, string type, double? latitude = null, double? longitude = null);
    Task<(List<Fichaje> Items, bool HasMore)> GetPageAsync(string userId, int page, int pageSize = 10);
    Task SyncPendingAsync(string userId);
    Task SyncAllPendingAsync();
    /// <summary>Descarga de Supabase los fichajes del usuario que no existen localmente.</summary>
    /// <returns>true si se insertaron registros nuevos.</returns>
    Task<bool> SyncFromSupabaseAsync(string userId);
    Task<List<FichajeExportRow>> GetAllForExportAsync(DateTime from, DateTime to);
}
