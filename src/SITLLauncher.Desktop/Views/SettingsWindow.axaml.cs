using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SITLLauncher.Core.ViewModels;

namespace SITLLauncher.Desktop.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        // Only allow Copy operation for .7z files
#pragma warning disable CS0618 // Type or member is obsolete
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles()?.ToList();
#pragma warning restore CS0618 // Type or member is obsolete
            if (files?.Any(f => f.Path.LocalPath.EndsWith(".7z", System.StringComparison.OrdinalIgnoreCase)) == true)
            {
                e.DragEffects = DragDropEffects.Copy;
                return;
            }
        }
        e.DragEffects = DragDropEffects.None;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles()?.ToList();
#pragma warning restore CS0618 // Type or member is obsolete
            var archiveFile = files?.FirstOrDefault(f =>
                f.Path.LocalPath.EndsWith(".7z", System.StringComparison.OrdinalIgnoreCase));

            if (archiveFile != null && DataContext is SettingsViewModel vm)
            {
                await vm.InstallVersionCommand.ExecuteAsync(archiveFile.Path.LocalPath);
            }
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
