using Lauter_Fichaje.ViewModels;

namespace Lauter_Fichaje.Views;

public partial class UserDetailPage : ContentPage
{
    public UserDetailPage(UserDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
