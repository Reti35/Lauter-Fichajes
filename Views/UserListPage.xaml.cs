using Lauter_Fichaje.Models;
using Lauter_Fichaje.ViewModels;

namespace Lauter_Fichaje.Views;

public partial class UserListPage : ContentPage
{
    private readonly UserListViewModel _viewModel;

    public UserListPage(UserListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    private async void OnUserSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not User user) return;
        UsersCollectionView.SelectedItem = null;
        await _viewModel.GoToUserDetailCommand.ExecuteAsync(user);
    }
}
