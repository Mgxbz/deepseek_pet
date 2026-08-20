namespace DeepSeekPet.Core.Snap;

public readonly record struct ScreenWorkArea(double X, double Y, double Width, double Height)
{
    public double Left => X;
    public double Top => Y;
    public double Right => X + Width;
    public double Bottom => Y + Height;

    public RectD ToRect() => new(X, Y, Width, Height);
}
