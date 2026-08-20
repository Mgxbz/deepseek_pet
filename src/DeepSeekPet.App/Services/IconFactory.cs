using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using DeepSeekPet.App.Native;

namespace DeepSeekPet.App.Services;

internal static class IconFactory
{
    public static Icon CreateTrayIcon()
    {
        using var source = LoadTrayPng();
        using var sized = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(sized))
        {
            g.Clear(Color.Transparent);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(source, 0, 0, 32, 32);
        }

        var handle = sized.GetHicon();
        try
        {
            using var tmp = Icon.FromHandle(handle);
            return (Icon)tmp.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(handle);
        }
    }

    private static Bitmap LoadTrayPng()
    {
        var uri = new Uri("pack://application:,,,/Assets/tray.png", UriKind.Absolute);
        using var stream = System.Windows.Application.GetResourceStream(uri)?.Stream
                           ?? throw new InvalidOperationException("Missing tray.png resource.");
        using var temp = new Bitmap(stream);
        return new Bitmap(temp);
    }
}
