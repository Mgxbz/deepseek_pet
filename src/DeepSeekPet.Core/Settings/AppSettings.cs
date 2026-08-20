using System.Text.Json.Serialization;
using DeepSeekPet.Core.Snap;

namespace DeepSeekPet.Core.Settings;

public sealed class AppSettings
{
    public const int MinRefreshSeconds = 30;
    public const int MaxRefreshSeconds = 600;

    public string? ApiKeyProtected { get; set; }

    [JsonIgnore]
    public string? ApiKey { get; set; }

    public int RefreshIntervalSeconds { get; set; } = 60;

    public decimal LowBalanceThreshold { get; set; } = 10m;

    public bool AlwaysOnTop { get; set; } = true;

    public double Opacity { get; set; } = 1.0;

    public double Scale { get; set; } = 1.0;

    public double MagnetDistance { get; set; } = 32;

    public bool AutoHideWhenDocked { get; set; }

    public bool StartWithWindows { get; set; }

    public double? WindowLeft { get; set; }

    public double? WindowTop { get; set; }

    public SnapKind WindowSnapKind { get; set; } = SnapKind.Free;

    public DockEdge? WindowDockEdge { get; set; }

    [JsonIgnore]
    public int ClampedRefreshIntervalSeconds =>
        Math.Clamp(RefreshIntervalSeconds, MinRefreshSeconds, MaxRefreshSeconds);

    [JsonIgnore]
    public double ClampedOpacity => Math.Clamp(Opacity, 0.35, 1.0);

    [JsonIgnore]
    public double ClampedScale => Math.Clamp(Scale, 0.7, 1.6);

    [JsonIgnore]
    public double ClampedMagnetDistance => Math.Clamp(MagnetDistance, 8, 80);
}
