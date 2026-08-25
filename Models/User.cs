namespace Lauter_Fichaje.Models;

public enum UserRole { Employee = 0, Manager = 1, Admin = 2 }

public class User
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public string Dni { get; set; } = string.Empty;
    public DateTime? FechaAlta { get; set; }
    public DateTime? FechaBaja { get; set; }
}
