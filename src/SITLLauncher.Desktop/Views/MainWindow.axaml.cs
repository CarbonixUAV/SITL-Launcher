using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SITLLauncher.Core.ViewModels;

namespace SITLLauncher.Desktop.Views;

public partial class MainWindow : Window
{
    private const double ScrollBottomThreshold = 50;
    private bool _autoScroll = true;

    public MainWindow()
    {
        InitializeComponent();
        SetupAutoScroll();
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
}
