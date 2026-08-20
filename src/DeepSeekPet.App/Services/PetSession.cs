using System.Net.Http;
using DeepSeekPet.Core.Balance;
using DeepSeekPet.Core.Settings;

namespace DeepSeekPet.App.Services;

public sealed class PetSession : IDisposable
{
    private readonly HttpClient _http;

    public AppSettings Settings { get; }
    public SettingsStore Store { get; }
    public BalanceMonitor Monitor { get; }

    public event Action<BalanceUiState>? BalanceChanged;
    public event Action? SettingsChanged;

    public PetSession()
    {
        Store = SettingsStore.Default;
        Settings = Store.Load();
        _http = new HttpClient
        {
            BaseAddress = new Uri(DeepSeekBalanceClient.DefaultBaseUrl),
            Timeout = TimeSpan.FromSeconds(10)
        };
        Monitor = new BalanceMonitor(new DeepSeekBalanceClient(_http), Settings, () => Store.Save(Settings));
        Monitor.StateChanged += (_, state) => BalanceChanged?.Invoke(state);
    }

    public void Start() => Monitor.Start();

    public void Save() => Store.Save(Settings);

    public void ApplySettings()
    {
        StartupRegistration.Apply(Settings.StartWithWindows);
        Monitor.UpdateSettings(Settings);
        Save();
        SettingsChanged?.Invoke();
    }

    public void Dispose()
    {
        Monitor.Dispose();
        _http.Dispose();
    }
}
