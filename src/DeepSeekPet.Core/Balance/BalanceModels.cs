using DeepSeekPet.Core.Character;

namespace DeepSeekPet.Core.Balance;

public enum BalanceKind
{
    NoKey,
    Loading,
    Ok,
    Low,
    Empty,
    Unavailable,
    Error
}

public sealed record BalanceSnapshot(
    bool Success,
    bool IsKeyError,
    bool IsAvailable,
    string? Currency,
    decimal Total,
    decimal Granted,
    decimal ToppedUp,
    string? ErrorMessage)
{
    public static BalanceSnapshot Loading { get; } = new(
        false, false, false, null, 0, 0, 0, null);

    public static BalanceSnapshot MissingKey { get; } = new(
        false, false, false, null, 0, 0, 0, "先填 API Key");

    public static BalanceSnapshot KeyError(string? message = null) => new(
        false, true, false, null, 0, 0, 0, message ?? "密钥无效");

    public static BalanceSnapshot Failed(string message) => new(
        false, false, false, null, 0, 0, 0, message);
}

public sealed record BalanceUiState(
    BalanceKind Kind,
    PetMood Mood,
    string PrimaryText,
    string DetailText,
    string StatusText,
    bool IsRefreshing,
    string SpendText = "")
{
    public static BalanceUiState NoKey { get; } = new(
        BalanceKind.NoKey,
        PetMood.Sleepy,
        "先填 API Key",
        "在设置里粘贴 DeepSeek 密钥",
        "未配置",
        false);
}

