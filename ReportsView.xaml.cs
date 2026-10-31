using GestionBibliotheque.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace GestionBibliotheque;

public partial class ReportsView : UserControl
{
    private readonly ReportViewModel _viewModel;
    public ReportsView(ReportViewModel viewModel) { InitializeComponent(); _viewModel = viewModel; DataContext = viewModel; Loaded += ReportsView_Loaded; }
    private async void ReportsView_Loaded(object sender, RoutedEventArgs e) { await _viewModel.LoadAsync(); }
}