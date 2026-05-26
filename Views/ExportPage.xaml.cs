using Lauter_Fichaje.ViewModels;

namespace Lauter_Fichaje.Views;

public partial class ExportPage : ContentPage
{
    public ExportPage(ExportViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
