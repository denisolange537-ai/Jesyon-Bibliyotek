using GestionBibliotheque.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace GestionBibliotheque;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly AuthenticationSession _session;
    private readonly CurrentEmployeeService _currentEmployeeService;
    private readonly BooksView _booksView;
    private readonly MembersView _membersView;
    private readonly BorrowingView _borrowingView;
    private readonly ReturnsView _returnsView;
    private readonly ReservationsView _reservationsView;
    private readonly DashboardView _dashboardView;
    private readonly EmployeesView _employeesView;
    private readonly CompanyManagementView _companyManagementView;
    private readonly PayrollView _payrollView;
    private readonly IServiceProvider _services;
    private readonly LibrarySettingsService _settingsService;
    private readonly AuthorizationService _authorization;
    private readonly DispatcherTimer _sessionExpiryTimer;
    private bool _isDarkTheme;

    public MainWindow(AuthenticationSession session, CurrentEmployeeService currentEmployeeService, AuthorizationService authorization, BooksView booksView, MembersView membersView, BorrowingView borrowingView, ReturnsView returnsView, ReservationsView reservationsView, EmployeesView employeesView, CompanyManagementView companyManagementView, PayrollView payrollView, DashboardView dashboardView, IServiceProvider services, LibrarySettingsService settingsService)
    {
        InitializeComponent();
        _session = session;
        _currentEmployeeService = currentEmployeeService;
        _booksView = booksView;
        _membersView = membersView;
        _borrowingView = borrowingView;
        _returnsView = returnsView;
        _reservationsView = reservationsView;
        _employeesView = employeesView;
        _companyManagementView = companyManagementView;
        _payrollView = payrollView;
        _dashboardView = dashboardView;
        _services = services;
        _settingsService = settingsService;
        _authorization = authorization;
        PreviewMouseDown += (_, _) => _session.RecordActivity();
        PreviewKeyDown += (_, _) => _session.RecordActivity();
        _sessionExpiryTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _sessionExpiryTimer.Tick += SessionExpiryTimer_Tick;
        _sessionExpiryTimer.Start();
        Closed += (_, _) => _sessionExpiryTimer.Stop();
        BooksButton.Visibility = CanNavigate(Permissions.BooksView);
        MembersButton.Visibility = CanNavigate(Permissions.MembersView);
        BorrowingButton.Visibility = CanNavigate(Permissions.BorrowingView);
        ReturnsButton.Visibility = CanNavigate(Permissions.BorrowingView);
        ReservationsButton.Visibility = CanNavigate(Permissions.ReservationsView);
        EmployeesButton.Visibility = CanNavigate(Permissions.EmployeesView);
        RolesButton.Visibility = CanNavigate(Permissions.RolesView);
        CompanyButton.Visibility = CanNavigate(Permissions.CompanyView);
        PayrollButton.Visibility = CanNavigate(Permissions.PayrollView);
        ReportsButton.Visibility = CanNavigate(Permissions.ReportsView);
        SettingsButton.Visibility = CanNavigate(Permissions.SettingsView);
        Loaded += MainWindow_Loaded;
        var employee = _currentEmployeeService.Current;
        UserTextBlock.Text = employee is null ? string.Empty : $"{employee.FullName} · {employee.JobTitle ?? ""} · {employee.Role}";
        ContentArea.Content = _dashboardView;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var settings = await _settingsService.GetAsync();
            ApplyTheme(settings.Theme == "Dark");
            Title = settings.ApplicationName;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Unable to load shell settings: {exception.Message}");
        }
    }

    private async void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        if (_session.CurrentUser is { } user)
        {
            try { await _services.GetRequiredService<AuthenticationService>().RecordSessionEndAsync(user.Id, timedOut: false); }
            catch (Exception exception) { System.Diagnostics.Debug.WriteLine($"Unable to audit logout: {exception.Message}"); }
        }
        _session.SignOut();
        Close();
    }

    private async void SessionExpiryTimer_Tick(object? sender, EventArgs e)
    {
        if (_session.IsAuthenticated) return;
        _sessionExpiryTimer.Stop();
        if (_session.TakeExpiredUser() is not { } expiredUser)
        {
            Close();
            return;
        }

        try { await _services.GetRequiredService<AuthenticationService>().RecordSessionEndAsync(expiredUser.Id, timedOut: true); }
        catch (Exception exception) { System.Diagnostics.Debug.WriteLine($"Unable to audit session timeout: {exception.Message}"); }

        var lockWindow = _services.GetRequiredService<LoginWindow>();
        lockWindow.RequireReauthentication(expiredUser.Id, expiredUser.Username);
        lockWindow.Owner = this;
        if (lockWindow.ShowDialog() != true)
        {
            _session.SignOut();
            Close();
            return;
        }

        _sessionExpiryTimer.Start();
    }

    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        PageTitleTextBlock.Text = "Akèy";
        ContentArea.Content = _dashboardView;
    }

    private void BooksButton_Click(object sender, RoutedEventArgs e)
    {
        if (!HasPermission(Permissions.BooksView)) return;
        PageTitleTextBlock.Text = "Liv";
        ContentArea.Content = _booksView;
    }

    private void MembersButton_Click(object sender, RoutedEventArgs e)
    {
        if (!HasPermission(Permissions.MembersView)) return;
        PageTitleTextBlock.Text = "Manm";
        ContentArea.Content = _membersView;
    }

    private void BorrowingButton_Click(object sender, RoutedEventArgs e)
    {
        if (!HasPermission(Permissions.BorrowingView)) return;
        PageTitleTextBlock.Text = "Prete";
        ContentArea.Content = _borrowingView;
    }

    private void ReturnsButton_Click(object sender, RoutedEventArgs e)
    {
        if (!HasPermission(Permissions.BorrowingView)) return;
        PageTitleTextBlock.Text = "Retou + Amann";
        ContentArea.Content = _returnsView;
    }

    private void ReservationsButton_Click(object sender, RoutedEventArgs e)
    {
        if (!HasPermission(Permissions.ReservationsView)) return;
        PageTitleTextBlock.Text = "Rezèvasyon";
        ContentArea.Content = _reservationsView;
    }

    private void EmployeesButton_Click(object sender, RoutedEventArgs e)
    {
        if (!HasPermission(Permissions.EmployeesView)) return;
        PageTitleTextBlock.Text = "Anplwaye";
        ContentArea.Content = _employeesView;
    }

    private void RolesButton_Click(object sender, RoutedEventArgs e)
    {
        if (!HasPermission(Permissions.RolesView)) return;
        PageTitleTextBlock.Text = "Wòl ak pèmisyon";
        ContentArea.Content = _services.GetRequiredService<RolesView>();
    }

    private void CompanyButton_Click(object sender, RoutedEventArgs e)
    {
        if (!HasPermission(Permissions.CompanyView)) return;
        PageTitleTextBlock.Text = "Company";
        ContentArea.Content = _companyManagementView;
    }

    private void PayrollButton_Click(object sender, RoutedEventArgs e)
    {
        if (!HasPermission(Permissions.PayrollView)) return;
        PageTitleTextBlock.Text = "Payroll";
        ContentArea.Content = _payrollView;
    }

    private void ReportsButton_Click(object sender, RoutedEventArgs e)
    {
        if (!HasPermission(Permissions.ReportsView)) return;
        try
        {
            PageTitleTextBlock.Text = "Rapò";
            ContentArea.Content = _services.GetRequiredService<ReportsView>();
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Rapò yo pa t ka louvri: {exception.Message}", "Erè rapò", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (!HasPermission(Permissions.SettingsView)) return;
        try
        {
            PageTitleTextBlock.Text = "Paramèt";
            ContentArea.Content = _services.GetRequiredService<SettingsView>();
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Paramèt yo pa t ka louvri: {exception.Message}", "Erè paramèt", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        _isDarkTheme = !_isDarkTheme;
        ApplyTheme(_isDarkTheme);
        ((Button)sender).Content = _isDarkTheme ? "Tèm: Fènwa" : "Tèm: Klè";
    }

    private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        var window = new ChangePasswordWindow(_services.GetRequiredService<AuthenticationService>(), _session)
        {
            Owner = this
        };
        window.ShowDialog();
    }

    private void ApplyTheme(bool isDark)
    {
        _isDarkTheme = isDark;
        var resources = Application.Current.Resources;
        resources["ShellBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#172426" : "#F4F1EA"));
        resources["PanelBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#233436" : "#FFFFFF"));
        resources["PrimaryText"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#F4F1EA" : "#173033"));
        resources["MutedText"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#B5C8C5" : "#657477"));
    }

    private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            LogoutButton_Click(this, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        if ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == 0) return;
        switch (e.Key)
        {
            case System.Windows.Input.Key.D1: HomeButton_Click(this, new RoutedEventArgs()); break;
            case System.Windows.Input.Key.D2: BooksButton_Click(this, new RoutedEventArgs()); break;
            case System.Windows.Input.Key.D3: MembersButton_Click(this, new RoutedEventArgs()); break;
            case System.Windows.Input.Key.D4: BorrowingButton_Click(this, new RoutedEventArgs()); break;
            case System.Windows.Input.Key.D5: ReturnsButton_Click(this, new RoutedEventArgs()); break;
            case System.Windows.Input.Key.D6: ReservationsButton_Click(this, new RoutedEventArgs()); break;
            case System.Windows.Input.Key.D7: ReportsButton_Click(this, new RoutedEventArgs()); break;
            case System.Windows.Input.Key.D8: SettingsButton_Click(this, new RoutedEventArgs()); break;
            case System.Windows.Input.Key.D9: CompanyButton_Click(this, new RoutedEventArgs()); break;
            case System.Windows.Input.Key.D0: PayrollButton_Click(this, new RoutedEventArgs()); break;
            default: return;
        }
        e.Handled = true;
    }

    private Visibility CanNavigate(string permission) => _authorization.HasPermission(permission) ? Visibility.Visible : Visibility.Collapsed;
    private bool HasPermission(string permission) => _authorization.HasPermission(permission);
}
