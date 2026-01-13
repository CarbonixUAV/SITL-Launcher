using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using SITLLauncher.Core.Services;
using SITLLauncher.Core.ViewModels;
using SITLLauncher.Desktop.Services;
using SITLLauncher.Desktop.Views;

namespace SITLLauncher.Desktop;

public partial class App : Application
{
    private ConfigService? _configService;
    private LauncherService? _launcher;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        SetupExceptionHandling();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            _configService = new ConfigService(
                Path.Combine(AppContext.BaseDirectory, "config.json"));
            _launcher = new LauncherService(
                _configService,
                new AvaloniaUiDispatcher());

            var mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(_launcher),
            };
            mainWindow.SettingsRequested += OnSettingsRequested;

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async void OnSettingsRequested(object? sender, EventArgs e)
    {
        if (_configService is null || _launcher is null) return;
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop) return;

        var settingsVm = new SettingsViewModel(_configService, _ => _launcher.ScanVersions());
        var settingsWindow = new SettingsWindow { DataContext = settingsVm };

        await settingsWindow.ShowDialog<bool?>(desktop.MainWindow);
    }

    private void SetupExceptionHandling()
    {
        // UI thread exceptions - these can be handled and the app can continue
        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            e.Handled = true;
            ShowErrorDialog("An unexpected error occurred", e.Exception);
        };

        // Background task exceptions
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            e.SetObserved();
            Dispatcher.UIThread.Post(() => ShowErrorDialog("A background task failed", e.Exception));
        };

        // Last resort - app is terminating anyway, but try to show something
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var exception = e.ExceptionObject as Exception ?? new Exception("Unknown error");
            // Use Invoke (blocking) since Post won't execute before termination
            Dispatcher.UIThread.Invoke(() => ShowErrorDialog("A fatal error occurred", exception));
        };
    }

    private void ShowErrorDialog(string message, Exception exception)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop)
        {
            var dialog = ErrorDialog.Create(message, exception);
            dialog.ShowDialog(desktop.MainWindow);
        }
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
