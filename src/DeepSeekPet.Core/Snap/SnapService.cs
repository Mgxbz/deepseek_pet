namespace DeepSeekPet.Core.Snap;

public static class SnapService
{
    public static SnapResult SnapOnRelease(RectD window, ScreenWorkArea work, SnapOptions options)
    {
        var (edge, distance) = MeasureNearestEdge(window, work);
        if (distance > options.MagnetDistance)
        {
            var clamped = Clamp(window, work);
            return new SnapResult(SnapKind.Free, null, clamped.X, clamped.Y);
        }

        return Dock(window, work, edge);
    }

    public static SnapResult Dock(RectD window, ScreenWorkArea work, DockEdge edge)
    {
        var x = window.X;
        var y = window.Y;

        switch (edge)
        {
            case DockEdge.Left:
                x = work.Left;
                y = ClampAxis(window.Y, window.Height, work.Top, work.Bottom);
                break;
            case DockEdge.Right:
                x = work.Right - window.Width;
                y = ClampAxis(window.Y, window.Height, work.Top, work.Bottom);
                break;
            case DockEdge.Top:
                y = work.Top;
                x = ClampAxis(window.X, window.Width, work.Left, work.Right);
                break;
            case DockEdge.Bottom:
                y = work.Bottom - window.Height;
                x = ClampAxis(window.X, window.Width, work.Left, work.Right);
                break;
        }

        return new SnapResult(SnapKind.Docked, edge, x, y);
    }

    public static SnapResult Hide(RectD window, ScreenWorkArea work, DockEdge edge, SnapOptions options)
    {
        var peek = PeekPixels(window, edge, options);
        var docked = Dock(window, work, edge);

        return edge switch
        {
            DockEdge.Left => docked with
            {
                Kind = SnapKind.Hidden,
                X = work.Left - window.Width + peek
            },
            DockEdge.Right => docked with
            {
                Kind = SnapKind.Hidden,
                X = work.Right - peek
            },
            DockEdge.Top => docked with
            {
                Kind = SnapKind.Hidden,
                Y = work.Top - window.Height + peek
            },
            DockEdge.Bottom => docked with
            {
                Kind = SnapKind.Hidden,
                Y = work.Bottom - peek
            },
            _ => docked
        };
    }

    public static double PeekPixels(RectD window, DockEdge edge, SnapOptions options)
    {
        var ratio = Math.Clamp(options.PeekRatio, 0.25, 0.7);
        var size = edge is DockEdge.Top or DockEdge.Bottom ? window.Height : window.Width;
        var max = Math.Max(96, size - 8);
        return Math.Clamp(size * ratio, Math.Min(96, max), max);
    }

    public static double InwardPull(RectD window, SnapResult hidden, DockEdge edge)
    {
        return edge switch
        {
            DockEdge.Right => hidden.X - window.X,
            DockEdge.Left => window.X - hidden.X,
            DockEdge.Bottom => hidden.Y - window.Y,
            DockEdge.Top => window.Y - hidden.Y,
            _ => 0
        };
    }

    public static SnapResult HideNearest(RectD window, ScreenWorkArea work, SnapOptions options)
    {
        var (edge, _) = MeasureNearestEdge(window, work);
        return Hide(window, work, edge, options);
    }

    public static DockEdge NearestEdge(RectD window, ScreenWorkArea work)
        => MeasureNearestEdge(window, work).Edge;

    public static RectD Clamp(RectD window, ScreenWorkArea work)
    {
        var x = ClampAxis(window.X, window.Width, work.Left, work.Right);
        var y = ClampAxis(window.Y, window.Height, work.Top, work.Bottom);
        return window with { X = x, Y = y };
    }

    public static bool Intersects(RectD window, ScreenWorkArea work, double minOverlap = 32)
    {
        var left = Math.Max(window.Left, work.Left);
        var top = Math.Max(window.Top, work.Top);
        var right = Math.Min(window.Right, work.Right);
        var bottom = Math.Min(window.Bottom, work.Bottom);
        var width = Math.Max(0, right - left);
        var height = Math.Max(0, bottom - top);
        return width * height >= minOverlap * minOverlap;
    }

    private static (DockEdge Edge, double Distance) MeasureNearestEdge(RectD window, ScreenWorkArea work)
    {
        var dLeft = Math.Abs(window.Left - work.Left);
        var dRight = Math.Abs(window.Right - work.Right);
        var dTop = Math.Abs(window.Top - work.Top);
        var dBottom = Math.Abs(window.Bottom - work.Bottom);

        var edge = DockEdge.Left;
        var distance = dLeft;

        if (dRight < distance)
        {
            edge = DockEdge.Right;
            distance = dRight;
        }

        if (dTop < distance)
        {
            edge = DockEdge.Top;
            distance = dTop;
        }

        if (dBottom < distance)
        {
            edge = DockEdge.Bottom;
            distance = dBottom;
        }

        return (edge, distance);
    }

    private static double ClampAxis(double origin, double size, double min, double max)
    {
        var span = max - min;
        if (size >= span)
        {
            return min;
        }

        if (origin < min)
        {
            return min;
        }

        if (origin + size > max)
        {
            return max - size;
        }

        return origin;
    }
}
