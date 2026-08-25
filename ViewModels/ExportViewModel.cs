using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lauter_Fichaje.Models;
using Lauter_Fichaje.Services;

namespace Lauter_Fichaje.ViewModels;

public partial class ExportViewModel : ObservableObject
{
    private readonly IFichajeService _fichajeService;
    private readonly IExportService _exportService;
    private readonly IAuthService _authService;
    private readonly ILocalStorageService _localStorageService;

    [ObservableProperty]
    public partial DateTime DateFrom { get; set; }

    [ObservableProperty]
    public partial DateTime DateTo { get; set; }

    [ObservableProperty]
    public partial EmployeeFilter? SelectedEmployee { get; set; }

    /// <summary>Lista de empleados para el Picker de exportación (null = todos).</summary>
    public ObservableCollection<EmployeeFilter> Employees { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    public partial string StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool HasRows { get; set; }

    public bool IsNotLoading => !IsLoading;
    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public ObservableCollection<FichajeExportRow> Rows { get; } = [];

    public ExportViewModel(
        IFichajeService fichajeService,
        IExportService exportService,
        IAuthService authService,
        ILocalStorageService localStorageService)
    {
        _fichajeService = fichajeService;
        _exportService = exportService;
        _authService = authService;
        _localStorageService = localStorageService;
        DateFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        DateTo = DateTime.Today;
        StatusMessage = string.Empty;
    }

    public async Task InitializeAsync()
    {
        if (Employees.Count > 0) return;

        await _authService.SyncUsersFromSupabaseAsync();
        var users = await _localStorageService.GetAllUsersAsync();

        Employees.Clear();
        Employees.Add(EmployeeFilter.All);
        foreach (var u in users.OrderBy(u => u.FullName))
            Employees.Add(new EmployeeFilter { Id = u.Id, DisplayName = u.FullName });

        SelectedEmployee = Employees[0]; // "Todos los empleados"
    }

    [RelayCommand]
    private async Task LoadPreviewAsync()
    {
        IsLoading = true;
        StatusMessage = "Sincronizando datos...";
        try
        {
            Rows.Clear();
            await _fichajeService.SyncAllPendingAsync();
            StatusMessage = string.Empty;
            var to = DateTo.Date.AddDays(1);
            var rows = await _fichajeService.GetAllForExportAsync(DateFrom.Date, to, SelectedEmployee?.Id);
            foreach (var r in rows) Rows.Add(r);
            HasRows = Rows.Count > 0;
            StatusMessage = Rows.Count == 0
                ? "No hay fichajes en el rango seleccionado"
                : $"{Rows.Count} registros encontrados";
        }
        catch (Exception ex)
        {
            HasRows = false;
            StatusMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        if (!HasRows) return;
        IsLoading = true;
        string path;
        try
        {
            path = await _exportService.GenerateExcelAsync([.. Rows], DateFrom, DateTo);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al generar el fichero: {ex.Message}";
            IsLoading = false;
            return;
        }

        // El fichero ya está listo: liberamos IsLoading ANTES del diálogo del sistema.
        // Share.RequestAsync puede bloquearse indefinidamente hasta que el usuario
        // interactúa con el cuadro de compartir/guardar; si IsLoading quedara a true
        // durante ese tiempo, el botón "Ver fichajes" permanecería deshabilitado.
        IsLoading = false;

        try
        {
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Exportar fichajes",
                File = new ShareFile(path, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al compartir: {ex.Message}";
        }
    }
}
