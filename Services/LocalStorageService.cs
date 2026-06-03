using SQLite;
using Lauter_Fichaje.Models;

namespace Lauter_Fichaje.Services;

public class LocalStorageService : ILocalStorageService
{
    private SQLiteAsyncConnection? _db;

    private async Task<SQLiteAsyncConnection> GetDbAsync()
    {
        if (_db is not null) return _db;
        var path = Path.Combine(FileSystem.AppDataDirectory, "lauter.db");
        _db = new SQLiteAsyncConnection(path);
        await _db.CreateTableAsync<LocalUser>();
        await _db.CreateTableAsync<LocalSession>();
        return _db;
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
            IsActive = user.IsActive
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
}
