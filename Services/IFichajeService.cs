using Lauter_Fichaje.Models;

namespace Lauter_Fichaje.Services;

public interface IFichajeService
{
    Task<Fichaje?> GetLastFichajeAsync(string userId);
    Task<Fichaje> RegisterAsync(string userId, string type, double? latitude = null, double? longitude = null);
    /// <summary>
    /// Devuelve fichajes filtrados desde SQLite local, listos para agrupar en la UI.
    /// </summary>
    /// <param name="userId">Usuario autenticado (usado cuando includeAll=false).</param>
    /// <param name="includeAll">true = admin/gestor ve todos los empleados.</param>
    /// <param name="filterByUserId">Filtrar a un empleado concreto (null = todos).</param>
    /// <param name="from">Inicio del rango de fechas (inclusive, hora local).</param>
    /// <param name="to">Fin del rango de fechas (inclusive, hora local).</param>
    Task<List<Fichaje>> GetFilteredAsync(string userId, bool includeAll, string? filterByUserId, DateTime? from, DateTime? to);
    Task<(List<Fichaje> Items, bool HasMore)> GetPageAsync(string userId, int page, int pageSize = 10);
    Task SyncPendingAsync(string userId);
    Task SyncAllPendingAsync();
    /// <summary>Descarga de Supabase los fichajes del usuario que no existen localmente.</summary>
    /// <returns>true si se insertaron registros nuevos.</returns>
    Task<bool> SyncFromSupabaseAsync(string userId);
    /// <summary>Descarga de Supabase los fichajes de TODOS los usuarios que no existen localmente.
    /// Solo debe llamarse para admin/gestor (la policy RLS "Managers can read all fichajes" lo permite).</summary>
    /// <returns>true si se insertaron registros nuevos.</returns>
    Task<bool> SyncAllUsersFromSupabaseAsync();
    /// <param name="filterByUserId">Restringir la exportación a un empleado concreto (null = todos).</param>
    Task<List<FichajeExportRow>> GetAllForExportAsync(DateTime from, DateTime to, string? filterByUserId = null);
}
