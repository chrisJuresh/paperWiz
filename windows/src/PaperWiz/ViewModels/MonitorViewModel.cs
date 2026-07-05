using System.Windows;
using System.Windows.Media;
using PaperWiz.Models;

namespace PaperWiz.ViewModels;

/// <summary>
/// A single monitor as shown in the layout preview. Position/size are in preview-canvas
/// units; the visual state (accent fill, wallpaper stretch/anchor) mirrors exactly what
/// <see cref="Services.WallpaperComposer"/> will produce on apply.
/// </summary>
public sealed class MonitorViewModel : ViewModelBase
{
    public MonitorViewModel(MonitorInfo info)
    {
        Info = info;
    }

    public MonitorInfo Info { get; }

    public int Index => Info.Index;
    public bool IsPrimary => Info.IsPrimary;

    public string Label => $"Display {Info.Index + 1}";
    public string Details => Info.IsPrimary
        ? $"{Info.ResolutionText}  ·  Primary"
        : Info.ResolutionText;

    // --- Preview canvas geometry (set by MainViewModel.LayoutMonitors) ---

    private double _canvasX;
    public double CanvasX { get => _canvasX; set => SetProperty(ref _canvasX, value); }

    private double _canvasY;
    public double CanvasY { get => _canvasY; set => SetProperty(ref _canvasY, value); }

    private double _canvasWidth;
    public double CanvasWidth { get => _canvasWidth; set => SetProperty(ref _canvasWidth, value); }

    private double _canvasHeight;
    public double CanvasHeight { get => _canvasHeight; set => SetProperty(ref _canvasHeight, value); }

    // --- Visual state ---

    private bool _isWallpaperMonitor;
    public bool IsWallpaperMonitor
    {
        get => _isWallpaperMonitor;
        set
        {
            if (SetProperty(ref _isWallpaperMonitor, value))
                OnPropertyChanged(nameof(ShowWallpaper));
        }
    }

    private ImageSource? _wallpaperImage;
    public ImageSource? WallpaperImage
    {
        get => _wallpaperImage;
        set
        {
            if (SetProperty(ref _wallpaperImage, value))
                OnPropertyChanged(nameof(ShowWallpaper));
        }
    }

    public bool ShowWallpaper => IsWallpaperMonitor && WallpaperImage is not null;

    private Brush _accentBrush = Brushes.Transparent;
    public Brush AccentBrush { get => _accentBrush; set => SetProperty(ref _accentBrush, value); }

    private Stretch _previewStretch = Stretch.Uniform;
    public Stretch PreviewStretch { get => _previewStretch; set => SetProperty(ref _previewStretch, value); }

    private HorizontalAlignment _previewHAlign = HorizontalAlignment.Center;
    public HorizontalAlignment PreviewHAlign { get => _previewHAlign; set => SetProperty(ref _previewHAlign, value); }

    private VerticalAlignment _previewVAlign = VerticalAlignment.Center;
    public VerticalAlignment PreviewVAlign { get => _previewVAlign; set => SetProperty(ref _previewVAlign, value); }

    /// <summary>Extra scale applied to the previewed wallpaper (1.0 normally, 1/φ for a golden border).</summary>
    private double _previewScale = 1.0;
    public double PreviewScale { get => _previewScale; set => SetProperty(ref _previewScale, value); }
}
