using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SITLLauncher.Core.Models;
using SITLLauncher.Core.Services;

namespace SITLLauncher.Core.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ConfigService _configService;
    private readonly Action? _rescanVersions;

    [ObservableProperty]
    private ObservableCollection<AirportViewModel> _airports = [];

    [ObservableProperty]
    private ObservableCollection<SerialPortViewModel> _serialPorts = [];

    [ObservableProperty]
    private ObservableCollection<VersionViewModel> _versions = [];

    public SettingsViewModel() : this(null!, null)
    {
        // Design-time data
        Airports =
        [
            new AirportViewModel(DeleteAirport) { Name = "Riverstone", Location = "-33.6671869,150.8543972,27.8,0" },
            new AirportViewModel(DeleteAirport) { Name = "Test Airport", Location = "-34.0,151.0,10,90" }
        ];
        SerialPorts =
        [
            new SerialPortViewModel(DeleteSerialPort) { Argument = "--serial0 tcp:0" },
            new SerialPortViewModel(DeleteSerialPort) { Argument = "--serial1 udpclient:127.0.0.1:14550" }
        ];
        Versions =
        [
            new VersionViewModel { Name = "CxPilot-8.0.0-dev-abc123", Path = "C:/Data/Versions/CxPilot-8.0.0" },
            new VersionViewModel { Name = "CxPilot-7.5.0-stable", Path = "C:/Data/Versions/CxPilot-7.5.0" }
        ];
    }

    public SettingsViewModel(ConfigService configService, Action? rescanVersions)
    {
        _configService = configService;
        _rescanVersions = rescanVersions;

        if (configService is null) return;

        LoadAirports();
        LoadSerialPorts();
        LoadVersions();
    }

    public void RefreshVersions()
    {
        LoadVersions();
    }

    private void LoadAirports()
    {
        Airports = new ObservableCollection<AirportViewModel>(
            _configService.Airports.Select(a => new AirportViewModel(DeleteAirport)
            {
                Name = a.Name,
                Location = a.Location
            }));
    }

    private void LoadSerialPorts()
    {
        SerialPorts = new ObservableCollection<SerialPortViewModel>(
            _configService.SerialPorts.Select(s => new SerialPortViewModel(DeleteSerialPort)
            {
                Argument = s.Argument
            }));
    }

    private void LoadVersions()
    {
        var versionsPath = Path.Combine(_configService.DataPath, "Versions");
        if (!Directory.Exists(versionsPath))
        {
            Versions = [];
            return;
        }

        var versionDirs = Directory.GetDirectories(versionsPath)
            .Select(path => new VersionViewModel(DeleteVersion)
            {
                Name = Path.GetFileName(path),
                Path = path
            })
            .OrderByDescending(v => v.Name)
            .ToList();

        Versions = new ObservableCollection<VersionViewModel>(versionDirs);
    }

    [RelayCommand]
    private void AddAirport()
    {
        var newAirport = new AirportViewModel(DeleteAirport) { Name = "New Airport", Location = "0,0,0,0" };
        Airports.Add(newAirport);
    }

    private void DeleteAirport(AirportViewModel? airport)
    {
        if (airport is null) return;

        Airports.Remove(airport);
    }

    [RelayCommand]
    private void AddSerialPort()
    {
        var newPort = new SerialPortViewModel(DeleteSerialPort) { Argument = "--serial0 tcp:0" };
        SerialPorts.Add(newPort);
    }

    [RelayCommand]
    private void ResetSerialPortsToDefaults()
    {
        SerialPorts = new ObservableCollection<SerialPortViewModel>(
            LauncherConfig.DefaultSerialPorts.Select(s => new SerialPortViewModel(DeleteSerialPort)
            {
                Argument = s.Argument
            }));
    }

    private void DeleteSerialPort(SerialPortViewModel? serialPort)
    {
        if (serialPort is null) return;

        SerialPorts.Remove(serialPort);
    }

    private void DeleteVersion(VersionViewModel? version)
    {
        if (version is null) return;

        Directory.Delete(version.Path, recursive: true);
        Versions.Remove(version);
        _rescanVersions?.Invoke();
        _configService.NotifyExternalChange();
    }

    [RelayCommand]
    private void ClearRuntimes()
    {
        var runtimePath = Path.Combine(_configService.DataPath, "Runtime");
        if (Directory.Exists(runtimePath))
        {
            Directory.Delete(runtimePath, recursive: true);
            _configService.NotifyExternalChange();
        }
    }

    [RelayCommand]
    private void ResetAll()
    {
        // Delete all versions
        var versionsPath = Path.Combine(_configService.DataPath, "Versions");
        if (Directory.Exists(versionsPath))
        {
            Directory.Delete(versionsPath, recursive: true);
        }

        // Delete runtime data
        var runtimePath = Path.Combine(_configService.DataPath, "Runtime");
        if (Directory.Exists(runtimePath))
        {
            Directory.Delete(runtimePath, recursive: true);
        }

        // Reset config to defaults
        _configService.ResetToDefaults();

        // Reload UI state
        LoadAirports();
        LoadSerialPorts();
        LoadVersions();
        _rescanVersions?.Invoke();
    }

    [RelayCommand]
    private void Save()
    {
        // Save airports
        var airports = Airports
            .Where(a => !string.IsNullOrWhiteSpace(a.Name))
            .Select(a => new Airport { Name = a.Name, Location = a.Location });
        _configService.UpdateAirports(airports);

        // Save serial ports
        var serialPorts = SerialPorts
            .Where(s => !string.IsNullOrWhiteSpace(s.Argument))
            .Select(s => new SerialPortConfig { Argument = s.Argument });
        _configService.UpdateSerialPorts(serialPorts);
    }
}

public partial class AirportViewModel : ObservableObject
{
    public AirportViewModel(Action<AirportViewModel> delete)
    {
        DeleteCommand = new RelayCommand(() => delete(this));
    }

    public AirportViewModel()
    {
        DeleteCommand = new RelayCommand(() => { });
    }

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _location = "";

    public IRelayCommand DeleteCommand { get; }
}

public partial class SerialPortViewModel : ObservableObject
{
    public SerialPortViewModel(Action<SerialPortViewModel> delete)
    {
        DeleteCommand = new RelayCommand(() => delete(this));
    }

    public SerialPortViewModel()
    {
        DeleteCommand = new RelayCommand(() => { });
    }

    [ObservableProperty]
    private string _argument = "";

    public IRelayCommand DeleteCommand { get; }
}

public partial class VersionViewModel : ObservableObject
{
    public VersionViewModel(Action<VersionViewModel> delete)
    {
        DeleteCommand = new RelayCommand(() => delete(this));
    }

    public VersionViewModel()
    {
        DeleteCommand = new RelayCommand(() => { });
    }

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _path = "";

    public IRelayCommand DeleteCommand { get; }
}
