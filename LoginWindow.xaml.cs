using GestionBibliotheque.Services;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace GestionBibliotheque;

public partial class LoginWindow : Window
{
    private readonly AuthenticationService _authenticationService;
    private readonly AuthenticationSession _session;
    private readonly ILogger<LoginWindow> _logger;
    private bool _isFirstRun;
    private int? _lockedUserId;

    public LoginWindow(
        AuthenticationService authenticationService,
        AuthenticationSession session,
        ILogger<LoginWindow> logger)
    {
        InitializeComponent();
        _authenticationService = authenticationService;
        _session = session;
        _logger = logger;
        Loaded += LoginWindow_Loaded;
    }

    public void RequireReauthentication(int userId, string username)
    {
        _lockedUserId = userId;
        UsernameTextBox.Text = username;
        UsernameTextBox.IsEnabled = false;
    }

    private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _isFirstRun = _lockedUserId is null && !await _authenticationService.HasUsersAsync();
            SetupPanel.Visibility = _isFirstRun ? Visibility.Visible : Visibility.Collapsed;
            LoginPanel.Visibility = _isFirstRun ? Visibility.Collapsed : Visibility.Visible;
            ActionButton.Content = _isFirstRun ? "Kreye administratè" : _lockedUserId is null ? "Ouvri sesyon" : "Déverrouiller";
            if (_lockedUserId is not null)
                MessageTextBlock.Text = "Sesyon an fèmen. Antre modpas kont sa a pou kontinye.";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to check authentication setup state.");
            MessageTextBlock.Text = "Bazdone a pa disponib. Tanpri kontakte administratè a.";
            ActionButton.IsEnabled = false;
        }
    }

    private async void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        ActionButton.IsEnabled = false;
        MessageTextBlock.Text = string.Empty;
        try
        {
            var password = _isFirstRun ? SetupPasswordBox.Password : PasswordBox.Password;
            var user = _isFirstRun
                ? await _authenticationService.CreateInitialAdminAsync(UsernameTextBox.Text, password)
                : await _authenticationService.AuthenticateAsync(UsernameTextBox.Text, password);

            if (user is null)
            {
                MessageTextBlock.Text = "Non itilizatè oswa modpas la pa kòrèk.";
                return;
            }

            if (_lockedUserId is int lockedUserId)
            {
                if (user.Id != lockedUserId || !_session.TryReauthenticate(user))
                {
                    MessageTextBlock.Text = "Sèl kont ki te louvri sesyon an ka déverrouiller aplikasyon an.";
                    return;
                }
            }
            else
            {
                _session.SignIn(user);
            }
            if (user.RequirePasswordChange)
            {
                var changePasswordWindow = new ChangePasswordWindow(_authenticationService, _session) { Owner = this };
                if (changePasswordWindow.ShowDialog() != true)
                {
                    _session.SignOut();
                    return;
                }
            }
            DialogResult = true;
        }
        catch (ArgumentException exception)
        {
            MessageTextBlock.Text = exception.Message;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Authentication operation failed for user {Username}.", UsernameTextBox.Text);
            MessageTextBlock.Text = "Operasyon an echwe. Tanpri eseye ankò.";
        }
        finally
        {
            ActionButton.IsEnabled = true;
        }
    }

    private void LoginWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter && ActionButton.IsEnabled)
        {
            ActionButton_Click(ActionButton, new RoutedEventArgs());
            e.Handled = true;
        }
    }
}
