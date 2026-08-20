using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using DeepSeekPet.Core.Character;
using UserControl = System.Windows.Controls.UserControl;

namespace DeepSeekPet.App.Character;

public partial class PetCharacter : UserControl, ICharacterView
{
    private static readonly ImageSource NormalSprite = Load("normal.png");
    private static readonly ImageSource LowSprite = Load("low.png");
    private static readonly ImageSource PeekSprite = Load("peek.png");

    // 平时角色大小（和 MainWindow.xaml 里 Pet 的 Width/Height 保持一致）
    private const double NormalWidth = 184;
    private const double NormalHeight = 168;

    // 贴边半隐藏时角色大小（peek.png 是头的特写，同样像素会显得更大）
    private const double PeekWidth = 120;
    private const double PeekHeight = 170;

    private PetMood _mood = PetMood.Happy;
    private bool _peek;
    private Storyboard? _breath;

    public PetCharacter()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        ApplySprite();
    }

    public void SetMood(PetMood mood)
    {
        _mood = mood;
        ApplySprite();
    }

    public void SetPeek(bool peek)
    {
        _peek = peek;
        ApplySprite();
    }

    public void SetFlip(bool horizontal, bool vertical)
    {
        if (FaceScale is null)
        {
            return;
        }

        FaceScale.ScaleX = horizontal ? -1 : 1;
        FaceScale.ScaleY = vertical ? -1 : 1;
    }

    public void SetLookAt(double? screenX, double? screenY)
    {
    }

    private void ApplySprite()
    {
        if (Sprite is null)
        {
            return;
        }

        Sprite.Source = _peek ? PeekSprite : SpriteFor(_mood);
        Width = _peek ? PeekWidth : NormalWidth;
        Height = _peek ? PeekHeight : NormalHeight;
    }

    private static ImageSource SpriteFor(PetMood mood) =>
        mood is PetMood.Worry or PetMood.Sad ? LowSprite : NormalSprite;

    private static ImageSource Load(string fileName)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri($"pack://application:,,,/Assets/Character/{fileName}", UriKind.Absolute);
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _breath = (Storyboard)FindResource("BreathStoryboard");
        _breath.Begin();
        ApplySprite();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => _breath?.Stop();
}
