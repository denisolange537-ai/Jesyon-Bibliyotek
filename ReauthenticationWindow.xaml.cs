using GestionBibliotheque.Services;
using System.Windows;

namespace GestionBibliotheque;

public partial class ReauthenticationWindow : Window
{
    private readonly AuthenticationService _authenticationService;

    public ReauthenticationWindow(AuthenticationService authenticationService, AuthenticationSession session)
    {
        InitializeComponent();
        _authenticationService = authenticationService;
        UsernameTextBlock.Text = session.CurrentUser?.Username ?? string.Empty;
        Loaded += (_, _) => PasswordBox.Focus();
    }

    private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        ConfirmButton.IsEnabled = false;
        MessageTextBlock.Text = string.Empty;
        try
        {
            if (!await _authenticationService.ReauthenticateForPayrollApprovalAsync(PasswordBox.Password))
            {
                MessageTextBlock.Text = "Modpas la pa kòrèk oswa kont la pa disponib.";
                PasswordBox.Clear();
                return;
            }

            PasswordBox.Clear();
            DialogResult = true;
        }
        catch (Exception)
        {
            PasswordBox.Clear();
            MessageTextBlock.Text = "Konfimasyon an echwe. Tanpri eseye ankò.";
        }
        finally
        {
            ConfirmButton.IsEnabled = true;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
