using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Threading;
using DeepSeekPet.App.Native;
using DeepSeekPet.App.Services;
using DeepSeekPet.App.Tray;
using DeepSeekPet.Core.Balance;
using DeepSeekPet.Core.Snap;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace DeepSeekPet.App;

public partial class MainWindow : Window
{
    private readonly PetSession _session;
    private readonly DispatcherTimer _lookTimer;
    private readonly DispatcherTimer _autoHideTimer;

    private SnapKind _snapKind = SnapKind.Free;
    private DockEdge? _edge;
    private bool _dragging;
    private bool _pulling;
    private Point _dragOffset;
    private BalanceKind? _lastBalloonKind;

    public MainWindow(PetSession session)
    {
        _session = session;
        InitializeComponent();

        _lookTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _lookTimer.Tick += (_, _) => UpdateLookAt();

        _autoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
        _autoHideTimer.Tick += (_, _) =>
        {
            _autoHideTimer.Stop();
            if (_session.Settings.AutoHideWhenDocked && _snapKind == SnapKind.Docked && !_dragging)
            {
                HideToEdge(animate: true);
            }
        };

        _session.BalanceChanged += OnBalanceChanged;
        _session.SettingsChanged += ApplyWindowSettings;
    }

    public void PersistPosition()
    {
        RememberSnap();
        _session.Save();
    }

    public void ToggleVisible()
    {
        if (IsVisible)
        {
            Hide();
        }
        else
        {
            Show();
            PlaceInForeground();
        }
    }

    public void ToggleDockHide()
    {
        if (_snapKind == SnapKind.Hidden)
        {
            ExpandFromEdge();
            return;
        }

        HideToEdge();
    }

    public void HideToEdge(bool animate = true)
    {
        var work = WorkAreaService.FromWindow(this);
        var options = WorkAreaService.OptionsFrom(this, _session.Settings.ClampedMagnetDistance);
        var window = CurrentRect();
        var edge = _edge ?? SnapService.NearestEdge(window, work);
        var result = SnapService.Hide(window, work, edge, options);
        ApplySnap(result, animate);
    }

    public void ExpandFromEdge(bool animate = true)
    {
        if (_snapKind != SnapKind.Hidden || _edge is null)
        {
            return;
        }

        var work = WorkAreaService.FromWindow(this);
        var result = SnapService.Dock(CurrentRect(), work, _edge.Value);
        ApplySnap(result, animate);
        ArmAutoHide();
    }

    internal void OpenSettings() => SettingsWindow.ShowFor(_session, this);

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SizeToContent = SizeToContent.WidthAndHeight;
        ApplyWindowSettings();
        UpdateLayout();
        RestorePosition();
        _lookTimer.Start();
    }

    private void RestorePosition()
    {
        var work = WorkAreaService.FromWindow(this);
        var width = ActualWidth > 0 ? ActualWidth : Width;
        var height = ActualHeight > 0 ? ActualHeight : Height;
        var savedKind = _session.Settings.WindowSnapKind;

        if (_session.Settings.WindowLeft is { } left && _session.Settings.WindowTop is { } top)
        {
            Left = left;
            Top = top;
            if (savedKind != SnapKind.Hidden && !SnapService.Intersects(CurrentRect(), work))
            {
                Left = work.Right - width - 20;
                Top = work.Bottom - height - 20;
            }
        }
        else
        {
            Left = work.Right - width - 20;
            Top = work.Bottom - height - 20;
        }

        Dispatcher.BeginInvoke(ApplyRestoredSnap, DispatcherPriority.Loaded);
    }

    private void ApplyRestoredSnap()
    {
        var work = WorkAreaService.FromWindow(this);
        var options = WorkAreaService.OptionsFrom(this, _session.Settings.ClampedMagnetDistance);
        var window = CurrentRect();
        var edge = _session.Settings.WindowDockEdge;

        var result = (_session.Settings.WindowSnapKind, edge) switch
        {
            (SnapKind.Hidden, { } hiddenEdge) => SnapService.Hide(window, work, hiddenEdge, options),
            (SnapKind.Docked, { } dockEdge) => SnapService.Dock(window, work, dockEdge),
            _ => SnapService.SnapOnRelease(window, work, options)
        };

        ApplySnap(result, animate: false);
        if (result.Kind == SnapKind.Docked)
        {
            ArmAutoHide();
        }
    }

    private void ApplyWindowSettings()
    {
        Topmost = _session.Settings.AlwaysOnTop;
        Opacity = _session.Settings.ClampedOpacity;
        var scale = _session.Settings.ClampedScale;
        LayoutRoot.LayoutTransform = new ScaleTransform(scale, scale);
    }

    private void OnBalanceChanged(BalanceUiState state)
    {
        if (Dispatcher.HasShutdownStarted)
        {
            return;
        }

        _ = Dispatcher.BeginInvoke(() =>
        {
            if (!IsLoaded)
            {
                return;
            }

            PrimaryText.Text = state.PrimaryText;
            StatusText.Text = state.IsRefreshing ? $"{state.StatusText} · 刷新中" : state.StatusText;
            DetailRun.Text = state.DetailText;
            Pet.SetMood(state.Mood);
            MaybeBalloon(state);
        });
    }

    private void MaybeBalloon(BalanceUiState state)
    {
        if (state.Kind is not (BalanceKind.Empty or BalanceKind.Unavailable))
        {
            if (state.Kind is BalanceKind.Ok or BalanceKind.Low)
            {
                _lastBalloonKind = null;
            }

            return;
        }

        if (_lastBalloonKind == state.Kind)
        {
            return;
        }

        _lastBalloonKind = state.Kind;
        TrayIconService.ShowWarning(state.Kind == BalanceKind.Empty
            ? "DeepSeek 余额已用完"
            : "DeepSeek 余额当前不可用于 API");
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source && IsInsideBubble(source))
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            ToggleHide();
            e.Handled = true;
            return;
        }

        StopWindowAnimation();
        _dragging = true;
        _pulling = _snapKind == SnapKind.Hidden && _edge is not null;
        _autoHideTimer.Stop();
        _dragOffset = e.GetPosition(this);
        CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var dpi = VisualTreeHelper.GetDpi(this);
        var screen = PointToScreen(e.GetPosition(this));
        var x = screen.X / dpi.DpiScaleX - _dragOffset.X;
        var y = screen.Y / dpi.DpiScaleY - _dragOffset.Y;

        if (_pulling && _edge is { } edge)
        {
            MoveAlongEdge(edge, x, y);
            if (!TryBreakPull(edge))
            {
                return;
            }
        }

        Left = x;
        Top = y;
    }

    private void MoveAlongEdge(DockEdge edge, double x, double y)
    {
        var work = WorkAreaService.FromWindow(this);
        var options = WorkAreaService.OptionsFrom(this, _session.Settings.ClampedMagnetDistance);
        var window = CurrentRect();
        var hidden = SnapService.Hide(window, work, edge, options);
        var along = SnapService.Clamp(window with { X = x, Y = y }, work);

        switch (edge)
        {
            case DockEdge.Right:
                Left = Math.Min(x, hidden.X);
                Top = along.Y;
                break;
            case DockEdge.Left:
                Left = Math.Max(x, hidden.X);
                Top = along.Y;
                break;
            case DockEdge.Bottom:
                Top = Math.Min(y, hidden.Y);
                Left = along.X;
                break;
            case DockEdge.Top:
                Top = Math.Max(y, hidden.Y);
                Left = along.X;
                break;
        }
    }

    private bool TryBreakPull(DockEdge edge)
    {
        var work = WorkAreaService.FromWindow(this);
        var options = WorkAreaService.OptionsFrom(this, _session.Settings.ClampedMagnetDistance);
        var window = CurrentRect();
        var hidden = SnapService.Hide(window, work, edge, options);
        if (SnapService.InwardPull(window, hidden, edge) < SnapOptions.PullOutThreshold)
        {
            return false;
        }

        _pulling = false;
        _snapKind = SnapKind.Free;
        _edge = null;
        Pet.SetPeek(false);
        Pet.SetFlip(false, false);
        BubbleHost.Visibility = Visibility.Visible;
        UpdateLayout();
        return true;
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        var pulling = _pulling;
        _dragging = false;
        _pulling = false;
        ReleaseMouseCapture();

        if (pulling && _edge is { } edge)
        {
            FinishPull(edge);
        }
        else
        {
            SnapOnRelease();
        }

        e.Handled = true;
    }

    private void FinishPull(DockEdge edge)
    {
        var work = WorkAreaService.FromWindow(this);
        var options = WorkAreaService.OptionsFrom(this, _session.Settings.ClampedMagnetDistance);
        var window = CurrentRect();
        var hidden = SnapService.Hide(window, work, edge, options);
        var pulled = SnapService.InwardPull(window, hidden, edge);

        if (pulled >= SnapOptions.PullOutThreshold)
        {
            SnapOnRelease();
            return;
        }

        ApplySnap(hidden, animate: true);
    }

    private void OnMouseLeave(object sender, MouseEventArgs e) => ArmAutoHide();

    private void OnBubbleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount > 1)
        {
            e.Handled = true;
            return;
        }

        if (e.OriginalSource is DependencyObject source && IsInsideUsageLink(source))
        {
            return;
        }

        if (!_session.Monitor.TryManualRefresh())
        {
            StatusText.Text = "刷新太快了，稍等几秒";
        }

        e.Handled = true;
    }

    private void OnBubbleMouseEnter(object sender, MouseEventArgs e)
        => DetailPanel.Visibility = Visibility.Visible;

    private void OnBubbleMouseLeave(object sender, MouseEventArgs e)
        => DetailPanel.Visibility = Visibility.Collapsed;

    private void ToggleHide()
    {
        if (_snapKind == SnapKind.Hidden)
        {
            ExpandFromEdge();
            return;
        }

        HideToEdge();
    }

    private void SnapOnRelease()
    {
        var work = WorkAreaService.FromWindow(this);
        var options = WorkAreaService.OptionsFrom(this, _session.Settings.ClampedMagnetDistance);
        var result = SnapService.SnapOnRelease(CurrentRect(), work, options);
        ApplySnap(result, animate: true);
        ArmAutoHide();
    }

    private void ApplySnap(SnapResult result, bool animate)
    {
        _snapKind = result.Kind;
        _edge = result.Edge;
        Pet.SetPeek(result.Kind == SnapKind.Hidden);
        Pet.SetFlip(_edge is DockEdge.Left, _edge is DockEdge.Top);
        BubbleHost.Visibility = result.Kind == SnapKind.Hidden ? Visibility.Collapsed : Visibility.Visible;
        UpdateLayout();

        if (_edge is { } edge)
        {
            var work = WorkAreaService.FromWindow(this);
            var options = WorkAreaService.OptionsFrom(this, _session.Settings.ClampedMagnetDistance);
            result = result.Kind == SnapKind.Hidden
                ? SnapService.Hide(CurrentRect(), work, edge, options)
                : SnapService.Dock(CurrentRect(), work, edge);
        }

        MoveWindow(result.X, result.Y, animate);
        RememberSnap();
    }

    private void RememberSnap()
    {
        _session.Settings.WindowLeft = Left;
        _session.Settings.WindowTop = Top;
        _session.Settings.WindowSnapKind = _snapKind;
        _session.Settings.WindowDockEdge = _edge;
    }

    private void ArmAutoHide()
    {
        _autoHideTimer.Stop();
        if (_session.Settings.AutoHideWhenDocked && _snapKind == SnapKind.Docked)
        {
            _autoHideTimer.Start();
        }
    }

    private RectD CurrentRect()
    {
        var width = ActualWidth > 0 ? ActualWidth : Width;
        var height = ActualHeight > 0 ? ActualHeight : Height;
        return new RectD(Left, Top, width, height);
    }

    private void MoveWindow(double x, double y, bool animate)
    {
        StopWindowAnimation();
        if (!animate)
        {
            Left = x;
            Top = y;
            return;
        }

        var fromX = Left;
        var fromY = Top;
        Left = x;
        Top = y;
        var duration = TimeSpan.FromMilliseconds(220);
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        BeginAnimation(LeftProperty, new DoubleAnimation(fromX, x, duration) { EasingFunction = ease, FillBehavior = FillBehavior.Stop });
        BeginAnimation(TopProperty, new DoubleAnimation(fromY, y, duration) { EasingFunction = ease, FillBehavior = FillBehavior.Stop });
    }

    private void StopWindowAnimation()
    {
        var left = Left;
        var top = Top;
        BeginAnimation(LeftProperty, null);
        BeginAnimation(TopProperty, null);
        Left = left;
        Top = top;
    }

    private void UpdateLookAt()
    {
        if (_snapKind == SnapKind.Hidden || !IsVisible)
        {
            Pet.SetLookAt(null, null);
            return;
        }

        if (!NativeMethods.GetCursorPos(out var point))
        {
            return;
        }

        var dpi = VisualTreeHelper.GetDpi(this);
        Pet.SetLookAt(point.X / dpi.DpiScaleX, point.Y / dpi.DpiScaleY);
    }

    private void PlaceInForeground()
    {
        var topmost = Topmost;
        Topmost = true;
        Activate();
        Topmost = topmost;
    }

    private bool IsInsideBubble(DependencyObject source)
    {
        DependencyObject? current = source;
        while (current is not null)
        {
            if (ReferenceEquals(current, BubbleHost))
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private static bool IsInsideUsageLink(DependencyObject source)
    {
        DependencyObject? current = source;
        while (current is not null)
        {
            if (current is Hyperlink)
            {
                return true;
            }

            current = LogicalTreeHelper.GetParent(current) as DependencyObject
                      ?? VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private void OnOpenUsage(object sender, RequestNavigateEventArgs e)
    {
        DeepSeekLinks.OpenUsage();
        e.Handled = true;
    }

    private void OnOpenUsageMenu(object sender, RoutedEventArgs e) => DeepSeekLinks.OpenUsage();

    private void OnRefreshMenu(object sender, RoutedEventArgs e) => _session.Monitor.TryManualRefresh();

    private void OnToggleDockMenu(object sender, RoutedEventArgs e) => ToggleDockHide();

    private void OnSettingsMenu(object sender, RoutedEventArgs e) => OpenSettings();

    private void OnExitMenu(object sender, RoutedEventArgs e) => System.Windows.Application.Current.Shutdown();

    protected override void OnClosed(EventArgs e)
    {
        _lookTimer.Stop();
        _autoHideTimer.Stop();
        _session.BalanceChanged -= OnBalanceChanged;
        _session.SettingsChanged -= ApplyWindowSettings;
        base.OnClosed(e);
    }
}
