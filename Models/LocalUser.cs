using SQLite;

namespace Lauter_Fichaje.Models;

[Table("local_users")]
public class LocalUser
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int Role { get; set; }
    public bool IsActive { get; set; }
}

[Table("local_session")]
public class LocalSession
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
}
