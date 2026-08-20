using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

var dir = args.Length > 0
    ? args[0]
    : @"d:\Desktop\file\project\plugin\src\DeepSeekPet.App\Assets\Character";

foreach (var name in new[] { "normal.png", "low.png", "peek.png" })
{
    var path = Path.Combine(dir, name);
    Punch(path);
    Console.WriteLine($"punched {path}");
}

static void Punch(string path)
{
    Bitmap original = new(path);
    var bmp = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppArgb);
    using (var g = Graphics.FromImage(bmp))
    {
        g.DrawImage(original, 0, 0, original.Width, original.Height);
    }

    original.Dispose();

    var w = bmp.Width;
    var h = bmp.Height;
    var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
    var stride = data.Stride;
    var bytes = new byte[Math.Abs(stride) * h];
    Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);

    bool IsBg(int x, int y)
    {
        var i = y * stride + x * 4;
        var b = bytes[i];
        var g = bytes[i + 1];
        var r = bytes[i + 2];
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        return max >= 228 && max - min <= 22;
    }

    void Clear(int x, int y)
    {
        var i = y * stride + x * 4;
        bytes[i] = 0;
        bytes[i + 1] = 0;
        bytes[i + 2] = 0;
        bytes[i + 3] = 0;
    }

    var seen = new bool[w * h];
    var queue = new Queue<(int X, int Y)>();

    void Enqueue(int x, int y)
    {
        var key = y * w + x;
        if (seen[key])
        {
            return;
        }

        seen[key] = true;
        queue.Enqueue((x, y));
    }

    for (var x = 0; x < w; x++)
    {
        if (IsBg(x, 0)) Enqueue(x, 0);
        if (IsBg(x, h - 1)) Enqueue(x, h - 1);
    }

    for (var y = 0; y < h; y++)
    {
        if (IsBg(0, y)) Enqueue(0, y);
        if (IsBg(w - 1, y)) Enqueue(w - 1, y);
    }

    while (queue.Count > 0)
    {
        var (x, y) = queue.Dequeue();
        if (!IsBg(x, y))
        {
            continue;
        }

        Clear(x, y);
        if (x > 0) Enqueue(x - 1, y);
        if (x + 1 < w) Enqueue(x + 1, y);
        if (y > 0) Enqueue(x, y - 1);
        if (y + 1 < h) Enqueue(x, y + 1);
    }

    // Feather a 1px halo of leftover checkerboard stuck to the silhouette.
    for (var y = 1; y < h - 1; y++)
    {
        for (var x = 1; x < w - 1; x++)
        {
            var i = y * stride + x * 4;
            if (bytes[i + 3] == 0 || !IsBg(x, y))
            {
                continue;
            }

            if (bytes[(y * stride) + (x - 1) * 4 + 3] == 0
                || bytes[(y * stride) + (x + 1) * 4 + 3] == 0
                || bytes[((y - 1) * stride) + x * 4 + 3] == 0
                || bytes[((y + 1) * stride) + x * 4 + 3] == 0)
            {
                Clear(x, y);
            }
        }
    }

    Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
    bmp.UnlockBits(data);
    bmp.Save(path, ImageFormat.Png);
    bmp.Dispose();
}
