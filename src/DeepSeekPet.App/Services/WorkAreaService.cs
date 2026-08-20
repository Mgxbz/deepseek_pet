using System.Windows;
using System.Windows.Media;
using DeepSeekPet.App.Native;
using DeepSeekPet.Core.Snap;

namespace DeepSeekPet.App.Services;

internal static class WorkAreaService
{
    public static ScreenWorkArea FromWindow(Window window)
    {
        var dpi = VisualTreeHelper.GetDpi(window);
        var width = window.ActualWidth > 0 ? window.ActualWidth : window.Width;
        var height = window.ActualHeight > 0 ? window.ActualHeight : window.Height;
        var rect = new NativeMethods.RECT
        {
            Left = (int)Math.Round(window.Left * dpi.DpiScaleX),
            Top = (int)Math.Round(window.Top * dpi.DpiScaleY),
            Right = (int)Math.Round((window.Left + width) * dpi.DpiScaleX),
            Bottom = (int)Math.Round((window.Top + height) * dpi.DpiScaleY)
        };

        var monitor = NativeMethods.MonitorFromRect(ref rect, NativeMethods.MonitorDefaultToNearest);
        var info = NativeMethods.MONITORINFO.Create();
        if (monitor == IntPtr.Zero || !NativeMethods.GetMonitorInfo(monitor, ref info))
        {
            return new ScreenWorkArea(
                SystemParameters.WorkArea.Left,
                SystemParameters.WorkArea.Top,
                SystemParameters.WorkArea.Width,
                SystemParameters.WorkArea.Height);
        }

        var work = info.rcWork;
        return new ScreenWorkArea(
            work.Left / dpi.DpiScaleX,
            work.Top / dpi.DpiScaleY,
            (work.Right - work.Left) / dpi.DpiScaleX,
            (work.Bottom - work.Top) / dpi.DpiScaleY);
    }

    public static SnapOptions OptionsFrom(Window window, double magnetDistance)
        => new(magnetDistance, 0.45);
}
