using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SITLLauncher.Core.Services;
using SITLLauncher.Core.ViewModels;

namespace SITLLauncher.Desktop.Views;

public partial class MainWindow : Window
{
    public static readonly BoolToDoubleConverter BoolToDouble = new();

    private const double ScrollBottomThreshold = 50;
    private bool _autoScroll = true;

    private ConfigService? _configService;
    private Action? _rescanVersions;

    public MainWindow()
    {
        InitializeComponent();
        SetupAutoScroll();
        SetupDragDrop();
    }

    public void Initialize(ConfigService configService, Action rescanVersions)
    {
        _configService = configService;
        _rescanVersions = rescanVersions;
    }

    private void SetupDragDrop()
    {
        AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private bool HasValidArchive(DragEventArgs e)
    {
#pragma warning disable CS0618 // DataFormats.Files is obsolete but still functional
        if (!e.Data.Contains(DataFormats.Files))
            return false;

        var files = e.Data.GetFiles()?.ToList();
#pragma warning restore CS0618
        return files?.Any(f => f.Path.LocalPath.EndsWith(".7z", StringComparison.OrdinalIgnoreCase)) == true;
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (HasValidArchive(e))
            DragOverlay.IsVisible = true;
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        DragOverlay.IsVisible = false;
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = HasValidArchive(e) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        DragOverlay.IsVisible = false;

#pragma warning disable CS0618
        if (!e.Data.Contains(DataFormats.Files))
            return;

        var files = e.Data.GetFiles()?.ToList();
#pragma warning restore CS0618
        var archiveFile = files?.FirstOrDefault(f =>
            f.Path.LocalPath.EndsWith(".7z", StringComparison.OrdinalIgnoreCase));

        if (archiveFile != null)
            _ = InstallVersionAsync(archiveFile.Path.LocalPath);
    }

    private async Task InstallVersionAsync(string archivePath)
    {
        if (_configService is null) return;

        var versionsPath = Path.Combine(_configService.DataPath, "Versions");
        var success = await InstallProgressWindow.ShowAndInstallAsync(archivePath, versionsPath, this);

        if (success)
        {
            _rescanVersions?.Invoke();
            _configService.NotifyExternalChange();
        }
    }

    private void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        _ = ShowSettingsAsync();
    }

    private void OnOpenLogsClick(object? sender, RoutedEventArgs e)
    {
        if (_configService is null || DataContext is not MainWindowViewModel vm)
            return;

        var logsPath = Path.Combine(_configService.DataPath, "Runtime");
        if (vm.SelectedAircraft is not null)
            logsPath = Path.Combine(logsPath, vm.SelectedAircraft.Name);
        logsPath = Path.Combine(logsPath, "logs");

        Directory.CreateDirectory(logsPath);
        Process.Start(new ProcessStartInfo
        {
            FileName = logsPath,
            UseShellExecute = true
        });
    }

    private async Task ShowSettingsAsync()
    {
        if (_configService is null || _rescanVersions is null) return;

        var settingsVm = new SettingsViewModel(_configService, _rescanVersions);
        var settingsWindow = new SettingsWindow { DataContext = settingsVm };
        settingsWindow.VersionInstalled += (_, _) =>
        {
            _rescanVersions();
            _configService.NotifyExternalChange();
        };

        await settingsWindow.ShowDialog<bool?>(this);
    }

    private void SetupAutoScroll()
    {
        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.OutputLines.CollectionChanged += (_, _) =>
                {
                    if (_autoScroll)
                        OutputScrollViewer.ScrollToEnd();
                };
            }
        };

        OutputScrollViewer.AddHandler(PointerPressedEvent, (_, e) =>
        {
            if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
                _autoScroll = false;
        }, RoutingStrategies.Tunnel, handledEventsToo: true);

        OutputScrollViewer.AddHandler(PointerReleasedEvent, (_, e) =>
        {
            if (e.InitialPressMouseButton == MouseButton.Left)
                _autoScroll = IsScrolledToBottom();
        }, RoutingStrategies.Tunnel, handledEventsToo: true);

        OutputScrollViewer.AddHandler(PointerWheelChangedEvent, (_, _) =>
            Dispatcher.UIThread.Post(() => _autoScroll = IsScrolledToBottom()),
            RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    private bool IsScrolledToBottom()
    {
        var distanceFromBottom = OutputScrollViewer.Extent.Height
            - OutputScrollViewer.Viewport.Height
            - OutputScrollViewer.Offset.Y;
        return distanceFromBottom < ScrollBottomThreshold;
    }

    private async void OnCopyAllClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && Clipboard is not null)
        {
            var text = string.Join(Environment.NewLine, vm.OutputLines);
            await Clipboard.SetTextAsync(text);
        }
    }

    /// <summary>
    /// Converts a boolean to one of two double values.
    /// ConverterParameter format: "trueValue,falseValue" (e.g., "450,250")
    /// </summary>
    public class BoolToDoubleConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (parameter is not string param || !param.Contains(','))
                return 0.0;

            var parts = param.Split(',');
            var trueValue = double.Parse(parts[0], CultureInfo.InvariantCulture);
            var falseValue = double.Parse(parts[1], CultureInfo.InvariantCulture);

            return value is true ? trueValue : falseValue;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
