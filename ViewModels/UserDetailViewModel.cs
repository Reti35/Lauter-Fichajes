using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lauter_Fichaje.Models;
using Lauter_Fichaje.Services;

namespace Lauter_Fichaje.ViewModels;

[QueryProperty(nameof(UserId), "id")]
public partial class UserDetailViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly ILocalStorageService _localStorageService;

    [ObservableProperty] public partial string UserId { get; set; }

    [ObservableProperty] public partial string Email { get; set; }
    [ObservableProperty] public partial string FullName { get; set; }
    [ObservableProperty] public partial string Dni { get; set; }
    [ObservableProperty] public partial int SelectedRoleIndex { get; set; }
    [ObservableProperty] public partial DateTime FechaAlta { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFechaBaja))]
    public partial DateTime? FechaBaja { get; set; }
    public bool HasFechaBaja => FechaBaja.HasValue;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToggleActiveText))]
    public partial bool IsActive { get; set; }
    public string ToggleActiveText => IsActive ? "Dar de baja" : "Dar de alta";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    public partial bool IsLoading { get; set; }
    public bool IsNotLoading => !IsLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    public partial string StatusMessage { get; set; }
    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    [ObservableProperty] public partial bool IsSuccess { get; set; }

    public List<string> Roles { get; } = ["Empleado", "Gestor", "Admin"];

    public UserDetailViewModel(IAuthService authService, ILocalStorageService localStorageService)
    {
        _authService = authService;
        _localStorageService = localStorageService;
        UserId = string.Empty;
        Email = string.Empty;
        FullName = string.Empty;
        Dni = string.Empty;
        StatusMessage = string.Empty;
        FechaAlta = DateTime.Today;
    }

    partial void OnUserIdChanged(string value) => _ = LoadAsync();

    private async Task LoadAsync()
    {
        if (string.IsNullOrEmpty(UserId)) return;

        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            var local = await _localStorageService.GetUserByIdAsync(UserId);
            if (local is null)
            {
                StatusMessage = "Usuario no encontrado";
                IsSuccess = false;
                return;
            }

            Email = local.Email;
            FullName = local.FullName;
            Dni = local.Dni ?? string.Empty;
            SelectedRoleIndex = local.Role;
            FechaAlta = local.FechaAlta ?? DateTime.Today;
            FechaBaja = local.FechaBaja;
            IsActive = local.IsActive;
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Dni))
        {
            StatusMessage = "Completa nombre y DNI";
            IsSuccess = false;
            return;
        }

        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            var role = (UserRole)SelectedRoleIndex;
            var result = await _authService.UpdateUserProfileAsync(UserId, FullName.Trim(), Dni.Trim(), role, FechaAlta);
            IsSuccess = result.Success;
            StatusMessage = result.Success
                ? "Cambios guardados correctamente"
                : result.ErrorMessage ?? "Error al guardar los cambios";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync()
    {
        var confirm = await Shell.Current.DisplayAlertAsync(
            IsActive ? "Dar de baja" : "Dar de alta",
            IsActive
                ? $"¿Seguro que quieres dar de baja a {FullName}? No podrá iniciar sesión hasta que se le vuelva a dar de alta."
                : $"¿Seguro que quieres dar de alta a {FullName}?",
            "Sí", "Cancelar");
        if (!confirm) return;

        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            var result = await _authService.SetUserActiveAsync(UserId, !IsActive);
            IsSuccess = result.Success;
            if (result.Success)
            {
                IsActive = !IsActive;
                FechaBaja = IsActive ? null : DateTime.UtcNow;
                StatusMessage = IsActive ? "Usuario dado de alta" : "Usuario dado de baja";
            }
            else
            {
                StatusMessage = result.ErrorMessage ?? "Error al cambiar el estado";
            }
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private static async Task GoBackAsync() => await Shell.Current.GoToAsync("..");
}
