using System.Globalization;
using System.Windows;
using DeepSeekPet.App.Services;
using DeepSeekPet.Core.Settings;

namespace DeepSeekPet.App;

public partial class SettingsWindow : Window
{
    private readonly PetSession _session;

    public SettingsWindow(PetSession session)
    {
        _session = session;
        InitializeComponent();
        LoadFromSettings(_session.Settings);
    }

    public static void ShowFor(PetSession session, Window? owner)
    {
        var window = new SettingsWindow(session);
        if (owner is { IsVisible: true })
        {
            window.Owner = owner;
        }

        window.ShowDialog();
    }

    private void LoadFromSettings(AppSettings settings)
    {
        KeyHint.Text = string.IsNullOrWhiteSpace(settings.ApiKey)
            ? "在 platform.deepseek.com 创建密钥后粘贴到这里。"
            : "已保存密钥。留空则保持不变，输入新值则替换。";
        IntervalBox.Text = settings.ClampedRefreshIntervalSeconds.ToString(CultureInfo.InvariantCulture);
        ThresholdBox.Text = settings.LowBalanceThreshold.ToString(CultureInfo.InvariantCulture);
        OpacitySlider.Value = settings.ClampedOpacity;
        ScaleSlider.Value = settings.ClampedScale;
        MagnetSlider.Value = settings.ClampedMagnetDistance;
        TopmostBox.IsChecked = settings.AlwaysOnTop;
        AutoHideBox.IsChecked = settings.AutoHideWhenDocked;
        StartupBox.IsChecked = settings.StartWithWindows;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var settings = _session.Settings;
        var typedKey = ApiKeyBox.Password.Trim();
        if (!string.IsNullOrWhiteSpace(typedKey))
        {
            settings.ApiKey = typedKey;
        }

        if (int.TryParse(IntervalBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var interval))
        {
            settings.RefreshIntervalSeconds = interval;
        }

        if (decimal.TryParse(ThresholdBox.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var threshold)
            || decimal.TryParse(ThresholdBox.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out threshold))
        {
            settings.LowBalanceThreshold = Math.Max(0, threshold);
        }

        settings.Opacity = OpacitySlider.Value;
        settings.Scale = ScaleSlider.Value;
        settings.MagnetDistance = MagnetSlider.Value;
        settings.AlwaysOnTop = TopmostBox.IsChecked == true;
        settings.AutoHideWhenDocked = AutoHideBox.IsChecked == true;
        settings.StartWithWindows = StartupBox.IsChecked == true;
        _session.ApplySettings();
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
