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
}
