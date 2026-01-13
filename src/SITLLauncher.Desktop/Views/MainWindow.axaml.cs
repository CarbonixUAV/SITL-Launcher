using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SITLLauncher.Core.ViewModels;

namespace SITLLauncher.Desktop.Views;

public partial class MainWindow : Window
{
    public static readonly BoolToDoubleConverter BoolToDouble = new();

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
