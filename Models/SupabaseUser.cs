using Postgrest.Attributes;
using Postgrest.Models;

namespace Lauter_Fichaje.Models;

[Table("users")]
public class SupabaseUser : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Column("role")]
    public string Role { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("dni")]
    public string? Dni { get; set; }

    [Column("fecha_alta")]
    public DateTime? FechaAlta { get; set; }

    [Column("fecha_baja")]
    public DateTime? FechaBaja { get; set; }

    public UserRole GetUserRole() => Role switch
    {
        "admin" => UserRole.Admin,
        "manager" => UserRole.Manager,
        _ => UserRole.Employee
    };
}
