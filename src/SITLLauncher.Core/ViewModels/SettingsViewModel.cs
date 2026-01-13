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
    private readonly VersionInstaller _versionInstaller;
    private readonly Action<string>? _rescanVersions;

    [ObservableProperty]
    private ObservableCollection<AirportViewModel> _airports = [];

    [ObservableProperty]
    private AirportViewModel? _selectedAirport;

    [ObservableProperty]
    private ObservableCollection<SerialPortViewModel> _serialPorts = [];

    [ObservableProperty]
    private SerialPortViewModel? _selectedSerialPort;

    [ObservableProperty]
    private ObservableCollection<VersionViewModel> _versions = [];

    [ObservableProperty]
    private VersionViewModel? _selectedVersion;

    [ObservableProperty]
    private string _dataPath = "";

    [ObservableProperty]
    private bool _isInstalling;

    [ObservableProperty]
    private double _installProgress;

    [ObservableProperty]
    private string _installStatus = "";

    public SettingsViewModel() : this(null!, null!, null)
    {
        // Design-time data
        Airports =
        [
            new AirportViewModel { Name = "Riverstone", Location = "-33.6671869,150.8543972,27.8,0" },
            new AirportViewModel { Name = "Test Airport", Location = "-34.0,151.0,10,90" }
        ];
        SerialPorts =
        [
            new SerialPortViewModel { Argument = "--serial0 tcp:0" },
            new SerialPortViewModel { Argument = "--serial1 udpclient:127.0.0.1:14550" }
        ];
        Versions =
        [
            new VersionViewModel { Name = "CxPilot-8.0.0-dev-abc123", Path = "C:/Data/Versions/CxPilot-8.0.0" },
            new VersionViewModel { Name = "CxPilot-7.5.0-stable", Path = "C:/Data/Versions/CxPilot-7.5.0" }
        ];
        DataPath = "C:/Data/SITLLauncher";
    }

    public SettingsViewModel(ConfigService configService, VersionInstaller versionInstaller, Action<string>? rescanVersions)
    {
        _configService = configService;
        _versionInstaller = versionInstaller;
        _rescanVersions = rescanVersions;

        if (configService is null) return;

        DataPath = configService.DataPath;
        LoadAirports();
        LoadSerialPorts();
        LoadVersions();
    }

    private void LoadAirports()
    {
        Airports = new ObservableCollection<AirportViewModel>(
            _configService.Airports.Select(a => new AirportViewModel
            {
                Name = a.Name,
                Location = a.Location
            }));
    }

    private void LoadSerialPorts()
    {
        SerialPorts = new ObservableCollection<SerialPortViewModel>(
            _configService.SerialPorts.Select(s => new SerialPortViewModel
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
            .Select(path => new VersionViewModel
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
        var newAirport = new AirportViewModel { Name = "New Airport", Location = "0,0,0,0" };
        Airports.Add(newAirport);
        SelectedAirport = newAirport;
    }

    [RelayCommand(CanExecute = nameof(CanRemoveAirport))]
    private void RemoveAirport()
    {
        if (SelectedAirport is not null)
        {
            Airports.Remove(SelectedAirport);
            SelectedAirport = Airports.FirstOrDefault();
        }
    }

    private bool CanRemoveAirport() => SelectedAirport is not null;

    [RelayCommand]
    private void AddSerialPort()
    {
        var newPort = new SerialPortViewModel { Argument = "--serial0 tcp:0" };
        SerialPorts.Add(newPort);
        SelectedSerialPort = newPort;
    }

    [RelayCommand(CanExecute = nameof(CanRemoveSerialPort))]
    private void RemoveSerialPort()
    {
        if (SelectedSerialPort is not null)
        {
            SerialPorts.Remove(SelectedSerialPort);
            SelectedSerialPort = SerialPorts.FirstOrDefault();
        }
    }

    private bool CanRemoveSerialPort() => SelectedSerialPort is not null;

    partial void OnSelectedAirportChanged(AirportViewModel? value)
    {
        RemoveAirportCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedSerialPortChanged(SerialPortViewModel? value)
    {
        RemoveSerialPortCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedVersionChanged(VersionViewModel? value)
    {
        DeleteVersionCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanDeleteVersion))]
    private void DeleteVersion()
    {
        if (SelectedVersion is null) return;

        try
        {
            Directory.Delete(SelectedVersion.Path, recursive: true);
            Versions.Remove(SelectedVersion);
            SelectedVersion = Versions.FirstOrDefault();
            _rescanVersions?.Invoke(_configService.DataPath);
            _configService.NotifyExternalChange();
        }
        catch (Exception)
        {
            // TODO: Show error dialog
        }
    }

    private bool CanDeleteVersion() => SelectedVersion is not null;

    [RelayCommand]
    private void ClearRuntimes()
    {
        var runtimePath = Path.Combine(_configService.DataPath, "Runtime");
        if (Directory.Exists(runtimePath))
        {
            try
            {
                Directory.Delete(runtimePath, recursive: true);
                _configService.NotifyExternalChange();
            }
            catch (Exception)
            {
                // TODO: Show error dialog
            }
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task InstallVersion(string archivePath)
    {
        if (string.IsNullOrWhiteSpace(archivePath))
            return;

        IsInstalling = true;
        InstallProgress = 0;
        InstallStatus = "Extracting archive...";

        try
        {
            var progress = new Progress<double>(p =>
            {
                InstallProgress = p * 100;
            });

            var installedPath = await System.Threading.Tasks.Task.Run(() =>
                _versionInstaller.Install(archivePath, progress));

            InstallStatus = "Installation complete!";
            LoadVersions();
            _rescanVersions?.Invoke(_configService.DataPath);
            _configService.NotifyExternalChange();

            // Reset after a brief delay
            await System.Threading.Tasks.Task.Delay(1500);
            IsInstalling = false;
            InstallProgress = 0;
            InstallStatus = "";
        }
        catch (Exception ex)
        {
            InstallStatus = $"Installation failed: {ex.Message}";
            await System.Threading.Tasks.Task.Delay(3000);
            IsInstalling = false;
            InstallProgress = 0;
            InstallStatus = "";
        }
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

        // Save data path if changed
        if (DataPath != _configService.DataPath)
        {
            _configService.UpdateDataPath(DataPath);
            _rescanVersions?.Invoke(DataPath);
        }
    }
}

public partial class AirportViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _location = "";
}

public partial class SerialPortViewModel : ObservableObject
{
    [ObservableProperty]
    private string _argument = "";
}

public partial class VersionViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _path = "";
}
