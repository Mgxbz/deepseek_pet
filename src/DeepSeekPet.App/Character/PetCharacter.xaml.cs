using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using DeepSeekPet.Core.Character;
using DeepSeekPet.Core.Snap;
using UserControl = System.Windows.Controls.UserControl;

namespace DeepSeekPet.App.Character;

public partial class PetCharacter : UserControl, ICharacterView
{
    private static readonly ImageSource NormalSprite = Load("normal.png");
    private static readonly ImageSource LowSprite = Load("low.png");
    private static readonly ImageSource PeekSprite = Load("peek.png");
    private static readonly ImageSource PeekTbSprite = Load("peek-tb.png");

    // 平时角色大小（和 MainWindow.xaml 里 Pet 的 Width/Height 保持一致）
    private const double NormalWidth = 184;
    private const double NormalHeight = 168;

    // 左右收起：peek.png 是斜切头特写
    private const double PeekWidth = 120;
    private const double PeekHeight = 185;

    // 上下收起：peek-tb.png 是正立横裁
    private const double PeekTbWidth = 110;
    private const double PeekTbHeight = 178;

    private PetMood _mood = PetMood.Happy;
    private bool _peek;
    private DockEdge? _peekEdge;
    private Storyboard? _breath;
    private Storyboard? _poke;

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

    public void SetPeek(bool peek, DockEdge? edge = null)
    {
        _peek = peek;
        _peekEdge = peek ? edge : null;
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

    public void PlayPoke()
    {
        if (_poke is null)
        {
            return;
        }

        _poke.Stop();
        _poke.Begin();
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

        if (_peek)
        {
            var topOrBottom = _peekEdge is DockEdge.Top or DockEdge.Bottom;
            Sprite.Source = topOrBottom ? PeekTbSprite : PeekSprite;
            Width = topOrBottom ? PeekTbWidth : PeekWidth;
            Height = topOrBottom ? PeekTbHeight : PeekHeight;
            return;
        }

        Sprite.Source = SpriteFor(_mood);
        Width = NormalWidth;
        Height = NormalHeight;
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
        _poke = (Storyboard)FindResource("PokeStoryboard");
        _breath.Begin();
        ApplySprite();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _poke?.Stop();
        _breath?.Stop();
    }
}
