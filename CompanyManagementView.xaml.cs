using GestionBibliotheque.ViewModels;
using System.Windows.Controls;

namespace GestionBibliotheque;

public partial class CompanyManagementView : UserControl
{
    private readonly CompanyManagementViewModel viewModel;
    public CompanyManagementView(CompanyManagementViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        DataContext = viewModel;
        Loaded += async (_, _) => await this.viewModel.LoadAsync();
    }
}
