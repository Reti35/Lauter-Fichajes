namespace Lauter_Fichaje.Models;

/// <summary>
/// Grupo de fichajes para CollectionView.IsGrouped.
/// Header: "Mayo 2026" (empleado) · "Ana García — Mayo 2026" (admin).
/// </summary>
public class FichajeGroup : List<Fichaje>
{
    public string Header { get; }

    public FichajeGroup(string header, IEnumerable<Fichaje> items) : base(items)
    {
        Header = header;
    }
}
