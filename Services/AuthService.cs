using Lauter_Fichaje.Models;
using Supabase;
using static Postgrest.Constants;

namespace Lauter_Fichaje.Services;

public class AuthService : IAuthService
{
    private readonly Client _supabase;
    private readonly ILocalStorageService _localStorage;
    private User? _currentUser;

    public AuthService(Client supabase, ILocalStorageService localStorage)
    {
        _supabase = supabase;
        _localStorage = localStorage;
    }

    public async Task<LoginResult> LoginAsync(string email, string password)
    {
        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            try
            {
                var session = await _supabase.Auth.SignIn(email: email, password: password);
                if (session?.User is null)
                    return new LoginResult(false, ErrorMessage: "Credenciales incorrectas");

                var response = await _supabase.From<SupabaseUser>()
                    .Filter("id", Operator.Equals, session.User.Id)
                    .Get();

                var supabaseUser = response.Models.FirstOrDefault();
                if (supabaseUser is null)
                    return new LoginResult(false, ErrorMessage: "Perfil de usuario no encontrado en la base de datos");

                var user = MapToUser(supabaseUser);
                _currentUser = user;
                await _localStorage.SaveUserAsync(user, password);
                await _localStorage.SaveSessionAsync(user.Id);
                return new LoginResult(true, user);
            }
            catch (Exception ex) when (IsAuthError(ex))
            {
                return new LoginResult(false, ErrorMessage: "Credenciales incorrectas");
            }
            catch
            {
                // Network or other error — fall through to offline
            }
        }

        // Offline fallback
        var localUser = await _localStorage.GetUserByEmailAsync(email);
        if (localUser is null)
            return new LoginResult(false, ErrorMessage: "Sin conexión. Conéctate a internet para el primer acceso.");

        if (!BCrypt.Net.BCrypt.Verify(password, localUser.PasswordHash))
            return new LoginResult(false, ErrorMessage: "Credenciales incorrectas");

        var offlineUser = MapToUser(localUser);
        _currentUser = offlineUser;
        await _localStorage.SaveSessionAsync(offlineUser.Id);
        return new LoginResult(true, offlineUser);
    }

    public async Task LogoutAsync()
    {
        _currentUser = null;
        await _localStorage.ClearSessionAsync();
        try { await _supabase.Auth.SignOut(); } catch { }
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        if (_currentUser is not null) return _currentUser;

        // Restore Supabase auth session from persistent storage after app restart.
        // Without this, Supabase queries run unauthenticated and RLS returns empty results.
        if (_supabase.Auth.CurrentSession is null)
        {
            try { await _supabase.InitializeAsync(); } catch { }
        }

        var userId = await _localStorage.GetSessionUserIdAsync();
        if (userId is null) return null;

        var localUser = await _localStorage.GetUserByIdAsync(userId);
        if (localUser is null) return null;

        _currentUser = MapToUser(localUser);
        return _currentUser;
    }

    private static bool IsAuthError(Exception ex) =>
        ex.Message.Contains("Invalid login") ||
        ex.Message.Contains("invalid_grant") ||
        ex.Message.Contains("Email not confirmed") ||
        ex.Message.Contains("Invalid email");

    private static User MapToUser(SupabaseUser u) => new()
    {
        Id = u.Id,
        Email = u.Email,
        FullName = u.FullName,
        Role = u.GetUserRole(),
        IsActive = u.IsActive
    };

    private static User MapToUser(LocalUser u) => new()
    {
        Id = u.Id,
        Email = u.Email,
        FullName = u.FullName,
        Role = (UserRole)u.Role,
        IsActive = u.IsActive
    };
}
