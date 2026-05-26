using Lauter_Fichaje.ViewModels;

namespace Lauter_Fichaje.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override bool OnBackButtonPressed() => true;
}
