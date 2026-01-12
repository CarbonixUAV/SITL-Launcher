using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SITLLauncher.Desktop.Views;

public partial class ErrorDialog : Window
{
    public ErrorDialog()
    {
        InitializeComponent();
    }

    public static ErrorDialog Create(string message, Exception exception)
    {
        var dialog = new ErrorDialog();
        dialog.MessageText.Text = message;
        dialog.DetailsText.Text = FormatException(exception);
        return dialog;
    }

    public static ErrorDialog Create(string message, string details)
    {
        var dialog = new ErrorDialog();
        dialog.MessageText.Text = message;
        dialog.DetailsText.Text = details;
        return dialog;
    }

    private static string FormatException(Exception ex)
    {
        return $"{ex.GetType().FullName}: {ex.Message}\n\n{ex.StackTrace}";
    }

    private async void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is not null)
        {
            var text = $"{MessageText.Text}\n\n{DetailsText.Text}";
            await clipboard.SetTextAsync(text);
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
