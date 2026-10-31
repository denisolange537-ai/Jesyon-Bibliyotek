using GestionBibliotheque.ViewModels;
using GestionBibliotheque.Services;
using System.Windows;
using System.Windows.Controls;

namespace GestionBibliotheque;

public partial class PayrollView : UserControl
{
    private readonly PayrollViewModel viewModel;
    public PayrollView(PayrollViewModel viewModel, AuthenticationService authenticationService, AuthenticationSession session)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        this.viewModel.ReauthenticateForApprovalAsync = () =>
        {
            var dialog = new ReauthenticationWindow(authenticationService, session)
            {
                Owner = Window.GetWindow(this)
            };
            return Task.FromResult(dialog.ShowDialog() == true);
        };
        DataContext = viewModel;
        Loaded += async (_, _) => await this.viewModel.LoadAsync();
    }
}
