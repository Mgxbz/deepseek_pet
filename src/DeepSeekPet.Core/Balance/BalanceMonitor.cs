using DeepSeekPet.Core.Character;
using DeepSeekPet.Core.Settings;

namespace DeepSeekPet.Core.Balance;

public sealed class BalanceMonitor : IDisposable
{
    private static readonly TimeSpan[] Backoffs =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(5)
    ];

    private readonly IBalanceClient _client;
    private readonly SemaphoreSlim _kick = new(0, 1);
    private readonly object _gate = new();

    private CancellationTokenSource? _cts;
    private Task? _loop;
    private string? _apiKey;
    private int _intervalSeconds;
    private decimal _lowThreshold;
    private int _failStreak;
    private BalanceSnapshot? _lastSuccess;
    private bool _disposed;

    public event EventHandler<BalanceUiState>? StateChanged;

    public TimeSpan RefreshCooldown { get; } = TimeSpan.FromSeconds(0.5);//刷新一秒冷却

    public DateTimeOffset LastManualRefreshUtc { get; private set; } = DateTimeOffset.MinValue;

    public BalanceMonitor(IBalanceClient client, AppSettings settings)
    {
        _client = client;
        ApplySettings(settings);
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_loop is { IsCompleted: false })
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _loop = Task.Run(() => RunAsync(_cts.Token));
    }

    public void RequestRefresh()
    {
        try
        {
            _kick.Release();
        }
        catch (SemaphoreFullException)
        {
        }
    }

    public bool TryManualRefresh()
    {
        var now = DateTimeOffset.UtcNow;
        if (now - LastManualRefreshUtc < RefreshCooldown)
        {
            return false;
        }

        LastManualRefreshUtc = now;
        RequestRefresh();
        return true;
    }

    public void UpdateSettings(AppSettings settings)
    {
        ApplySettings(settings);
        RequestRefresh();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cts?.Cancel();
        _cts?.Dispose();
        _kick.Dispose();
    }

    private void ApplySettings(AppSettings settings)
    {
        lock (_gate)
        {
            _apiKey = string.IsNullOrWhiteSpace(settings.ApiKey) ? null : settings.ApiKey.Trim();
            _intervalSeconds = settings.ClampedRefreshIntervalSeconds;
            _lowThreshold = settings.LowBalanceThreshold;
        }
    }

    private (string? Key, int Interval, decimal Threshold) SnapshotSettings()
    {
        lock (_gate)
        {
            return (_apiKey, _intervalSeconds, _lowThreshold);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var (key, interval, threshold) = SnapshotSettings();
            if (string.IsNullOrWhiteSpace(key))
            {
                _failStreak = 0;
                _lastSuccess = null;
                Raise(BalanceUiState.NoKey);
                await WaitAsync(Timeout.InfiniteTimeSpan, ct).ConfigureAwait(false);
                continue;
            }

            EmitRefreshing(threshold);

            BalanceSnapshot result;
            try
            {
                result = await _client.GetBalanceAsync(key, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch
            {
                result = BalanceSnapshot.Failed("网络失败");
            }

            TimeSpan delay;
            if (result.Success)
            {
                _failStreak = 0;
                _lastSuccess = result;
                delay = TimeSpan.FromSeconds(interval);
                Raise(BalanceUiMapper.Map(result, true, false, threshold));
            }
            else
            {
                _failStreak = Math.Min(_failStreak + 1, Backoffs.Length);
                delay = Backoffs[Math.Max(0, _failStreak - 1)];
                Raise(BalanceUiMapper.Map(result, true, false, threshold));
            }

            await WaitAsync(delay, ct).ConfigureAwait(false);
        }
    }

    private void EmitRefreshing(decimal threshold)
    {
        if (_lastSuccess is null)
        {
            Raise(BalanceUiMapper.Map(null, true, true, threshold));
            return;
        }

        Raise(BalanceUiMapper.Map(_lastSuccess, true, true, threshold) with
        {
            Mood = PetMood.Loading,
            IsRefreshing = true
        });
    }

    private async Task WaitAsync(TimeSpan delay, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            return;
        }

        try
        {
            if (delay == Timeout.InfiniteTimeSpan)
            {
                await _kick.WaitAsync(ct).ConfigureAwait(false);
                return;
            }

            await _kick.WaitAsync(delay, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
    }

    private void Raise(BalanceUiState state) => StateChanged?.Invoke(this, state);
}
