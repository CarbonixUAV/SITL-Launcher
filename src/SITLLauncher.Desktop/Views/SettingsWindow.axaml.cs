using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SITLLauncher.Core;
using SITLLauncher.Core.ViewModels;

namespace SITLLauncher.Desktop.Views;

public partial class SettingsWindow : Window
{
    public event EventHandler? VersionInstalled;

    public SettingsWindow()
    {
        InitializeComponent();
    }

    private async void OnBrowseArchiveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm) return;

        var options = new FilePickerOpenOptions
        {
            Title = "Select SITL Version Archive",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("7z Archives") { Patterns = ["*.7z"] }
            ]
        };

        var result = await StorageProvider.OpenFilePickerAsync(options);
        if (result.Count == 0) return;

        var versionsPath = Path.Combine(AppInfo.DataDirectory, "Versions");
        var success = await InstallProgressWindow.ShowAndInstallAsync(result[0].Path.LocalPath, versionsPath, this);

        if (success)
        {
            vm.RefreshVersions();
            VersionInstalled?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            vm.SaveCommand.Execute(null);
        }
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
