using GestionBibliotheque.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace GestionBibliotheque;

public partial class ReservationsView : UserControl
{
    private readonly ReservationsViewModel _viewModel;
    public ReservationsView(ReservationsViewModel viewModel) { InitializeComponent(); _viewModel = viewModel; DataContext = viewModel; Loaded += ReservationsView_Loaded; }
    private async void ReservationsView_Loaded(object sender, RoutedEventArgs e) { if (_viewModel.Books.Count > 0 || _viewModel.Members.Count > 0 || _viewModel.Reservations.Count > 0) return; await _viewModel.LoadAsync(); }
}
