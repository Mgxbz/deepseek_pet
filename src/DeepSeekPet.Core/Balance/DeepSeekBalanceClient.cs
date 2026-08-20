using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeepSeekPet.Core.Balance;

public interface IBalanceClient
{
    Task<BalanceSnapshot> GetBalanceAsync(string apiKey, CancellationToken cancellationToken = default);
}

public sealed class DeepSeekBalanceClient : IBalanceClient
{
    public const string DefaultBaseUrl = "https://api.deepseek.com/";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;

    public DeepSeekBalanceClient(HttpClient http)
    {
        _http = http;
        if (_http.BaseAddress is null)
        {
            _http.BaseAddress = new Uri(DefaultBaseUrl);
        }

        if (_http.Timeout == Timeout.InfiniteTimeSpan || _http.Timeout > TimeSpan.FromSeconds(10))
        {
            _http.Timeout = TimeSpan.FromSeconds(10);
        }
    }

    public async Task<BalanceSnapshot> GetBalanceAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "user/balance");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.Accept.ParseAdd("application/json");

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return BalanceSnapshot.Failed("请求超时");
        }
        catch (HttpRequestException)
        {
            return BalanceSnapshot.Failed("网络失败");
        }

        using (response)
        {
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return BalanceSnapshot.KeyError("密钥无效");
            }

            if (!response.IsSuccessStatusCode)
            {
                return BalanceSnapshot.Failed($"服务返回 {(int)response.StatusCode}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            DeepSeekBalanceResponse? payload;
            try
            {
                payload = await JsonSerializer.DeserializeAsync<DeepSeekBalanceResponse>(stream, JsonOptions, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (JsonException)
            {
                return BalanceSnapshot.Failed("余额数据无法解析");
            }

            if (payload is null)
            {
                return BalanceSnapshot.Failed("余额数据为空");
            }

            var info = PickPreferred(payload.BalanceInfos);
            if (info is null)
            {
                return new BalanceSnapshot(
                    true,
                    false,
                    payload.IsAvailable,
                    null,
                    0,
                    0,
                    0,
                    null);
            }

            return new BalanceSnapshot(
                true,
                false,
                payload.IsAvailable,
                info.Currency,
                ParseMoney(info.TotalBalance),
                ParseMoney(info.GrantedBalance),
                ParseMoney(info.ToppedUpBalance),
                null);
        }
    }

    private static DeepSeekBalanceInfo? PickPreferred(IReadOnlyList<DeepSeekBalanceInfo>? infos)
    {
        if (infos is null || infos.Count == 0)
        {
            return null;
        }

        return infos.FirstOrDefault(i => string.Equals(i.Currency, "CNY", StringComparison.OrdinalIgnoreCase))
               ?? infos.FirstOrDefault(i => string.Equals(i.Currency, "USD", StringComparison.OrdinalIgnoreCase))
               ?? infos[0];
    }

    private static decimal ParseMoney(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        return decimal.TryParse(value, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var amount)
            ? amount
            : 0;
    }

    private sealed class DeepSeekBalanceResponse
    {
        [JsonPropertyName("is_available")]
        public bool IsAvailable { get; set; }

        [JsonPropertyName("balance_infos")]
        public List<DeepSeekBalanceInfo>? BalanceInfos { get; set; }
    }

    private sealed class DeepSeekBalanceInfo
    {
        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("total_balance")]
        public string? TotalBalance { get; set; }

        [JsonPropertyName("granted_balance")]
        public string? GrantedBalance { get; set; }

        [JsonPropertyName("topped_up_balance")]
        public string? ToppedUpBalance { get; set; }
    }
}
