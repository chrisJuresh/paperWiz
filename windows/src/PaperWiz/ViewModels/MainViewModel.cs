using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using PaperWiz.Models;
using PaperWiz.Services;
using Drawing = System.Drawing;

namespace PaperWiz.ViewModels;

/// <summary>A suggested accent colour with a label explaining why it was picked.</summary>
public sealed class AccentOption : ViewModelBase
{
    public AccentOption(Color color, string label, string reason)
    {
        Color = color;
        Brush = new SolidColorBrush(color);
        Brush.Freeze();
        Hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        Label = label;
        Reason = reason;
    }

    public Color Color { get; }
    public SolidColorBrush Brush { get; }
    public string Hex { get; }
    public string Label { get; }
    public string Reason { get; }

    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
}

/// <summary>A thumbnail for one image in the currently-browsed folder.</summary>
public sealed class WallpaperThumb : ViewModelBase
{
    public WallpaperThumb(string path, ImageSource thumbnail)
    {
        Path = path;
        FileName = System.IO.Path.GetFileName(path);
        Thumbnail = thumbnail;
    }

    public string Path { get; }
    public string FileName { get; }
    public ImageSource Thumbnail { get; }

    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
}

public sealed class MainViewModel : ViewModelBase
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff", ".webp",
    };

    private readonly WallpaperService _service = new();

    private IReadOnlyList<MonitorInfo> _monitorInfos = Array.Empty<MonitorInfo>();
    private BitmapSource? _sourceWallpaperThumbnail;
    private int _sourcePixelWidth;
    private int _sourcePixelHeight;
    private int _wallPixelWidth;
    private int _wallPixelHeight;
    private bool _updatingAccent;
    private bool _pickerOpen;
    private int _folderToken;
    private readonly DispatcherTimer _applyTimer;
    private bool _restoringSettings = true;

    /// <summary>
    /// The label of the palette entry the accent follows (e.g. "Most common", "Darkest",
    /// "color3"). Persists across photos so the same *kind* of colour is re-derived from each
    /// new wallpaper. Null means the user set a fixed custom colour that shouldn't refresh.
    /// </summary>
    private string? _selectedAccentKey = "Most common";

    private double _lastAreaWidth = 480;
    private double _lastAreaHeight = 300;

    public MainViewModel()
    {
        BrowseCommand = new RelayCommand(Browse);
        OpenFolderCommand = new RelayCommand(OpenFolder);
        RotateLeftCommand = new RelayCommand(() => RotationDegrees -= 90);
        RotateRightCommand = new RelayCommand(() => RotationDegrees += 90);
        RefreshMonitorsCommand = new RelayCommand(LoadMonitors);
        SelectMonitorCommand = new RelayCommand(p =>
        {
            if (p is MonitorViewModel vm)
                WallpaperMonitorIndex = vm.Index;
        });
        SelectSwatchCommand = new RelayCommand(p =>
        {
            if (p is AccentOption s)
            {
                // Remember which palette entry (by label) so it refreshes for the next photo.
                _selectedAccentKey = s.Label;
                SetAccent(s.Color);
            }
        });
        SelectFolderImageCommand = new RelayCommand(p =>
        {
            if (p is WallpaperThumb t)
                SetWallpaper(t.Path);
        });

        // Every change auto-applies; the timer coalesces rapid tweaks (slider drags, typing).
        _applyTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _applyTimer.Tick += (_, _) => { _applyTimer.Stop(); _ = RunApplyAsync(); };

        FolderImages.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasFolderImages));

        LoadMonitors();
        SetAccent(Color.FromRgb(32, 34, 40));

        try
        {
            RestoreSettings();
        }
        finally
        {
            _restoringSettings = false;
        }

        // Rebuild and apply the saved composites when the app is reopened. This also repairs
        // the desktop after a Windows restart if the shell did not retain its cached mapping.
        ScheduleApply();
    }

    // --- Commands ---
    public RelayCommand BrowseCommand { get; }
    public RelayCommand OpenFolderCommand { get; }
    public RelayCommand RotateLeftCommand { get; }
    public RelayCommand RotateRightCommand { get; }
    public RelayCommand RefreshMonitorsCommand { get; }
    public RelayCommand SelectMonitorCommand { get; }
    public RelayCommand SelectSwatchCommand { get; }
    public RelayCommand SelectFolderImageCommand { get; }

    // --- Collections ---
    public ObservableCollection<MonitorViewModel> Monitors { get; } = new();

    /// <summary>pywal-style scheme, shown above the wallpaper suggestions.</summary>
    public ObservableCollection<AccentOption> PywalPalette { get; } = new();

    /// <summary>The original "why this colour" suggestions (most common, vibrant, …).</summary>
    public ObservableCollection<AccentOption> Palette { get; } = new();

    /// <summary>Thumbnails of the currently-browsed folder.</summary>
    public ObservableCollection<WallpaperThumb> FolderImages { get; } = new();

    public bool HasFolderImages => FolderImages.Count > 0;

    private string? _currentFolder;
    public string? CurrentFolder
    {
        get => _currentFolder;
        private set
        {
            if (SetProperty(ref _currentFolder, value))
                OnPropertyChanged(nameof(CurrentFolderName));
        }
    }

    public string CurrentFolderName =>
        string.IsNullOrEmpty(CurrentFolder) ? string.Empty : new DirectoryInfo(CurrentFolder!).Name;

    // --- Wallpaper ---
    private string? _wallpaperPath;
    public string? WallpaperPath
    {
        get => _wallpaperPath;
        private set
        {
            if (SetProperty(ref _wallpaperPath, value))
            {
                OnPropertyChanged(nameof(HasWallpaper));
                OnPropertyChanged(nameof(WallpaperFileName));
            }
        }
    }

    public bool HasWallpaper => !string.IsNullOrEmpty(WallpaperPath);
    public string WallpaperFileName => HasWallpaper ? Path.GetFileName(WallpaperPath!) : string.Empty;
    public string WallpaperDimensions => _wallPixelWidth > 0 ? $"{_wallPixelWidth} × {_wallPixelHeight}" : string.Empty;

    private ImageSource? _wallpaperThumbnail;
    public ImageSource? WallpaperThumbnail
    {
        get => _wallpaperThumbnail;
        private set => SetProperty(ref _wallpaperThumbnail, value);
    }

    // --- Accent colour (single source of truth) ---
    private Color _accentColor = Colors.Black;
    public Color AccentColor
    {
        get => _accentColor;
        private set => SetAccent(value);
    }

    private SolidColorBrush _accentBrush = Brushes.Black;
    public SolidColorBrush AccentBrush
    {
        get => _accentBrush;
        private set => SetProperty(ref _accentBrush, value);
    }

    public byte AccentR
    {
        get => _accentColor.R;
        set => SetAccent(Color.FromRgb(value, _accentColor.G, _accentColor.B), fromUser: true);
    }
    public byte AccentG
    {
        get => _accentColor.G;
        set => SetAccent(Color.FromRgb(_accentColor.R, value, _accentColor.B), fromUser: true);
    }
    public byte AccentB
    {
        get => _accentColor.B;
        set => SetAccent(Color.FromRgb(_accentColor.R, _accentColor.G, value), fromUser: true);
    }

    private string _accentHex = "#000000";
    public string AccentHex
    {
        get => _accentHex;
        set
        {
            if (_accentHex == value)
                return;
            _accentHex = value;
            OnPropertyChanged();
            if (TryParseHex(value, out Color c))
                SetAccent(c, fromUser: true);
        }
    }

    // --- Placement ---
    private int _wallpaperMonitorIndex;
    public int WallpaperMonitorIndex
    {
        get => _wallpaperMonitorIndex;
        set
        {
            if (SetProperty(ref _wallpaperMonitorIndex, value))
            {
                UpdateShrinkDefault();
                UpdatePreview();
            }
        }
    }

    private Anchor _anchor = Anchor.Center;
    public Anchor Anchor
    {
        get => _anchor;
        set { if (SetProperty(ref _anchor, value)) UpdatePreview(); }
    }

    private FitMode _fitMode = FitMode.Auto;
    public FitMode FitMode
    {
        get => _fitMode;
        set { if (SetProperty(ref _fitMode, value)) UpdatePreview(); }
    }

    private int _rotationDegrees;
    public int RotationDegrees
    {
        get => _rotationDegrees;
        set
        {
            int normalized = ((value % 360) + 360) % 360;
            if (normalized is not (0 or 90 or 180 or 270))
                normalized = 0;
            if (SetProperty(ref _rotationDegrees, normalized))
            {
                RefreshRotatedWallpaper();
                UpdateShrinkDefault();
                UpdatePreview();
            }
        }
    }

    private bool _shrink;
    public bool Shrink
    {
        get => _shrink;
        set { if (SetProperty(ref _shrink, value)) UpdatePreview(); }
    }

    private int _shrinkHeight = 1080;
    public int ShrinkHeight
    {
        get => _shrinkHeight;
        set
        {
            _shrinkTouched = true;
            if (SetProperty(ref _shrinkHeight, Math.Clamp(value, 120, 16000)) && Shrink)
                UpdatePreview();
        }
    }

    private bool _fillOtherMonitors = true;
    public bool FillOtherMonitors
    {
        get => _fillOtherMonitors;
        set { if (SetProperty(ref _fillOtherMonitors, value)) UpdatePreview(); }
    }

    private bool _goldenMargin;
    public bool GoldenMargin
    {
        get => _goldenMargin;
        set { if (SetProperty(ref _goldenMargin, value)) UpdatePreview(); }
    }

    // --- Status ---
    private string _status = "Ready.";
    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    // --- Monitor enumeration ---
    public void LoadMonitors()
    {
        string? selectedDeviceId = _monitorInfos
            .FirstOrDefault(m => m.Index == _wallpaperMonitorIndex)?.DeviceId;

        try
        {
            _monitorInfos = MonitorService.GetMonitors();
        }
        catch (Exception ex)
        {
            _monitorInfos = Array.Empty<MonitorInfo>();
            Status = $"Could not read monitors: {ex.Message}";
        }

        Monitors.Clear();
        foreach (var info in _monitorInfos)
            Monitors.Add(new MonitorViewModel(info));

        if (_wallpaperMonitorIndex >= Monitors.Count)
            _wallpaperMonitorIndex = 0;

        int primary = _monitorInfos.FirstOrDefault(m => m.IsPrimary)?.Index ?? 0;
        int previous = selectedDeviceId is null
            ? -1
            : _monitorInfos.ToList().FindIndex(m => m.DeviceId == selectedDeviceId);
        WallpaperMonitorIndex = Monitors.Count > 0 && previous >= 0 ? previous : primary;

        UpdateShrinkDefault();
        LayoutMonitors(_lastAreaWidth, _lastAreaHeight);
        UpdatePreview();

        if (Monitors.Count > 0 && !Status.StartsWith("Could not"))
            Status = $"{Monitors.Count} display{(Monitors.Count == 1 ? "" : "s")} detected.";
    }

    // --- Wallpaper selection ---
    private void Browse()
    {
        // Guard against the dialog being opened twice (WPF can re-deliver the click that
        // opened it once the modal dialog closes).
        if (_pickerOpen)
            return;
        _pickerOpen = true;
        try
        {
            var dialog = new OpenFileDialog
            {
                Title = "Choose a wallpaper",
                Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tif;*.tiff;*.webp|All files|*.*",
            };
            if (dialog.ShowDialog() == true)
                SetWallpaper(dialog.FileName);
        }
        finally
        {
            _pickerOpen = false;
        }
    }

    private void OpenFolder()
    {
        if (_pickerOpen)
            return;
        _pickerOpen = true;
        try
        {
            var dialog = new OpenFolderDialog { Title = "Choose a wallpaper folder" };
            if (dialog.ShowDialog() == true)
                _ = LoadFolderAsync(dialog.FolderName);
        }
        finally
        {
            _pickerOpen = false;
        }
    }

    private async Task LoadFolderAsync(string folder)
    {
        int token = ++_folderToken;
        CurrentFolder = folder;
        FolderImages.Clear();

        List<string> files;
        try
        {
            files = Directory.EnumerateFiles(folder)
                .Where(f => ImageExtensions.Contains(Path.GetExtension(f)))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .Take(400)
                .ToList();
        }
        catch (Exception ex)
        {
            Status = $"Could not read folder: {ex.Message}";
            return;
        }

        if (files.Count == 0)
        {
            Status = "No images in that folder.";
            return;
        }

        Status = $"Loading {files.Count} image{(files.Count == 1 ? "" : "s")} from {CurrentFolderName}…";
        foreach (var file in files)
        {
            if (token != _folderToken)
                return; // a newer folder was opened

            ImageSource? thumb = null;
            try { thumb = await Task.Run(() => LoadFrozenBitmap(file, 240)); }
            catch { continue; }

            if (token != _folderToken)
                return;

            var item = new WallpaperThumb(file, thumb) { IsSelected = file == WallpaperPath };
            FolderImages.Add(item);
        }

        Status = $"{FolderImages.Count} image{(FolderImages.Count == 1 ? "" : "s")} in {CurrentFolderName}.";
    }

    public void SetWallpaper(string path)
    {
        if (!File.Exists(path))
        {
            Status = "That file no longer exists.";
            return;
        }

        try
        {
            var thumb = LoadFrozenBitmap(path, decodePixelWidth: 900);
            _sourcePixelWidth = thumb.PixelWidth;
            _sourcePixelHeight = thumb.PixelHeight;

            // Full-resolution dimensions for accurate cover/frame decisions.
            var (fullW, fullH) = ReadPixelSize(path);
            if (fullW > 0)
            {
                _sourcePixelWidth = fullW;
                _sourcePixelHeight = fullH;
            }

            bool isDifferentWallpaper = !string.Equals(path, WallpaperPath, StringComparison.OrdinalIgnoreCase);
            _sourceWallpaperThumbnail = thumb;
            WallpaperPath = path;
            if (isDifferentWallpaper && _rotationDegrees != 0)
            {
                _rotationDegrees = 0;
                OnPropertyChanged(nameof(RotationDegrees));
            }
            RefreshRotatedWallpaper();

            ExtractPalette(path);
            RefreshFolderSelection();
            UpdateShrinkDefault();
            UpdatePreview();
            OnPropertyChanged(nameof(WallpaperDimensions));
            Status = $"Loaded {Path.GetFileName(path)}.";
        }
        catch (Exception ex)
        {
            Status = $"Could not open image: {ex.Message}";
        }
    }

    private void RefreshRotatedWallpaper()
    {
        if (_sourceWallpaperThumbnail is null)
            return;

        if (_rotationDegrees == 0)
        {
            WallpaperThumbnail = _sourceWallpaperThumbnail;
        }
        else
        {
            var rotated = new TransformedBitmap(
                _sourceWallpaperThumbnail,
                new RotateTransform(_rotationDegrees));
            rotated.Freeze();
            WallpaperThumbnail = rotated;
        }

        bool swapsAxes = _rotationDegrees is 90 or 270;
        _wallPixelWidth = swapsAxes ? _sourcePixelHeight : _sourcePixelWidth;
        _wallPixelHeight = swapsAxes ? _sourcePixelWidth : _sourcePixelHeight;
        OnPropertyChanged(nameof(WallpaperDimensions));
    }

    private void ExtractPalette(string path)
    {
        Palette.Clear();
        PywalPalette.Clear();
        try
        {
            foreach (var c in PywalGenerator.Generate(path))
                PywalPalette.Add(new AccentOption(Color.FromRgb(c.Color.R, c.Color.G, c.Color.B), c.Label, c.Reason));

            foreach (var c in ColorExtractor.Analyze(path))
                Palette.Add(new AccentOption(Color.FromRgb(c.Color.R, c.Color.G, c.Color.B), c.Label, c.Reason));
        }
        catch
        {
            // Non-fatal: user can still pick a colour manually.
        }

        ApplySelectedAccent();
    }

    /// <summary>
    /// Re-derive the accent from the freshly-analysed palette using the remembered selection
    /// (e.g. "Darkest" → this photo's darkest). Falls back to "Most common" if the remembered
    /// entry isn't available this time. A custom colour (null key) is left untouched.
    /// </summary>
    private void ApplySelectedAccent()
    {
        if (_selectedAccentKey is null)
        {
            RefreshSelection();
            return;
        }

        var match = PywalPalette.Concat(Palette).FirstOrDefault(o => o.Label == _selectedAccentKey)
                    ?? Palette.FirstOrDefault();
        if (match is not null)
            SetAccent(match.Color);
        else
            RefreshSelection();
    }

    private void RefreshSelection()
    {
        foreach (var option in Palette)
            option.IsSelected = IsAccentSelected(option);
        foreach (var option in PywalPalette)
            option.IsSelected = IsAccentSelected(option);
    }

    private bool IsAccentSelected(AccentOption option) =>
        _selectedAccentKey is not null ? option.Label == _selectedAccentKey : option.Color == _accentColor;

    private void RefreshFolderSelection()
    {
        foreach (var thumb in FolderImages)
            thumb.IsSelected = thumb.Path == WallpaperPath;
    }

    // --- Accent handling ---
    private void SetAccent(Color color, bool fromUser = false)
    {
        if (_updatingAccent)
            return;
        _updatingAccent = true;
        try
        {
            // A hand-picked colour (hex box / RGB sliders) is a fixed override that
            // shouldn't be re-derived from the next photo.
            if (fromUser)
                _selectedAccentKey = null;

            _accentColor = color;
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            AccentBrush = brush;
            _accentHex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

            OnPropertyChanged(nameof(AccentColor));
            OnPropertyChanged(nameof(AccentR));
            OnPropertyChanged(nameof(AccentG));
            OnPropertyChanged(nameof(AccentB));
            OnPropertyChanged(nameof(AccentHex));
        }
        finally
        {
            _updatingAccent = false;
        }
        RefreshSelection();
        UpdatePreview();
    }

    // --- Preview ---
    public void LayoutMonitors(double areaWidth, double areaHeight)
    {
        _lastAreaWidth = areaWidth;
        _lastAreaHeight = areaHeight;
        if (Monitors.Count == 0 || areaWidth <= 0 || areaHeight <= 0)
            return;

        int minX = _monitorInfos.Min(m => m.Bounds.Left);
        int minY = _monitorInfos.Min(m => m.Bounds.Top);
        int maxX = _monitorInfos.Max(m => m.Bounds.Right);
        int maxY = _monitorInfos.Max(m => m.Bounds.Bottom);

        double totalW = Math.Max(1, maxX - minX);
        double totalH = Math.Max(1, maxY - minY);

        const double pad = 12;
        double scale = Math.Min((areaWidth - pad * 2) / totalW, (areaHeight - pad * 2) / totalH);
        if (scale <= 0 || double.IsInfinity(scale))
            return;

        double offsetX = pad + (areaWidth - pad * 2 - totalW * scale) / 2;
        double offsetY = pad + (areaHeight - pad * 2 - totalH * scale) / 2;

        const double gap = 3;
        foreach (var vm in Monitors)
        {
            var b = vm.Info.Bounds;
            vm.CanvasX = offsetX + (b.Left - minX) * scale;
            vm.CanvasY = offsetY + (b.Top - minY) * scale;
            vm.CanvasWidth = Math.Max(1, b.Width * scale - gap);
            vm.CanvasHeight = Math.Max(1, b.Height * scale - gap);
        }
    }

    private void UpdatePreview()
    {
        foreach (var vm in Monitors)
        {
            bool isWall = vm.Index == WallpaperMonitorIndex;
            vm.IsWallpaperMonitor = isWall;
            vm.AccentBrush = AccentBrush;

            if (isWall && HasWallpaper)
            {
                vm.WallpaperImage = WallpaperThumbnail;

                int monW = vm.Info.Width;
                int monH = vm.Info.Height;
                (int effW, int effH) = EffectiveWallpaperSize();
                bool covers = effW >= monW && effH >= monH;
                bool useCover = FitMode switch
                {
                    FitMode.Cover => true,
                    FitMode.Frame => false,
                    _ => covers,
                };

                // A golden border always frames (whole image visible) and shrinks it by 1/φ.
                if (GoldenMargin)
                    useCover = false;

                vm.PreviewStretch = useCover ? Stretch.UniformToFill : Stretch.Uniform;
                (vm.PreviewHAlign, vm.PreviewVAlign) = useCover
                    ? (HorizontalAlignment.Center, VerticalAlignment.Center)
                    : AnchorToAlignment(Anchor);
                vm.PreviewScale = GoldenMargin ? WallpaperComposer.GoldenFraction : 1.0;
            }
            else
            {
                vm.WallpaperImage = null;
            }
        }

        // Every preview-affecting change is also a wallpaper-affecting change → auto-apply.
        ScheduleApply();
    }

    private (int Width, int Height) EffectiveWallpaperSize()
    {
        int w = _wallPixelWidth;
        int h = _wallPixelHeight;
        if (Shrink && h > ShrinkHeight && h > 0)
        {
            double scale = (double)ShrinkHeight / h;
            w = Math.Max(1, (int)Math.Round(w * scale));
            h = ShrinkHeight;
        }
        return (w, h);
    }

    private static (HorizontalAlignment, VerticalAlignment) AnchorToAlignment(Anchor anchor) => anchor switch
    {
        Anchor.TopLeft => (HorizontalAlignment.Left, VerticalAlignment.Top),
        Anchor.Top => (HorizontalAlignment.Center, VerticalAlignment.Top),
        Anchor.TopRight => (HorizontalAlignment.Right, VerticalAlignment.Top),
        Anchor.Left => (HorizontalAlignment.Left, VerticalAlignment.Center),
        Anchor.Right => (HorizontalAlignment.Right, VerticalAlignment.Center),
        Anchor.BottomLeft => (HorizontalAlignment.Left, VerticalAlignment.Bottom),
        Anchor.Bottom => (HorizontalAlignment.Center, VerticalAlignment.Bottom),
        Anchor.BottomRight => (HorizontalAlignment.Right, VerticalAlignment.Bottom),
        _ => (HorizontalAlignment.Center, VerticalAlignment.Center),
    };

    private void UpdateShrinkDefault()
    {
        // Recommended by the original README: main monitor height / 9 * 7.
        var wallMon = _monitorInfos.FirstOrDefault(m => m.Index == WallpaperMonitorIndex);
        if (wallMon is not null && !_shrinkTouched)
            _shrinkHeight = Math.Clamp(wallMon.Height * 7 / 9, 120, 16000);
        OnPropertyChanged(nameof(ShrinkHeight));
    }

    private bool _shrinkTouched;

    // --- Auto-apply (debounced) ---
    private bool _isApplying;
    private bool _applyPending;

    /// <summary>Queue an apply. The timer coalesces bursts of changes into a single apply.</summary>
    private void ScheduleApply()
    {
        if (!HasWallpaper || Monitors.Count == 0)
            return;
        if (_restoringSettings)
            return;

        SaveSettings();
        _applyTimer.Stop();
        _applyTimer.Start();
    }

    private async Task RunApplyAsync()
    {
        if (!HasWallpaper || Monitors.Count == 0)
            return;
        if (_isApplying)
        {
            _applyPending = true; // re-apply once the current pass finishes
            return;
        }

        _isApplying = true;
        IsBusy = true;
        try
        {
            do
            {
                _applyPending = false;
                Status = "Applying…";
                var options = BuildOptions();
                var infos = _monitorInfos;
                await Task.Run(() => _service.Apply(infos, options));
                SaveSettings();
                Status = $"Applied to {infos.Count} display{(infos.Count == 1 ? "" : "s")}.";
            }
            while (_applyPending); // pick up any change that arrived mid-apply
        }
        catch (Exception ex)
        {
            Status = $"Couldn't apply: {ex.Message}";
        }
        finally
        {
            _isApplying = false;
            IsBusy = false;
        }
    }

    /// <summary>Apply immediately, used by the silent sign-in restoration path.</summary>
    public Task ApplyNowAsync()
    {
        _applyTimer.Stop();
        return RunApplyAsync();
    }

    private PaperWizOptions BuildOptions() => new()
    {
        WallpaperPath = WallpaperPath!,
        WallpaperMonitorIndex = WallpaperMonitorIndex,
        AccentColor = Drawing.Color.FromArgb(255, _accentColor.R, _accentColor.G, _accentColor.B),
        Anchor = Anchor,
        FitMode = FitMode,
        RotationDegrees = RotationDegrees,
        ShrinkHeight = Shrink ? ShrinkHeight : null,
        GoldenMargin = GoldenMargin,
        FillOtherMonitors = FillOtherMonitors,
    };

    /// <summary>Persist the current choices so closing the app or rebooting loses nothing.</summary>
    public void SaveSettings()
    {
        if (_restoringSettings || !HasWallpaper)
            return;

        var selectedMonitor = _monitorInfos.FirstOrDefault(m => m.Index == WallpaperMonitorIndex);
        var settings = new PaperWizSettings
        {
            WallpaperPath = WallpaperPath,
            WallpaperMonitorDeviceId = selectedMonitor?.DeviceId,
            WallpaperMonitorIndex = WallpaperMonitorIndex,
            AccentHex = $"#{_accentColor.R:X2}{_accentColor.G:X2}{_accentColor.B:X2}",
            SelectedAccentKey = _selectedAccentKey,
            Anchor = Anchor,
            FitMode = FitMode,
            RotationDegrees = RotationDegrees,
            Shrink = Shrink,
            ShrinkHeight = ShrinkHeight,
            FillOtherMonitors = FillOtherMonitors,
            GoldenMargin = GoldenMargin,
        };

        try { SettingsService.Save(settings); }
        catch { /* Wallpaper application still works if settings cannot be written. */ }
    }

    private void RestoreSettings()
    {
        var settings = SettingsService.Load();
        if (settings is null || string.IsNullOrWhiteSpace(settings.WallpaperPath))
            return;

        Anchor = Enum.IsDefined(settings.Anchor) ? settings.Anchor : Anchor.Center;
        FitMode = Enum.IsDefined(settings.FitMode) ? settings.FitMode : FitMode.Auto;
        ShrinkHeight = Math.Clamp(settings.ShrinkHeight, 120, 16000);
        Shrink = settings.Shrink;
        FillOtherMonitors = settings.FillOtherMonitors;
        GoldenMargin = settings.GoldenMargin;

        int monitorIndex = settings.WallpaperMonitorDeviceId is null
            ? -1
            : _monitorInfos.ToList().FindIndex(m => m.DeviceId == settings.WallpaperMonitorDeviceId);
        if (monitorIndex < 0 && settings.WallpaperMonitorIndex >= 0 &&
            settings.WallpaperMonitorIndex < _monitorInfos.Count)
        {
            monitorIndex = settings.WallpaperMonitorIndex;
        }
        if (monitorIndex >= 0)
            WallpaperMonitorIndex = monitorIndex;

        _selectedAccentKey = settings.SelectedAccentKey;
        if (_selectedAccentKey is null && TryParseHex(settings.AccentHex, out Color customAccent))
            SetAccent(customAccent);

        if (File.Exists(settings.WallpaperPath))
        {
            SetWallpaper(settings.WallpaperPath);
            RotationDegrees = settings.RotationDegrees;
        }
        else
            Status = "The saved wallpaper file could not be found.";
    }

    // --- Helpers ---
    private static BitmapImage LoadFrozenBitmap(string path, int decodePixelWidth)
    {
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
        bmp.DecodePixelWidth = decodePixelWidth;
        bmp.UriSource = new Uri(path);
        bmp.EndInit();
        bmp.Freeze();
        return bmp;
    }

    private static (int Width, int Height) ReadPixelSize(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            var frame = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
            return (frame.PixelWidth, frame.PixelHeight);
        }
        catch
        {
            return (0, 0);
        }
    }

    private static bool TryParseHex(string text, out Color color)
    {
        color = Colors.Black;
        if (string.IsNullOrWhiteSpace(text))
            return false;
        string hex = text.Trim().TrimStart('#');
        if (hex.Length == 3)
            hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);
        if (hex.Length != 6)
            return false;
        if (!byte.TryParse(hex.AsSpan(0, 2), System.Globalization.NumberStyles.HexNumber, null, out byte r) ||
            !byte.TryParse(hex.AsSpan(2, 2), System.Globalization.NumberStyles.HexNumber, null, out byte g) ||
            !byte.TryParse(hex.AsSpan(4, 2), System.Globalization.NumberStyles.HexNumber, null, out byte b))
            return false;
        color = Color.FromRgb(r, g, b);
        return true;
    }
}
