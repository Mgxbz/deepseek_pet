using System.Diagnostics;

namespace DeepSeekPet.App.Services;

internal static class DeepSeekLinks
{
    public const string Usage = "https://platform.deepseek.com/usage";

    public static void OpenUsage()
    {
        Process.Start(new ProcessStartInfo(Usage) { UseShellExecute = true });
    }
}
