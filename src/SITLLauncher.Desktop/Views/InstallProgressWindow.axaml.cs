using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SITLLauncher.Core.Services;

namespace SITLLauncher.Desktop.Views;

public partial class InstallProgressWindow : Window
{
    private CancellationTokenSource? _cts;
    private bool _completed;

    public InstallProgressWindow()
    {
        InitializeComponent();
    }

    public static async Task<bool> ShowAndInstallAsync(string archivePath, string versionsPath, Window owner)
    {
        var installer = new VersionInstaller(versionsPath);
        var progressWindow = new InstallProgressWindow();
        progressWindow.Show(owner);
        return await progressWindow.InstallAsync(archivePath, installer);
    }

    private async Task<bool> InstallAsync(string archivePath, VersionInstaller installer)
    {
        var versionName = Path.GetFileNameWithoutExtension(archivePath);
        VersionNameText.Text = $"Installing {versionName}...";

        _cts = new CancellationTokenSource();
        var progress = new Progress<double>(p =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                ProgressBar.Value = p * 100;
                if (p < 0.8)
                    StatusText.Text = "Extracting archive...";
                else if (p < 1.0)
                    StatusText.Text = "Validating installation...";
            });
        });

        try
        {
            await Task.Run(() => installer.Install(archivePath, progress), _cts.Token);

            _completed = true;
            StatusText.Text = "Installation complete!";
            ProgressBar.Value = 100;

            await Task.Delay(800);
            Close(true);
            return true;
        }
        catch (OperationCanceledException)
        {
            Close(false);
            return false;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Failed: {ex.Message}";
            CancelButton.Content = "Close";
            return false;
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        if (_completed)
        {
            Close(true);
            return;
        }

        _cts?.Cancel();
        Close(false);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        _cts?.Cancel();
        base.OnClosing(e);
    }
}
