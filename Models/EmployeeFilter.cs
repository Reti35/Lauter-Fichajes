namespace Lauter_Fichaje.Models;

/// <summary>
/// Ítem del Picker de empleados en el panel de filtros.
/// Id = null significa "todos los empleados".
/// </summary>
public sealed class EmployeeFilter
{
    public string? Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;

    public static readonly EmployeeFilter All = new() { Id = null, DisplayName = "Todos los empleados" };
}
