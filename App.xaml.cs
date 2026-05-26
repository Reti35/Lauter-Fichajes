using Lauter_Fichaje.Services;

namespace Lauter_Fichaje;

public partial class App : Application
{
    private readonly IAuthService _authService;

    public App(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var shell = new AppShell();
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var user = await _authService.GetCurrentUserAsync();
                if (user is null)
                    await shell.GoToAsync("login");
            }
            catch (Exception ex)
            {
                await shell.DisplayAlertAsync(
                    "Error al iniciar",
                    $"{ex.GetType().Name}: {ex.Message}\n\n{ex.InnerException?.Message}",
                    "OK");
            }
        });
        return new Window(shell);
    }
}
