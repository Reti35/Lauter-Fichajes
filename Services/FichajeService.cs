using SQLite;
using Lauter_Fichaje.Models;
using Supabase;
using static Postgrest.Constants;

namespace Lauter_Fichaje.Services;

public class FichajeService : IFichajeService
{
    private readonly Client _supabase;
    private SQLiteAsyncConnection? _db;

    public FichajeService(Client supabase)
    {
        _supabase = supabase;
    }

    private async Task<SQLiteAsyncConnection> GetDbAsync()
    {
        if (_db is not null) return _db;
        var path = Path.Combine(FileSystem.AppDataDirectory, "lauter.db");
        _db = new SQLiteAsyncConnection(path);
        await _db.CreateTableAsync<LocalFichaje>();
        return _db;
    }

    public async Task<List<Fichaje>> GetFilteredAsync(
        string userId, bool includeAll, string? filterByUserId, DateTime? from, DateTime? to)
    {
        var db = await GetDbAsync();

        // Pre-computar límites UTC para que sqlite-net-pcl pueda traducirlos a SQL
        DateTime? fromUtc = from.HasValue ? from.Value.Date.ToUniversalTime() : null;
        DateTime? toUtc   = to.HasValue   ? to.Value.Date.AddDays(1).ToUniversalTime() : null;

        var query = db.Table<LocalFichaje>();

        if (!includeAll)
            query = query.Where(f => f.UserId == userId);
        else if (filterByUserId is not null)
            query = query.Where(f => f.UserId == filterByUserId);

        if (fromUtc.HasValue) { var f = fromUtc.Value; query = query.Where(x => x.Timestamp >= f); }
        if (toUtc.HasValue)   { var t = toUtc.Value;   query = query.Where(x => x.Timestamp < t);  }

        var rows = await query.OrderByDescending(x => x.Timestamp).ToListAsync();
        return rows.Select(MapToDomain).ToList();
    }

    public async Task<Fichaje?> GetLastFichajeAsync(string userId)
    {
        var db = await GetDbAsync();
        var local = await db.Table<LocalFichaje>()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.Timestamp)
            .FirstOrDefaultAsync();
        return local is null ? null : MapToDomain(local);
    }

    public async Task<Fichaje> RegisterAsync(string userId, string type, double? latitude = null, double? longitude = null)
    {
        var db = await GetDbAsync();
        var fichaje = new LocalFichaje
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Type = type,
            Timestamp = DateTime.UtcNow,
            Synced = false,
            Latitude = latitude,
            Longitude = longitude
        };
        await db.InsertAsync(fichaje);
        _ = TrySyncAsync(fichaje); // fire and forget
        return MapToDomain(fichaje);
    }

    public async Task<(List<Fichaje> Items, bool HasMore)> GetPageAsync(string userId, int page, int pageSize = 10)
    {
        var db = await GetDbAsync();
        var rows = await db.Table<LocalFichaje>()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.Timestamp)
            .Skip(page * pageSize)
            .Take(pageSize + 1)
            .ToListAsync();

        var hasMore = rows.Count > pageSize;
        if (hasMore) rows.RemoveAt(rows.Count - 1);
        return (rows.Select(MapToDomain).ToList(), hasMore);
    }

    public async Task SyncPendingAsync(string userId)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return;
        var db = await GetDbAsync();
        var pending = await db.Table<LocalFichaje>()
            .Where(f => f.UserId == userId && !f.Synced)
            .ToListAsync();
        foreach (var f in pending)
            await TrySyncAsync(f);
    }

    public async Task SyncAllPendingAsync()
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return;
        var db = await GetDbAsync();
        var pending = await db.Table<LocalFichaje>()
            .Where(f => !f.Synced)
            .ToListAsync();
        foreach (var f in pending)
            await TrySyncAsync(f);
    }

    private async Task TrySyncAsync(LocalFichaje fichaje)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return;
        try
        {
            await _supabase.From<SupabaseFichaje>().Upsert(new SupabaseFichaje
            {
                Id        = fichaje.Id,
                UserId    = fichaje.UserId,
                Type      = fichaje.Type,
                Timestamp = fichaje.Timestamp,
                Latitude  = fichaje.Latitude,
                Longitude = fichaje.Longitude
            });
            fichaje.Synced = true;
            var db = await GetDbAsync();
            await db.UpdateAsync(fichaje);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FichajeService] Sync failed for {fichaje.Id}: {ex.Message}");
            /* retry on next sync */
        }
    }

    public async Task<bool> SyncFromSupabaseAsync(string userId)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return false;

        var db = await GetDbAsync();

        // IDs que ya tenemos localmente para este usuario
        var localIds = (await db.Table<LocalFichaje>()
            .Where(f => f.UserId == userId)
            .ToListAsync())
            .Select(f => f.Id)
            .ToHashSet();

        // Todos los fichajes del usuario en Supabase
        var response = await _supabase.From<SupabaseFichaje>()
            .Filter("user_id", Operator.Equals, userId)
            .Order("timestamp", Ordering.Ascending)
            .Get();

        // Solo los que no existen localmente
        var toInsert = response.Models
            .Where(r => !localIds.Contains(r.Id))
            .Select(r => new LocalFichaje
            {
                Id        = r.Id,
                UserId    = r.UserId,
                Type      = r.Type,
                Timestamp = r.Timestamp,
                Synced    = true,
                Latitude  = r.Latitude,
                Longitude = r.Longitude
            })
            .ToList();

        if (toInsert.Count == 0) return false;

        await db.InsertAllAsync(toInsert);
        return true;
    }

    public async Task<List<FichajeExportRow>> GetAllForExportAsync(DateTime from, DateTime to)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            throw new InvalidOperationException("Se requiere conexión a internet para exportar");

        var usersResp = await _supabase.From<SupabaseUser>().Get();
        var users = usersResp.Models.ToDictionary(u => u.Id, u => (u.FullName, u.Email));

        var fichajesResp = await _supabase.From<SupabaseFichaje>()
            .Filter("timestamp", Operator.GreaterThanOrEqual, from.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"))
            .Filter("timestamp", Operator.LessThan, to.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"))
            .Order("timestamp", Ordering.Descending)
            .Get();

        return fichajesResp.Models.Select(f =>
        {
            users.TryGetValue(f.UserId, out var user);
            var local = f.Timestamp.ToLocalTime();
            return new FichajeExportRow
            {
                Empleado  = user.FullName ?? "Desconocido",
                Email     = user.Email ?? string.Empty,
                Tipo      = f.Type == "entrada" ? "Entrada" : "Salida",
                Fecha     = local.ToString("dd/MM/yyyy"),
                Hora      = local.ToString("HH:mm"),
                Latitud   = f.Latitude.HasValue  ? f.Latitude.Value.ToString("F5")  : string.Empty,
                Longitud  = f.Longitude.HasValue ? f.Longitude.Value.ToString("F5") : string.Empty
            };
        }).ToList();
    }

    private static Fichaje MapToDomain(LocalFichaje f) => new()
    {
        Id        = f.Id,
        UserId    = f.UserId,
        Type      = f.Type,
        Timestamp = f.Timestamp.ToLocalTime(),
        Synced    = f.Synced,
        Latitude  = f.Latitude,
        Longitude = f.Longitude
    };
}
