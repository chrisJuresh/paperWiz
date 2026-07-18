using System.Windows;
using System.Windows.Threading;
using PaperWiz.ViewModels;

namespace PaperWiz;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnUnhandledException;

        if (e.Args.Any(arg => string.Equals(
                arg,
                "--restore-wallpaper",
                StringComparison.OrdinalIgnoreCase)))
        {
            // The per-user startup entry uses this headless mode to repair the desktop after
            // sign-in without leaving an app window or tray process running.
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _ = RestoreWallpaperAndExitAsync();
            return;
        }

        MainWindow = new MainWindow();
        MainWindow.Show();
    }

    private async Task RestoreWallpaperAndExitAsync()
    {
        try
        {
            var viewModel = new MainViewModel();
            await viewModel.ApplyNowAsync();
        }
        catch
        {
            // Startup restoration is best effort and intentionally silent.
        }
        finally
        {
            Shutdown();
        }
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            e.Exception.Message,
            "paperWiz — unexpected error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }
}
