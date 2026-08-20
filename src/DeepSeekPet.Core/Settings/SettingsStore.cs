using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeepSeekPet.Core.Settings;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly string _path;

    public SettingsStore(string path)
    {
        _path = path;
    }

    public static SettingsStore Default { get; } = new(
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DeepSeekPet",
            "settings.json"));

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(_path);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            settings.ApiKey = Unprotect(settings.ApiKeyProtected);
            return settings;
        }
        catch (CryptographicException)
        {
            return new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
        catch (IOException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        settings.ApiKeyProtected = Protect(settings.ApiKey);
        var copy = new AppSettings
        {
            ApiKeyProtected = settings.ApiKeyProtected,
            RefreshIntervalSeconds = settings.ClampedRefreshIntervalSeconds,
            LowBalanceThreshold = settings.LowBalanceThreshold,
            AlwaysOnTop = settings.AlwaysOnTop,
            Opacity = settings.ClampedOpacity,
            Scale = settings.ClampedScale,
            MagnetDistance = settings.ClampedMagnetDistance,
            AutoHideWhenDocked = settings.AutoHideWhenDocked,
            StartWithWindows = settings.StartWithWindows,
            WindowLeft = settings.WindowLeft,
            WindowTop = settings.WindowTop,
            WindowSnapKind = settings.WindowSnapKind,
            WindowDockEdge = settings.WindowDockEdge,
            SpendDate = settings.SpendDate,
            SpendStartTotal = settings.SpendStartTotal,
            SpendLastTotal = settings.SpendLastTotal
        };

        var json = JsonSerializer.Serialize(copy, JsonOptions);
        File.WriteAllText(_path, json);
    }

    private static string? Protect(string? plain)
    {
        if (string.IsNullOrWhiteSpace(plain))
        {
            return null;
        }

        var bytes = Encoding.UTF8.GetBytes(plain.Trim());
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    private static string? Unprotect(string? protectedValue)
    {
        if (string.IsNullOrWhiteSpace(protectedValue))
        {
            return null;
        }

        var protectedBytes = Convert.FromBase64String(protectedValue);
        var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }
}
