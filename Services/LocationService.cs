namespace Lauter_Fichaje.Services;

public class LocationService : ILocationService
{
    public async Task<(double Latitude, double Longitude)?> GetCurrentLocationAsync()
    {
        try
        {
            // Verificar y solicitar permiso
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    System.Diagnostics.Debug.WriteLine("[LocationService] Permiso de ubicación denegado.");
                    return null;
                }
            }

            // Obtener ubicación con precisión media y timeout de 8 segundos
            var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest
            {
                DesiredAccuracy = GeolocationAccuracy.Medium,
                Timeout = TimeSpan.FromSeconds(8)
            });

            if (location is null)
            {
                System.Diagnostics.Debug.WriteLine("[LocationService] Ubicación no disponible.");
                return null;
            }

            return (location.Latitude, location.Longitude);
        }
        catch (FeatureNotSupportedException)
        {
            System.Diagnostics.Debug.WriteLine("[LocationService] GPS no soportado en este dispositivo.");
            return null;
        }
        catch (FeatureNotEnabledException)
        {
            System.Diagnostics.Debug.WriteLine("[LocationService] GPS desactivado.");
            return null;
        }
        catch (PermissionException)
        {
            System.Diagnostics.Debug.WriteLine("[LocationService] Permiso denegado (excepción).");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LocationService] Error inesperado: {ex.Message}");
            return null;
        }
    }
}
