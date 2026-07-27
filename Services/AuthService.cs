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

        // Save tokens to Preferences whenever GoTrue issues or refreshes a session.
        // This survives OS background-kills because Preferences is persisted to disk.
        _supabase.Auth.AddStateChangedListener((sender, state) =>
        {
            var session = _supabase.Auth.CurrentSession;
            if (state is Supabase.Gotrue.Constants.AuthState.SignedIn
                      or Supabase.Gotrue.Constants.AuthState.TokenRefreshed)
            {
                if (session is not null)
                {
                    Preferences.Default.Set("supa_access_token",  session.AccessToken  ?? string.Empty);
                    Preferences.Default.Set("supa_refresh_token", session.RefreshToken ?? string.Empty);
                }
            }
            else if (state == Supabase.Gotrue.Constants.AuthState.SignedOut)
            {
                Preferences.Default.Remove("supa_access_token");
                Preferences.Default.Remove("supa_refresh_token");
            }
        });
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

        // If the OS killed the process while in background, the in-memory GoTrue session
        // is gone. Restore from tokens persisted to Preferences by the state listener.
        // SetSession uses the refresh token to obtain a new access token automatically.
        if (_supabase.Auth.CurrentSession is null)
        {
            var access  = Preferences.Default.Get("supa_access_token",  string.Empty);
            var refresh = Preferences.Default.Get("supa_refresh_token", string.Empty);
            if (!string.IsNullOrEmpty(refresh))
                try { await _supabase.Auth.SetSession(access, refresh); } catch { }
        }

        var userId = await _localStorage.GetSessionUserIdAsync();
        if (userId is null) return null;

        var localUser = await _localStorage.GetUserByIdAsync(userId);
        if (localUser is null) return null;

        _currentUser = MapToUser(localUser);
        return _currentUser;
    }

    public async Task<CreateUserResult> CreateUserAsync(string fullName, string email, string password, UserRole role)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            return new CreateUserResult(false, "Se requiere conexión a internet");

        if (string.IsNullOrEmpty(SupabaseConfig.ServiceRoleKey))
            return new CreateUserResult(false, "Service role key no configurada en Secrets.props");

        var roleStr = role switch
        {
            UserRole.Admin   => "admin",
            UserRole.Manager => "manager",
            _                => "employee"
        };

        try
        {
            // Use the Supabase Admin REST API with the service role key.
            // email_confirm=true creates the user as already confirmed — no email is sent.
            // The handle_new_user trigger inserts the profile into public.users.
            using var http = new System.Net.Http.HttpClient();
            http.DefaultRequestHeaders.Add("apikey", SupabaseConfig.ServiceRoleKey);
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SupabaseConfig.ServiceRoleKey);

            var body = System.Text.Json.JsonSerializer.Serialize(new
            {
                email,
                password,
                email_confirm = true,
                user_metadata = new { full_name = fullName, role = roleStr }
            });

            var response = await http.PostAsync(
                $"{SupabaseConfig.Url}/auth/v1/admin/users",
                new System.Net.Http.StringContent(body, System.Text.Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
                return new CreateUserResult(true);

            var raw = await response.Content.ReadAsStringAsync();
            var parsed = System.Text.Json.JsonDocument.Parse(raw).RootElement;
            var msg = parsed.TryGetProperty("msg", out var m) ? m.GetString()
                    : parsed.TryGetProperty("message", out var m2) ? m2.GetString()
                    : raw;

            if (msg != null && (msg.Contains("already registered", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("email_exists", StringComparison.OrdinalIgnoreCase)))
                return new CreateUserResult(false, "Este correo ya está registrado");

            return new CreateUserResult(false, msg ?? "Error desconocido");
        }
        catch (Exception ex)
        {
            return new CreateUserResult(false, ex.Message);
        }
    }

    public async Task SyncUsersFromSupabaseAsync()
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return;

        if (_supabase.Auth.CurrentSession is null)
        {
            var access  = Preferences.Default.Get("supa_access_token",  string.Empty);
            var refresh = Preferences.Default.Get("supa_refresh_token", string.Empty);
            if (!string.IsNullOrEmpty(refresh))
                try { await _supabase.Auth.SetSession(access, refresh); } catch { }
        }

        if (_supabase.Auth.CurrentSession is null) return;

        try
        {
            var response = await _supabase.From<SupabaseUser>().Get();
            foreach (var u in response.Models)
                await _localStorage.UpsertUserProfileAsync(MapToUser(u));
        }
        catch { /* offline or session expired — use cached local data */ }
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
