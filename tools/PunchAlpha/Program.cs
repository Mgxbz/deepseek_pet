using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

var dir = args.Length > 0
    ? args[0]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSeekPet.App", "Assets", "Character"));

var names = args.Length > 1
    ? args.Skip(1).ToArray()
    : new[] { "peek-tb.png" };

foreach (var name in names)
{
    var path = Path.IsPathRooted(name) ? name : Path.Combine(dir, name);
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
        var chroma = max - min;
        // Near-white background only. Do not treat pale skin / white gloves as fill.
        return max >= 245 && chroma <= 18;
    }

    bool IsCheckerGray(int x, int y)
    {
        var i = y * stride + x * 4;
        var b = bytes[i];
        var g = bytes[i + 1];
        var r = bytes[i + 2];
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        return max - min <= 16 && min >= 168 && max <= 230;
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

    // Feather leftover checkerboard / white halo stuck to the silhouette.
    for (var pass = 0; pass < 3; pass++)
    {
        for (var y = 1; y < h - 1; y++)
        {
            for (var x = 1; x < w - 1; x++)
            {
                var i = y * stride + x * 4;
                if (bytes[i + 3] == 0)
                {
                    continue;
                }

                var nextToClear = bytes[(y * stride) + (x - 1) * 4 + 3] == 0
                    || bytes[(y * stride) + (x + 1) * 4 + 3] == 0
                    || bytes[((y - 1) * stride) + x * 4 + 3] == 0
                    || bytes[((y + 1) * stride) + x * 4 + 3] == 0;
                if (!nextToClear)
                {
                    continue;
                }

                if (IsCheckerGray(x, y))
                {
                    Clear(x, y);
                }
            }
        }
    }

    // Drop leftover full-width ledge pixels (the generated white/black bar).
    for (var y = h - 1; y >= h * 2 / 3; y--)
    {
        var first = -1;
        var last = -1;
        for (var x = 0; x < w; x++)
        {
            if (bytes[y * stride + x * 4 + 3] <= 16)
            {
                continue;
            }

            if (first < 0) first = x;
            last = x;
        }

        if (first == 0 && last == w - 1)
        {
            for (var x = 0; x < w; x++)
            {
                Clear(x, y);
            }
        }
    }

    Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
    bmp.UnlockBits(data);

    using var cropped = CropTransparent(bmp);
    bmp.Dispose();
    cropped.Save(path, ImageFormat.Png);
}

static Bitmap CropTransparent(Bitmap bmp)
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
    var maxX = -1;
    var maxY = -1;
    for (var y = 0; y < h; y++)
    {
        for (var x = 0; x < w; x++)
        {
            if (bytes[y * stride + x * 4 + 3] < 16)
            {
                continue;
            }

            if (x < minX) minX = x;
            if (y < minY) minY = y;
            if (x > maxX) maxX = x;
            if (y > maxY) maxY = y;
        }
    }

    if (maxX < minX)
    {
        return new Bitmap(bmp);
    }

    const int pad = 4;
    const int padBottom = 16;
    minX = Math.Max(0, minX - pad);
    minY = Math.Max(0, minY - pad);
    maxX = Math.Min(w - 1, maxX + pad);
    maxY = Math.Min(h - 1, maxY + padBottom);
    var crop = new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
    var result = new Bitmap(crop.Width, crop.Height, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(result);
    g.DrawImage(bmp, new Rectangle(0, 0, crop.Width, crop.Height), crop, GraphicsUnit.Pixel);
    return result;
}
