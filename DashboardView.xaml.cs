using GestionBibliotheque.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace GestionBibliotheque;

public partial class DashboardView : UserControl
{
    private readonly DashboardViewModel _viewModel;
    public DashboardView(DashboardViewModel viewModel) { InitializeComponent(); _viewModel = viewModel; DataContext = viewModel; Loaded += DashboardView_Loaded; }
    private async void DashboardView_Loaded(object sender, RoutedEventArgs e) { await _viewModel.LoadAsync(); }
}
