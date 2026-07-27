using Lauter_Fichaje.Models;

namespace Lauter_Fichaje.Services;

public record LoginResult(bool Success, User? User = null, string? ErrorMessage = null);
public record CreateUserResult(bool Success, string? ErrorMessage = null);

public interface IAuthService
{
    Task<LoginResult> LoginAsync(string email, string password);
    Task LogoutAsync();
    Task<User?> GetCurrentUserAsync();
    Task<CreateUserResult> CreateUserAsync(string fullName, string email, string password, UserRole role);
    /// <summary>Descarga todos los perfiles de usuario de Supabase y los guarda/actualiza en SQLite local.</summary>
    Task SyncUsersFromSupabaseAsync();
}
