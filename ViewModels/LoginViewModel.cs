using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lauter_Fichaje.Models;
using Lauter_Fichaje.Services;

namespace Lauter_Fichaje.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    public partial string Email { get; set; }

    [ObservableProperty]
    public partial string Password { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    public partial bool IsLoading { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool IsNotLoading => !IsLoading;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
        Email = string.Empty;
        Password = string.Empty;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Introduce email y contraseña";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _authService.LoginAsync(Email, Password);
            if (result.Success && result.User is not null)
                await NavigateByRoleAsync(result.User.Role);
            else
                ErrorMessage = result.ErrorMessage ?? "Error al iniciar sesión";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static async Task NavigateByRoleAsync(UserRole role)
    {
        await Shell.Current.GoToAsync("//main");
    }
}
