using GestionBibliotheque.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace GestionBibliotheque;

public partial class ReturnsView : UserControl
{
    private readonly ReturnsViewModel _viewModel;
    public ReturnsView(ReturnsViewModel viewModel) { InitializeComponent(); _viewModel = viewModel; DataContext = viewModel; Loaded += ReturnsView_Loaded; }
    private async void ReturnsView_Loaded(object sender, RoutedEventArgs e) { if (_viewModel.OpenBorrowings.Count > 0 || _viewModel.OutstandingFines.Count > 0) return; await _viewModel.LoadAsync(); }
}
