using Lauter_Fichaje.ViewModels;

namespace Lauter_Fichaje.Views;

public partial class CreateUserPage : ContentPage
{
    public CreateUserPage(CreateUserViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
