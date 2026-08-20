using System.Windows.Forms;
using DeepSeekPet.App.Services;

namespace DeepSeekPet.App.Tray;

internal sealed class TrayIconService : IDisposable
{
    private static TrayIconService? _current;

    private readonly PetSession _session;
    private readonly MainWindow _window;
    private readonly NotifyIcon _icon;
    private readonly ToolStripMenuItem _startupItem;

    public TrayIconService(PetSession session, MainWindow window)
    {
        _session = session;
        _window = window;
        _current = this;

        _startupItem = new ToolStripMenuItem("开机启动")
        {
            CheckOnClick = true,
            Checked = session.Settings.StartWithWindows
        };
        _startupItem.CheckedChanged += (_, _) =>
        {
            _session.Settings.StartWithWindows = _startupItem.Checked;
            _session.ApplySettings();
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("立即刷新", null, (_, _) => _session.Monitor.TryManualRefresh());
        menu.Items.Add("打开用量页", null, (_, _) => DeepSeekLinks.OpenUsage());
        menu.Items.Add("显示 / 隐藏", null, (_, _) => _window.ToggleVisible());
        menu.Items.Add("收起 / 展开边缘", null, (_, _) => ToggleEdge());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("设置", null, (_, _) => _window.OpenSettings());
        menu.Items.Add(_startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => System.Windows.Application.Current.Shutdown());

        _icon = new NotifyIcon
        {
            Text = "余宠 · DeepSeek 余额",
            Icon = IconFactory.CreateTrayIcon(),
            Visible = false,
            ContextMenuStrip = menu
        };
        _icon.DoubleClick += (_, _) =>
        {
            if (!_window.IsVisible)
            {
                _window.ToggleVisible();
            }

            _window.Activate();
        };
    }

    public void Show() => _icon.Visible = true;

    public static void ShowWarning(string message)
    {
        _current?._icon.ShowBalloonTip(4000, "余宠", message, ToolTipIcon.Warning);
    }

    public void Dispose()
    {
        if (ReferenceEquals(_current, this))
        {
            _current = null;
        }

        _icon.Visible = false;
        _icon.Dispose();
    }

    private void ToggleEdge()
    {
        if (!_window.IsVisible)
        {
            _window.Show();
        }

        _window.ToggleDockHide();
    }
}
