using GestionBibliotheque.Services;
using System.Windows;

namespace GestionBibliotheque;

public partial class ChangePasswordWindow : Window
{
    private readonly AuthenticationService _authenticationService;
    private bool _isForced;

    public ChangePasswordWindow(AuthenticationService authenticationService, AuthenticationSession session)
    {
        InitializeComponent();
        _authenticationService = authenticationService;
        _isForced = session.CurrentUser?.RequirePasswordChange == true;
        CancelButton.IsEnabled = !_isForced;
        if (_isForced)
        {
            ForcedMessageTextBlock.Text = "Ou dwe chanje modpas tanporè a anvan ou sèvi ak aplikasyon an.";
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        MessageTextBlock.Text = string.Empty;
        if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
        {
            MessageTextBlock.Text = "Nouvo modpas yo pa menm.";
            return;
        }

        try
        {
            await _authenticationService.ChangeOwnPasswordAsync(CurrentPasswordBox.Password, NewPasswordBox.Password);
            DialogResult = true;
        }
        catch (ArgumentException exception)
        {
            MessageTextBlock.Text = exception.Message;
        }
        catch (UnauthorizedAccessException)
        {
            MessageTextBlock.Text = "Modpas aktyèl la pa kòrèk.";
        }
        catch (Exception)
        {
            MessageTextBlock.Text = "Modpas la pa t ka chanje. Tanpri eseye ankò.";
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isForced)
            DialogResult = false;
    }
}
