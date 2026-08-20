using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

var assets = @"d:\Desktop\file\project\plugin\src\DeepSeekPet.App\Assets";
var character = Path.Combine(assets, "Character", "normal.png");
var trayPng = Path.Combine(assets, "tray.png");
var trayIco = Path.Combine(assets, "tray.ico");

using var source = new Bitmap(character);
using var head = CropHead(source);
using var png256 = Resize(head, 256);
png256.Save(trayPng, ImageFormat.Png);

using var iconBmp = Resize(head, 32);
SaveIco(iconBmp, trayIco);
Console.WriteLine($"wrote {trayPng} and {trayIco}");

static Bitmap CropHead(Bitmap src)
{
    var (minX, minY, maxX, maxY) = OpaqueBounds(src);
    var bw = maxX - minX + 1;
    var bh = maxY - minY + 1;
    var size = (int)Math.Max(bw * 0.95, bh * 0.5);
    size = Math.Min(size, Math.Min(src.Width, src.Height));
    var cx = minX + bw / 2;
    var left = Math.Clamp(cx - size / 2, 0, src.Width - size);
    var top = Math.Clamp(minY - size / 16, 0, src.Height - size);

    var dest = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(dest);
    g.Clear(Color.Transparent);
    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
    g.DrawImage(src, new Rectangle(0, 0, size, size), new Rectangle(left, top, size, size), GraphicsUnit.Pixel);
    return dest;
}

static (int MinX, int MinY, int MaxX, int MaxY) OpaqueBounds(Bitmap bmp)
{
    var w = bmp.Width;
    var h = bmp.Height;
    var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
    var stride = data.Stride;
    var bytes = new byte[Math.Abs(stride) * h];
    Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
    bmp.UnlockBits(data);

    var minX = w;
    var minY = h;
    var maxX = 0;
    var maxY = 0;
    for (var y = 0; y < h; y++)
    {
        for (var x = 0; x < w; x++)
        {
            if (bytes[y * stride + x * 4 + 3] < 24)
            {
                continue;
            }

            if (x < minX) minX = x;
            if (y < minY) minY = y;
            if (x > maxX) maxX = x;
            if (y > maxY) maxY = y;
        }
    }

    return (minX, minY, maxX, maxY);
}

static Bitmap Resize(Bitmap src, int size)
{
    var dest = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(dest);
    g.Clear(Color.Transparent);
    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
    g.DrawImage(src, 0, 0, size, size);
    return dest;
}

static void SaveIco(Bitmap bmp, string path)
{
    var handle = bmp.GetHicon();
    try
    {
        using var icon = Icon.FromHandle(handle);
        using var clone = (Icon)icon.Clone();
        using var fs = File.Create(path);
        clone.Save(fs);
    }
    finally
    {
        DestroyIcon(handle);
    }
}

[DllImport("user32.dll", SetLastError = true)]
static extern bool DestroyIcon(IntPtr hIcon);
