using GestionBibliotheque.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace GestionBibliotheque;

public partial class BorrowingView : UserControl
{
    private readonly BorrowingViewModel _viewModel;

    public BorrowingView(BorrowingViewModel viewModel)
    {
        InitializeComponent(); _viewModel = viewModel; DataContext = viewModel; Loaded += BorrowingView_Loaded;
    }

    private async void BorrowingView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Books.Count > 0 || _viewModel.Members.Count > 0 || _viewModel.OpenBorrowings.Count > 0) return;
        await _viewModel.LoadAsync();
    }
}
