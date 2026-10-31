using GestionBibliotheque.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace GestionBibliotheque;

public partial class MembersView : UserControl
{
    private readonly MembersViewModel _viewModel;

    public MembersView(MembersViewModel viewModel)
    {
        InitializeComponent(); _viewModel = viewModel; DataContext = viewModel; Loaded += MembersView_Loaded;
    }

    private async void MembersView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Members.Count > 0 || !string.IsNullOrEmpty(_viewModel.Message)) return;
        await _viewModel.LoadAsync();
    }
}
