using Lauter_Fichaje.Models;

namespace Lauter_Fichaje.Services;

public record LoginResult(bool Success, User? User = null, string? ErrorMessage = null);
public record CreateUserResult(bool Success, string? ErrorMessage = null);

public interface IAuthService
{
    Task<LoginResult> LoginAsync(string email, string password);
    Task LogoutAsync();
    Task<User?> GetCurrentUserAsync();
    Task<CreateUserResult> CreateUserAsync(string fullName, string email, string password, UserRole role, string dni);
    /// <summary>Descarga todos los perfiles de usuario de Supabase y los guarda/actualiza en SQLite local.</summary>
    Task SyncUsersFromSupabaseAsync();
    /// <summary>Actualiza los datos de perfil (nombre, DNI, rol, fecha de alta) de un usuario existente. Solo Admin.</summary>
    Task<CreateUserResult> UpdateUserProfileAsync(string userId, string fullName, string dni, UserRole role, DateTime? fechaAlta);
    /// <summary>Da de alta o de baja a un usuario. Al dar de baja fija FechaBaja=ahora; al dar de alta la limpia.</summary>
    Task<CreateUserResult> SetUserActiveAsync(string userId, bool isActive);
}
