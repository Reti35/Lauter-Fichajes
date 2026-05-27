namespace Lauter_Fichaje.Services;

public interface ILocationService
{
    /// <summary>
    /// Solicita permiso de ubicación y obtiene las coordenadas actuales.
    /// Devuelve null si el permiso fue denegado o la ubicación no está disponible.
    /// </summary>
    Task<(double Latitude, double Longitude)?> GetCurrentLocationAsync();
}
