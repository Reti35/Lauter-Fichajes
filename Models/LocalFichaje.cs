using SQLite;

namespace Lauter_Fichaje.Models;

[Table("local_fichajes")]
public class LocalFichaje
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool Synced { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
