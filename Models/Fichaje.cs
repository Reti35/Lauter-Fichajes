namespace Lauter_Fichaje.Models;

public class Fichaje
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool Synced { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public bool IsEntrada => Type == "entrada";
    public string TypeDisplay => IsEntrada ? "Entrada" : "Salida";
    public string FormattedTime => Timestamp.ToString("dd/MM  HH:mm");
    public bool HasLocation => Latitude.HasValue && Longitude.HasValue;
    public string LocationDisplay => HasLocation
        ? $"📍 {Latitude:F5}, {Longitude:F5}"
        : string.Empty;
}
