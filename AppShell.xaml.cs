using Lauter_Fichaje.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Lauter_Fichaje;

public class AppShell : Shell
{
    public AppShell()
    {
        Items.Add(new ShellContent
        {
            Route = "main",
            ContentTemplate = new DataTemplate(() =>
                IPlatformApplication.Current!.Services.GetRequiredService<MainPage>())
        });

        Routing.RegisterRoute("login", typeof(LoginPage));
        Routing.RegisterRoute("export", typeof(ExportPage));
        Routing.RegisterRoute("createuser", typeof(CreateUserPage));
        Routing.RegisterRoute("userlist", typeof(UserListPage));
        Routing.RegisterRoute("userdetail", typeof(UserDetailPage));
    }
}
