using SQLite;
using Lauter_Fichaje.Models;

namespace Lauter_Fichaje.Services;

public class LocalStorageService : ILocalStorageService
{
    private SQLiteAsyncConnection? _db;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private async Task<SQLiteAsyncConnection> GetDbAsync()
    {
        if (_db is not null) return _db;

        // Guards against concurrent callers (e.g. App.CreateWindow and MainPage.OnAppearing
        // both racing to authenticate at startup) racing past the `_db is not null` check
        // and querying a table before its CreateTableAsync has completed.
        await _initLock.WaitAsync();
        try
        {
            if (_db is not null) return _db;
            var path = Path.Combine(FileSystem.AppDataDirectory, "lauter.db");
            var db = new SQLiteAsyncConnection(path);
            await db.CreateTableAsync<LocalUser>();
            await db.CreateTableAsync<LocalSession>();
            _db = db;
            return _db;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task InitializeAsync() => await GetDbAsync();

    public async Task<LocalUser?> GetUserByEmailAsync(string email)
    {
        var db = await GetDbAsync();
        return await db.Table<LocalUser>().Where(u => u.Email == email).FirstOrDefaultAsync();
    }

    public async Task<LocalUser?> GetUserByIdAsync(string id)
    {
        var db = await GetDbAsync();
        return await db.Table<LocalUser>().Where(u => u.Id == id).FirstOrDefaultAsync();
    }

    public async Task SaveUserAsync(User user, string password)
    {
        var db = await GetDbAsync();
        var existing = await db.Table<LocalUser>().Where(u => u.Id == user.Id).FirstOrDefaultAsync();
        var local = new LocalUser
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = (int)user.Role,
            IsActive = user.IsActive,
            Dni = user.Dni,
            FechaAlta = user.FechaAlta,
            FechaBaja = user.FechaBaja
        };
        if (existing is null) await db.InsertAsync(local);
        else await db.UpdateAsync(local);
    }

    public async Task<string?> GetSessionUserIdAsync()
    {
        var db = await GetDbAsync();
        var session = await db.Table<LocalSession>().FirstOrDefaultAsync();
        return session?.UserId;
    }

    public async Task SaveSessionAsync(string userId)
    {
        var db = await GetDbAsync();
        await db.DeleteAllAsync<LocalSession>();
        await db.InsertAsync(new LocalSession { UserId = userId });
    }

    public async Task ClearSessionAsync()
    {
        var db = await GetDbAsync();
        await db.DeleteAllAsync<LocalSession>();
    }

    public async Task<List<LocalUser>> GetAllUsersAsync()
    {
        var db = await GetDbAsync();
        return await db.Table<LocalUser>().ToListAsync();
    }

    public async Task UpsertUserProfileAsync(User user)
    {
        var db = await GetDbAsync();
        var existing = await db.Table<LocalUser>().Where(u => u.Id == user.Id).FirstOrDefaultAsync();
        var local = new LocalUser
        {
            Id           = user.Id,
            Email        = user.Email,
            FullName     = user.FullName,
            PasswordHash = existing?.PasswordHash ?? string.Empty,
            Role         = (int)user.Role,
            IsActive     = user.IsActive,
            Dni          = user.Dni,
            FechaAlta    = user.FechaAlta,
            FechaBaja    = user.FechaBaja
        };
        if (existing is null) await db.InsertAsync(local);
        else await db.UpdateAsync(local);
    }
}
