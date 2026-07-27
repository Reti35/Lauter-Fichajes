using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lauter_Fichaje.Models;
using Lauter_Fichaje.Services;

namespace Lauter_Fichaje.ViewModels;

public partial class CreateUserViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    public partial string StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsSuccess { get; set; }

    [ObservableProperty] public partial string FullName { get; set; }
    [ObservableProperty] public partial string Email { get; set; }
    [ObservableProperty] public partial string Password { get; set; }
    [ObservableProperty] public partial string ConfirmPassword { get; set; }
    [ObservableProperty] public partial int SelectedRoleIndex { get; set; }

    public bool IsNotLoading => !IsLoading;
    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public List<string> Roles { get; } = ["Empleado", "Gestor"];

    public CreateUserViewModel(IAuthService authService)
    {
        _authService = authService;
        FullName = string.Empty;
        Email = string.Empty;
        Password = string.Empty;
        ConfirmPassword = string.Empty;
        StatusMessage = string.Empty;
        SelectedRoleIndex = 0;
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (string.IsNullOrWhiteSpace(FullName) ||
            string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Password) ||
            string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            StatusMessage = "Completa todos los campos";
            IsSuccess = false;
            return;
        }

        if (Password != ConfirmPassword)
        {
            StatusMessage = "Las contraseñas no coinciden";
            IsSuccess = false;
            return;
        }

        if (Password.Length < 6)
        {
            StatusMessage = "La contraseña debe tener al menos 6 caracteres";
            IsSuccess = false;
            return;
        }

        IsLoading = true;
        IsSuccess = false;
        StatusMessage = "Creando usuario...";

        var role = SelectedRoleIndex == 1 ? UserRole.Manager : UserRole.Employee;
        var result = await _authService.CreateUserAsync(FullName.Trim(), Email.Trim(), Password, role);

        IsLoading = false;

        if (result.Success)
        {
            IsSuccess = true;
            StatusMessage = $"Usuario «{FullName.Trim()}» creado correctamente";
            FullName = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            SelectedRoleIndex = 0;
        }
        else
        {
            StatusMessage = result.ErrorMessage ?? "Error desconocido";
        }
    }

    [RelayCommand]
    private static async Task GoBackAsync() => await Shell.Current.GoToAsync("..");
}
