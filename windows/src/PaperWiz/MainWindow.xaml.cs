using System.IO;
using System.Windows;
using PaperWiz.ViewModels;

namespace PaperWiz;

public partial class MainWindow : Window
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff", ".webp",
    };

    public MainWindow()
    {
        InitializeComponent();
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    private void OnPreviewResized(object sender, SizeChangedEventArgs e) =>
        ViewModel?.LayoutMonitors(e.NewSize.Width, e.NewSize.Height);

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = TryGetImagePath(e, out _) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnFileDrop(object sender, DragEventArgs e)
    {
        if (TryGetImagePath(e, out string path))
            ViewModel?.SetWallpaper(path);
    }

    private static bool TryGetImagePath(DragEventArgs e, out string path)
    {
        path = string.Empty;
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return false;
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
            return false;

        string candidate = files[0];
        if (!File.Exists(candidate) || !ImageExtensions.Contains(Path.GetExtension(candidate)))
            return false;

        path = candidate;
        return true;
    }
}
