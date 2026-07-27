using Lauter_Fichaje.Models;

namespace Lauter_Fichaje.Services;

public interface ILocalStorageService
{
    Task InitializeAsync();
    Task<LocalUser?> GetUserByEmailAsync(string email);
    Task<LocalUser?> GetUserByIdAsync(string id);
    Task SaveUserAsync(User user, string password);
    Task<string?> GetSessionUserIdAsync();
    Task SaveSessionAsync(string userId);
    Task ClearSessionAsync();
    /// <summary>Devuelve todos los usuarios guardados localmente (para el filtro de empleados).</summary>
    Task<List<LocalUser>> GetAllUsersAsync();
    /// <summary>Guarda o actualiza el perfil de un usuario sin tocar su PasswordHash existente.</summary>
    Task UpsertUserProfileAsync(User user);
}
