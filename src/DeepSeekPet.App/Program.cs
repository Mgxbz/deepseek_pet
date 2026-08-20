using System.Windows.Forms;

namespace DeepSeekPet.App;

internal static class Program
{
    [STAThread]
    public static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
