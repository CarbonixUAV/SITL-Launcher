using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SITLLauncher.Core.Models;
using SITLLauncher.Core.Services;

namespace SITLLauncher.Core.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly LauncherService _launcher;

    [ObservableProperty]
    private ObservableCollection<SitlVersion> _versions = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LaunchCommand))]
    private SitlVersion? _selectedVersion;

    [ObservableProperty]
    private ObservableCollection<Aircraft> _aircraft = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LaunchCommand))]
    private Aircraft? _selectedAircraft;

    [ObservableProperty]
    private ObservableCollection<Airport> _airports = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LaunchCommand))]
    private Airport? _selectedAirport;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LaunchCommand))]
    [NotifyCanExecuteChangedFor(nameof(KillCommand))]
    private bool _isRunning;

    [ObservableProperty]
    private string _statusText = "Ready";

    public MainWindowViewModel(LauncherService launcher)
    {
        _launcher = launcher;
        _launcher.StateChanged += OnStateChanged;

        LoadData();
    }

    private void LoadData()
    {
        _launcher.ScanVersions();

        Versions = new ObservableCollection<SitlVersion>(_launcher.Versions);
        Airports = new ObservableCollection<Airport>(_launcher.Airports);

        // Restore last selected version
        if (_launcher.LastSelectedVersion is not null)
        {
            SelectedVersion = Versions.FirstOrDefault(v => v.Name == _launcher.LastSelectedVersion);
        }
        SelectedVersion ??= Versions.FirstOrDefault();
    }

    partial void OnSelectedVersionChanged(SitlVersion? value)
    {
        Aircraft = value is not null
            ? new ObservableCollection<Aircraft>(value.Aircraft)
            : [];

        // Restore last selected aircraft for this version, or pick first
        if (value is not null && _launcher.LastSelectedAircraft is not null)
        {
            SelectedAircraft = Aircraft.FirstOrDefault(a => a.Name == _launcher.LastSelectedAircraft);
        }
        SelectedAircraft ??= Aircraft.FirstOrDefault();
    }

    partial void OnSelectedAircraftChanged(Aircraft? value)
    {
        if (value is null) return;

        // Restore last-used airport for this aircraft
        var lastAirport = _launcher.GetLastAirportForAircraft(value.Name);
        if (lastAirport is not null)
        {
            SelectedAirport = Airports.FirstOrDefault(a => a.Name == lastAirport) ?? SelectedAirport;
        }

        SelectedAirport ??= Airports.FirstOrDefault();
    }

    [RelayCommand(CanExecute = nameof(CanLaunch))]
    private void Launch()
    {
        if (SelectedVersion is null || SelectedAircraft is null || SelectedAirport is null)
            return;

        _launcher.Launch(SelectedVersion, SelectedAircraft, SelectedAirport);
    }

    private bool CanLaunch() =>
        !IsRunning
        && SelectedVersion is not null
        && SelectedAircraft is not null
        && SelectedAirport is not null;

    [RelayCommand(CanExecute = nameof(CanKill))]
    private void Kill()
    {
        _launcher.Kill();
    }

    private bool CanKill() => IsRunning;

    private void OnStateChanged(object? sender, ProcessStateEventArgs e)
    {
        // Already on UI thread - guaranteed by LauncherService
        IsRunning = e.IsRunning;
        StatusText = e.StatusText;
    }
}
