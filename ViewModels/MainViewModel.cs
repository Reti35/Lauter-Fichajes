using System.Collections.ObjectModel;
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
    private string _userId = string.Empty;
    private int _currentPage;
    private const int PageSize = 10;

    [ObservableProperty] public partial string UserName { get; set; }
    [ObservableProperty] public partial string RoleDisplay { get; set; }
    [ObservableProperty] public partial string StatusText { get; set; }
    [ObservableProperty] public partial string ActionButtonText { get; set; }
    [ObservableProperty] public partial bool IsDentro { get; set; }
    [ObservableProperty] public partial bool HasMore { get; set; }
    [ObservableProperty] public partial bool IsManagerOrAdmin { get; set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    public partial bool IsLoading { get; set; }

    public bool IsNotLoading => !IsLoading;

    public ObservableCollection<Fichaje> Fichajes { get; } = [];

    public MainViewModel(IAuthService authService, IFichajeService fichajeService)
    {
        _authService = authService;
        _fichajeService = fichajeService;
        UserName = string.Empty;
        RoleDisplay = string.Empty;
        StatusText = "Fuera";
        ActionButtonText = "Registrar entrada";
    }

    public async Task InitializeAsync()
    {
        var user = await _authService.GetCurrentUserAsync();
        if (user is null) return;

        _userId = user.Id;
        UserName = user.FullName;
        RoleDisplay = user.Role switch
        {
            Models.UserRole.Admin => "Administrador",
            Models.UserRole.Manager => "Gestor",
            _ => "Empleado"
        };
        IsManagerOrAdmin = user.Role is Models.UserRole.Admin or Models.UserRole.Manager;

        _currentPage = 0;
        Fichajes.Clear();
        await RefreshStatusAsync();
        await LoadPageAsync();
        _ = SyncAndRefreshAsync();
    }

    /// <summary>
    /// Sube fichajes pendientes y descarga los nuevos de Supabase.
    /// Si llegan registros nuevos, refresca la UI en el hilo principal.
    /// </summary>
    private async Task SyncAndRefreshAsync()
    {
        try
        {
            await _fichajeService.SyncPendingAsync(_userId);
            var hasNew = await _fichajeService.SyncFromSupabaseAsync(_userId);
            if (!hasNew) return;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                _currentPage = 0;
                Fichajes.Clear();
                await RefreshStatusAsync();
                await LoadPageAsync();
            });
        }
        catch { /* sin conexión o error transitorio — se reintentará al próximo inicio */ }
    }

    private async Task RefreshStatusAsync()
    {
        var last = await _fichajeService.GetLastFichajeAsync(_userId);
        IsDentro = last?.Type == "entrada";
        StatusText = IsDentro ? "Dentro" : "Fuera";
        ActionButtonText = IsDentro ? "Registrar salida" : "Registrar entrada";
    }

    private async Task LoadPageAsync()
    {
        var (items, hasMore) = await _fichajeService.GetPageAsync(_userId, _currentPage, PageSize);
        foreach (var item in items) Fichajes.Add(item);
        HasMore = hasMore;
    }

    [RelayCommand]
    private async Task RegisterFichajeAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            var type = IsDentro ? "salida" : "entrada";
            var fichaje = await _fichajeService.RegisterAsync(_userId, type);
            Fichajes.Insert(0, fichaje);
            IsDentro = type == "entrada";
            StatusText = IsDentro ? "Dentro" : "Fuera";
            ActionButtonText = IsDentro ? "Registrar salida" : "Registrar entrada";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (!HasMore) return;
        _currentPage++;
        await LoadPageAsync();
    }

    [RelayCommand]
    private async Task GoToExportAsync()
    {
        await Shell.Current.GoToAsync("export");
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        await Shell.Current.GoToAsync("login");
    }
}
