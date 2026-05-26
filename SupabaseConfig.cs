using System.Reflection;

namespace Lauter_Fichaje;

/// <summary>
/// Lee las credenciales de Supabase inyectadas en tiempo de build via AssemblyMetadata.
/// Los valores proceden de Secrets.props (desarrollo) o de -p:SupabaseUrl / -p:SupabaseAnonKey (CI/CD).
/// Este archivo no contiene credenciales y es seguro para commitear.
/// </summary>
internal static class SupabaseConfig
{
    private static readonly IReadOnlyDictionary<string, string> _meta =
        typeof(SupabaseConfig).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .ToDictionary(a => a.Key, a => a.Value ?? string.Empty);

    public static string Url     => _meta["SupabaseUrl"];
    public static string AnonKey => _meta["SupabaseAnonKey"];
}
