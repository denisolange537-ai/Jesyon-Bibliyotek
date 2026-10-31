using GestionBibliotheque.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace GestionBibliotheque;

public partial class BooksView : UserControl
{
    private readonly BooksViewModel _viewModel;

    public BooksView(BooksViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += BooksView_Loaded;
    }

    private async void BooksView_Loaded(object sender, RoutedEventArgs e)
    {
        if (BooksViewModelHasLoaded()) return;
        await _viewModel.LoadAsync();
    }

    private bool BooksViewModelHasLoaded() => _viewModel.Books.Count > 0 || !string.IsNullOrEmpty(_viewModel.Message);
}
