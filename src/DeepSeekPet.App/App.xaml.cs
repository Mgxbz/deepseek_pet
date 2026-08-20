using System.Windows;
using DeepSeekPet.App.Services;
using DeepSeekPet.App.Tray;

namespace DeepSeekPet.App;

public partial class App : System.Windows.Application
{
    private PetSession? _session;
    private TrayIconService? _tray;
    private MainWindow? _main;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _session = new PetSession();
        StartupRegistration.Apply(_session.Settings.StartWithWindows);

        _main = new MainWindow(_session);
        _tray = new TrayIconService(_session, _main);
        _tray.Show();

        _main.Show();
        _session.Start();
        if (string.IsNullOrWhiteSpace(_session.Settings.ApiKey))
        {
            SettingsWindow.ShowFor(_session, _main);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _main?.PersistPosition();
        _tray?.Dispose();
        _session?.Dispose();
        base.OnExit(e);
    }
}
