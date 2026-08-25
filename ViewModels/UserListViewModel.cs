using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lauter_Fichaje.Models;
using Lauter_Fichaje.Services;

namespace Lauter_Fichaje.ViewModels;

public partial class UserListViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly ILocalStorageService _localStorageService;

    private List<User> _allUsers = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    public partial bool IsLoading { get; set; }
    public bool IsNotLoading => !IsLoading;

    [ObservableProperty] public partial string SearchText { get; set; }

    public ObservableCollection<User> Users { get; } = [];

    public UserListViewModel(IAuthService authService, ILocalStorageService localStorageService)
    {
        _authService = authService;
        _localStorageService = localStorageService;
        SearchText = string.Empty;
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            await _authService.SyncUsersFromSupabaseAsync();
            var localUsers = await _localStorageService.GetAllUsersAsync();
            _allUsers = localUsers
                .OrderBy(u => u.FullName)
                .Select(u => new User
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    Role = (UserRole)u.Role,
                    IsActive = u.IsActive,
                    Dni = u.Dni ?? string.Empty,
                    FechaAlta = u.FechaAlta,
                    FechaBaja = u.FechaBaja
                })
                .ToList();
            ApplyFilter();
        }
        finally { IsLoading = false; }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var query = string.IsNullOrWhiteSpace(SearchText)
            ? _allUsers
            : _allUsers.Where(u =>
                u.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                u.Dni.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        Users.Clear();
        foreach (var u in query) Users.Add(u);
    }

    [RelayCommand]
    private static async Task GoToUserDetailAsync(User? user)
    {
        if (user is null) return;
        await Shell.Current.GoToAsync($"userdetail?id={user.Id}");
    }

    [RelayCommand]
    private static async Task GoToCreateUserAsync() => await Shell.Current.GoToAsync("createuser");

    [RelayCommand]
    private async Task RefreshAsync() => await InitializeAsync();
}
