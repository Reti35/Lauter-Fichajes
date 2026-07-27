using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lauter_Fichaje.Models;
using Lauter_Fichaje.Services;
using Models = Lauter_Fichaje.Models;

namespace Lauter_Fichaje.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IFichajeService _fichajeService;
    private readonly ILocationService _locationService;
    private readonly ILocalStorageService _localStorageService;

    private string _userId = string.Empty;
    private Dictionary<string, string> _userNames = [];

    // ── Estado general ─────────────────────────────────────────────────
    [ObservableProperty] public partial string UserName { get; set; }
    [ObservableProperty] public partial string RoleDisplay { get; set; }
    [ObservableProperty] public partial string StatusText { get; set; }
    [ObservableProperty] public partial string ActionButtonText { get; set; }
    [ObservableProperty] public partial bool IsDentro { get; set; }
    [ObservableProperty] public partial bool IsManagerOrAdmin { get; set; }
    [ObservableProperty] public partial bool IsAdmin { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    public partial bool IsLoading { get; set; }
    public bool IsNotLoading => !IsLoading;

    // ── Lista agrupada ─────────────────────────────────────────────────
    public ObservableCollection<FichajeGroup> GroupedFichajes { get; } = [];

    // ── Panel de filtros ───────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilterToggleText))]
    public partial bool IsFilterVisible { get; set; }

    public string FilterToggleText => IsFilterVisible ? "▲ Filtros" : "▼ Filtros";

    [ObservableProperty] public partial DateTime DateFrom { get; set; }
    [ObservableProperty] public partial DateTime DateTo { get; set; }
    [ObservableProperty] public partial EmployeeFilter? SelectedEmployee { get; set; }

    /// <summary>Lista de empleados para el Picker (solo admin/gestor).</summary>
    public ObservableCollection<EmployeeFilter> Employees { get; } = [];

    // ── Constructor ────────────────────────────────────────────────────
    public MainViewModel(
        IAuthService authService,
        IFichajeService fichajeService,
        ILocationService locationService,
        ILocalStorageService localStorageService)
    {
        _authService = authService;
        _fichajeService = fichajeService;
        _locationService = locationService;
        _localStorageService = localStorageService;

        UserName = string.Empty;
        RoleDisplay = string.Empty;
        StatusText = "Fuera";
        ActionButtonText = "Registrar entrada";

        // Rango por defecto: últimos 3 meses
        DateFrom = DateTime.Today.AddMonths(-3);
        DateTo = DateTime.Today;
    }

    // ── Inicialización ─────────────────────────────────────────────────
    public async Task InitializeAsync()
    {
        var user = await _authService.GetCurrentUserAsync();
        if (user is null) return;

        _userId = user.Id;
        UserName = user.FullName;
        RoleDisplay = user.Role switch
        {
            Models.UserRole.Admin   => "Administrador",
            Models.UserRole.Manager => "Gestor",
            _                       => "Empleado"
        };
        IsManagerOrAdmin = user.Role is Models.UserRole.Admin or Models.UserRole.Manager;
        IsAdmin = user.Role == Models.UserRole.Admin;

        if (IsManagerOrAdmin)
            await LoadEmployeesAsync();

        await RefreshStatusAsync();
        await LoadGroupedFichajesAsync();
        _ = SyncAndRefreshAsync();
    }

    // ── Carga de empleados (admin/gestor) ──────────────────────────────
    private async Task LoadEmployeesAsync()
    {
        // Sync from Supabase first so newly created users appear immediately.
        await _authService.SyncUsersFromSupabaseAsync();

        var users = await _localStorageService.GetAllUsersAsync();
        _userNames = users.ToDictionary(u => u.Id, u => u.FullName);

        Employees.Clear();
        Employees.Add(EmployeeFilter.All);
        foreach (var u in users.OrderBy(u => u.FullName))
            Employees.Add(new EmployeeFilter { Id = u.Id, DisplayName = u.FullName });

        SelectedEmployee = Employees[0]; // "Todos los empleados"
    }

    // ── Carga y agrupación de fichajes ─────────────────────────────────
    private async Task LoadGroupedFichajesAsync()
    {
        var filterUserId = IsManagerOrAdmin ? SelectedEmployee?.Id : null;

        var fichajes = await _fichajeService.GetFilteredAsync(
            _userId,
            includeAll: IsManagerOrAdmin,
            filterByUserId: filterUserId,
            from: DateFrom.Date,
            to: DateTo.Date);

        var groups = IsManagerOrAdmin
            ? BuildAdminGroups(fichajes)
            : BuildEmployeeGroups(fichajes);

        GroupedFichajes.Clear();
        foreach (var g in groups)
            GroupedFichajes.Add(g);
    }

    /// <summary>Empleado: agrupa por Mes Año, ordenado del más reciente al más antiguo.</summary>
    private static IEnumerable<FichajeGroup> BuildEmployeeGroups(IEnumerable<Fichaje> fichajes) =>
        fichajes
            .GroupBy(f => new { f.Timestamp.Year, f.Timestamp.Month })
            .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month)
            .Select(g => new FichajeGroup(
                FormatMonth(g.Key.Year, g.Key.Month),
                g.OrderByDescending(f => f.Timestamp)));

    /// <summary>Admin/gestor: agrupa por (Empleado, Mes Año), ordenado por nombre de empleado y luego fecha desc.</summary>
    private IEnumerable<FichajeGroup> BuildAdminGroups(IEnumerable<Fichaje> fichajes) =>
        fichajes
            .GroupBy(f => new { f.UserId, f.Timestamp.Year, f.Timestamp.Month })
            .OrderBy(g => _userNames.TryGetValue(g.Key.UserId, out var n) ? n : g.Key.UserId)
            .ThenByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month)
            .Select(g =>
            {
                _userNames.TryGetValue(g.Key.UserId, out var name);
                var header = $"{name ?? g.Key.UserId} — {FormatMonth(g.Key.Year, g.Key.Month)}";
                return new FichajeGroup(header, g.OrderByDescending(f => f.Timestamp));
            });

    private static string FormatMonth(int year, int month)
    {
        var s = new DateTime(year, month, 1)
            .ToString("MMMM yyyy", CultureInfo.GetCultureInfo("es-ES"));
        return char.ToUpper(s[0]) + s[1..]; // capitalizar primera letra
    }

    // ── Refresco de estado (dentro/fuera) ──────────────────────────────
    private async Task RefreshStatusAsync()
    {
        var last = await _fichajeService.GetLastFichajeAsync(_userId);
        IsDentro = last?.Type == "entrada";
        StatusText = IsDentro ? "Dentro" : "Fuera";
        ActionButtonText = IsDentro ? "Registrar salida" : "Registrar entrada";
    }

    // ── Sync en background ─────────────────────────────────────────────
    private async Task SyncAndRefreshAsync()
    {
        try
        {
            await _fichajeService.SyncPendingAsync(_userId);
            var hasNew = await _fichajeService.SyncFromSupabaseAsync(_userId);
            if (!hasNew) return;
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await RefreshStatusAsync();
                await LoadGroupedFichajesAsync();
            });
        }
        catch { /* sin conexión — se reintentará al próximo inicio */ }
    }

    // ── Comando: pull-to-refresh ───────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    public partial bool IsRefreshing { get; set; }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        try
        {
            if (IsManagerOrAdmin)
                await LoadEmployeesAsync();   // syncs users from Supabase + reloads list
            await RefreshStatusAsync();
            await LoadGroupedFichajesAsync();
            _ = SyncAndRefreshAsync();
        }
        finally { IsRefreshing = false; }
    }

    // ── Comandos: filtros ──────────────────────────────────────────────
    [RelayCommand]
    private void ToggleFilter() => IsFilterVisible = !IsFilterVisible;

    [RelayCommand]
    private async Task ApplyFilterAsync()
    {
        IsLoading = true;
        try { await LoadGroupedFichajesAsync(); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ClearFilterAsync()
    {
        DateFrom = DateTime.Today.AddMonths(-3);
        DateTo = DateTime.Today;
        if (IsManagerOrAdmin && Employees.Count > 0)
            SelectedEmployee = Employees[0];
        IsLoading = true;
        try { await LoadGroupedFichajesAsync(); }
        finally { IsLoading = false; }
    }

    // ── Comandos: fichaje ──────────────────────────────────────────────
    [RelayCommand]
    private async Task RegisterFichajeAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            var location = await _locationService.GetCurrentLocationAsync();
            var type = IsDentro ? "salida" : "entrada";
            await _fichajeService.RegisterAsync(_userId, type, location?.Latitude, location?.Longitude);
            IsDentro = type == "entrada";
            StatusText = IsDentro ? "Dentro" : "Fuera";
            ActionButtonText = IsDentro ? "Registrar salida" : "Registrar entrada";
            await LoadGroupedFichajesAsync(); // recargar grupos para incluir el nuevo fichaje
        }
        finally { IsLoading = false; }
    }

    // ── Comandos: navegación ───────────────────────────────────────────
    [RelayCommand]
    private async Task GoToExportAsync() => await Shell.Current.GoToAsync("export");

    [RelayCommand]
    private async Task GoToCreateUserAsync() => await Shell.Current.GoToAsync("createuser");

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        await Shell.Current.GoToAsync("login");
    }
}
