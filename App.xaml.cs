using System.Configuration;
using System.Data;
using GestionBibliotheque.Data;
using GestionBibliotheque.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace GestionBibliotheque;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddLibraryFoundation(builder.Configuration);
        _host = builder.Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var logger = _host.Services.GetRequiredService<ILogger<App>>();
        try
        {
            await _host.StartAsync();
            await _host.Services.GetRequiredService<DatabaseInitializer>().InitializeAsync();
            logger.LogInformation("Application is starting.");

            var loginWindow = _host.Services.GetRequiredService<LoginWindow>();
            MainWindow = loginWindow;
            if (loginWindow.ShowDialog() != true)
            {
                Shutdown();
                return;
            }

            MainWindow = _host.Services.GetRequiredService<MainWindow>();
            MainWindow.Closed += (_, _) => Shutdown();
            MainWindow.Show();
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "Application startup failed.");
            MessageBox.Show(
                "Aplikasyon an pa t ka kòmanse. Verifye bazdone a oswa kontakte administratè a.",
                "Erè demaraj",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        var logger = _host.Services.GetRequiredService<ILogger<App>>();
        logger.LogInformation("Application is stopping.");

        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}

