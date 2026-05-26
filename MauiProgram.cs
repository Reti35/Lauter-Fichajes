using Microsoft.Extensions.Logging;
using Supabase;
using Lauter_Fichaje.Services;
using Lauter_Fichaje.ViewModels;
using Lauter_Fichaje.Views;

namespace Lauter_Fichaje;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        SQLitePCL.Batteries_V2.Init();

        builder.Services.AddSingleton(_ => new Client(
            SupabaseConfig.Url,
            SupabaseConfig.AnonKey,
            new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            }));

        builder.Services.AddSingleton<ILocalStorageService, LocalStorageService>();
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<IFichajeService, FichajeService>();
        builder.Services.AddSingleton<IExportService, ExportService>();

        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<ExportViewModel>();
        builder.Services.AddTransient<ExportPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
