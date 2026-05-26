namespace Lauter_Fichaje.Models;

public class Fichaje
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool Synced { get; set; }

    public bool IsEntrada => Type == "entrada";
    public string TypeDisplay => IsEntrada ? "Entrada" : "Salida";
    public string FormattedTime => Timestamp.ToString("dd/MM  HH:mm");
}
