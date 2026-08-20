namespace DeepSeekPet.Core.Snap;

public enum DockEdge
{
    Left,
    Right,
    Top,
    Bottom
}

public enum SnapKind
{
    Free,
    Docked,
    Hidden
}

public readonly record struct SnapOptions(double MagnetDistance, double PeekRatio)
{
    public static SnapOptions Default { get; } = new(32, 0.45);

    public const double PullOutThreshold = 40;
}

public readonly record struct SnapResult(SnapKind Kind, DockEdge? Edge, double X, double Y);
