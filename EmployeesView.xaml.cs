using GestionBibliotheque.ViewModels;
using System.Windows.Controls;

namespace GestionBibliotheque;

public partial class EmployeesView : UserControl
{
    public EmployeesView(EmployeesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.ConfirmPasswordReset = () => System.Windows.MessageBox.Show(
            "Verifye idantite anplwaye a dapre pwosedi bibliyotèk la. Kontinye ak reset modpas la?",
            "Konfime reset modpas",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning) == System.Windows.MessageBoxResult.Yes;
        viewModel.TemporaryPasswordIssued += temporaryPassword =>
            System.Windows.MessageBox.Show(
                $"Modpas tanporè a (montre yon sèl fwa) se:\n\n{temporaryPassword}\n\nVerifye idantite anplwaye a anvan ou transmèt li. Itilizatè a dwe chanje l nan pwochen login.",
                "Modpas tanporè",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        Loaded += async (_, _) => await viewModel.LoadAsync();
    }

    private void AccountPasswordBox_Changed(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is EmployeesViewModel viewModel && sender is System.Windows.Controls.PasswordBox passwordBox)
            viewModel.AccountPassword = passwordBox.Password;
    }
}
