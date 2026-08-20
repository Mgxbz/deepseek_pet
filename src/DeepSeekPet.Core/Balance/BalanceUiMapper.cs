using System.Globalization;
using DeepSeekPet.Core.Character;

namespace DeepSeekPet.Core.Balance;

public static class BalanceUiMapper
{
    public static BalanceUiState Map(
        BalanceSnapshot? snapshot,
        bool hasKey,
        bool isRefreshing,
        decimal lowThreshold,
        decimal spentToday = 0)
    {
        if (!hasKey)
        {
            return BalanceUiState.NoKey with { IsRefreshing = false };
        }

        if (snapshot is null || (!snapshot.Success && isRefreshing && snapshot.ErrorMessage is null))
        {
            return new BalanceUiState(
                BalanceKind.Loading,
                PetMood.Loading,
                "刷新中…",
                "正在读取 DeepSeek 余额",
                "请稍候",
                true);
        }

        if (!snapshot.Success)
        {
            return new BalanceUiState(
                BalanceKind.Error,
                PetMood.Confused,
                "暂时读不到",
                snapshot.ErrorMessage ?? "请稍后重试",
                snapshot.IsKeyError ? "密钥无效" : "网络或服务异常",
                isRefreshing);
        }

        var symbol = CurrencySymbol(snapshot.Currency);
        var primary = $"{symbol} {snapshot.Total.ToString("0.00", CultureInfo.InvariantCulture)}";
        var detail =
            $"赠送 {symbol} {snapshot.Granted.ToString("0.00", CultureInfo.InvariantCulture)}  ·  充值 {symbol} {snapshot.ToppedUp.ToString("0.00", CultureInfo.InvariantCulture)}";
        var spend = $"今日使用 {symbol} {spentToday.ToString("0.00", CultureInfo.InvariantCulture)}";

        if (!snapshot.IsAvailable)
        {
            return new BalanceUiState(
                BalanceKind.Unavailable,
                PetMood.Sad,
                primary,
                detail,
                "当前不可用于 API",
                isRefreshing,
                spend);
        }

        if (snapshot.Total <= 0)
        {
            return new BalanceUiState(
                BalanceKind.Empty,
                PetMood.Sad,
                primary,
                detail,
                "余额已用完",
                isRefreshing,
                spend);
        }

        if (snapshot.Total < lowThreshold)
        {
            return new BalanceUiState(
                BalanceKind.Low,
                PetMood.Worry,
                primary,
                detail,
                "余额偏低",
                isRefreshing,
                spend);
        }

        return new BalanceUiState(
            BalanceKind.Ok,
            PetMood.Happy,
            primary,
            detail,
            "可用于 API",
            isRefreshing,
            spend);
    }

    public static string CurrencySymbol(string? currency) =>
        string.Equals(currency, "USD", StringComparison.OrdinalIgnoreCase) ? "$" : "¥";
}
