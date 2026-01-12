using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SITLLauncher.Core.Models;

namespace SITLLauncher.Core.Services;

/// <summary>
/// Orchestrates SITL launching and process lifecycle.
/// All events are guaranteed to fire on the UI thread (via dispatcher).
/// </summary>
public class LauncherService : IDisposable
{
    private readonly IUiDispatcher _dispatcher;
    private readonly ConfigService _configService;
    private readonly SitlProcessManager _processManager;

    public LauncherService(ConfigService configService, IUiDispatcher dispatcher)
    {
        _configService = configService;
        _dispatcher = dispatcher;
        _processManager = new SitlProcessManager();

        _processManager.Exited += OnProcessExited;
    }

    /// <summary>
    /// Available SITL versions.
    /// </summary>
    public IReadOnlyList<SitlVersion> Versions { get; private set; } = [];

    /// <summary>
    /// Configured airports.
    /// </summary>
    public IReadOnlyList<Airport> Airports => _configService.Airports;

    /// <summary>
    /// Configured serial port arguments.
    /// </summary>
    public IReadOnlyList<SerialPortConfig> SerialPorts => _configService.SerialPorts;

    /// <summary>
    /// Whether a SITL process is currently running.
    /// </summary>
    public bool IsRunning => _processManager.IsRunning;

    /// <summary>
    /// Last selected version name (persisted).
    /// </summary>
    public string? LastSelectedVersion => _configService.LastSelectedVersion;

    /// <summary>
    /// Last selected aircraft name (persisted).
    /// </summary>
    public string? LastSelectedAircraft => _configService.LastSelectedAircraft;

    /// <summary>
    /// Raised when the process state changes (started, exited).
    /// Guaranteed to fire on UI thread.
    /// </summary>
    public event EventHandler<ProcessStateEventArgs>? StateChanged;

    /// <summary>
    /// Scans for available versions in the data path.
    /// </summary>
    public void ScanVersions()
    {
        var versionsPath = Path.Combine(_configService.DataPath, "Versions");
        var scanner = new VersionScanner(versionsPath);
        Versions = scanner.Scan();
    }

    /// <summary>
    /// Gets the last-used airport for a given aircraft.
    /// </summary>
    public string? GetLastAirportForAircraft(string aircraftName)
    {
        return _configService.GetLastAirportForAircraft(aircraftName);
    }

    /// <summary>
    /// Launches SITL with the specified configuration.
    /// </summary>
    public void Launch(SitlVersion version, Aircraft aircraft, Airport airport)
    {
        if (IsRunning)
            throw new InvalidOperationException("A process is already running.");

        // Sync runtime if version changed for this aircraft
        var runtimeRoot = Path.Combine(_configService.DataPath, "Runtime");
        var runtimeAircraftPath = Path.Combine(runtimeRoot, aircraft.Name);

        var lastVersion = _configService.GetLastVersionForAircraft(aircraft.Name);
        if (lastVersion != version.Name)
        {
            RuntimeSyncService.SyncToRuntime(aircraft.Path, runtimeAircraftPath);
        }
        else
        {
            RuntimeSyncService.EnsureRuntimeDirectory(runtimeRoot, aircraft.Name);
        }

        // Record selections (this also persists)
        _configService.RecordLaunch(version.Name, aircraft.Name, airport.Name);

        // Build launch params
        var launchParams = new SitlLaunchParams
        {
            ExecutablePath = aircraft.FrameConfig.ExecutablePath,
            WorkingDirectory = runtimeAircraftPath,
            ModelArg = aircraft.FrameConfig.ModelArg,
            DefaultsFile = "defaults.parm",
            Location = airport.Location,
            SerialPortArgs = _configService.SerialPorts.Select(s => s.Argument).ToList()
        };

        _processManager.Launch(launchParams);

        _dispatcher.Post(() => StateChanged?.Invoke(this, new ProcessStateEventArgs(
            true, $"Running: {aircraft.Name}")));
    }

    /// <summary>
    /// Kills the running SITL process.
    /// </summary>
    public void Kill()
    {
        _processManager.Kill();
    }

    private void OnProcessExited(object? sender, int exitCode)
    {
        _dispatcher.Post(() => StateChanged?.Invoke(this, new ProcessStateEventArgs(
            false, $"Exited (code {exitCode})")));
    }

    public void Dispose()
    {
        _processManager.Exited -= OnProcessExited;
        _processManager.Dispose();
    }
}

/// <summary>
/// Event args for process state changes.
/// </summary>
public record ProcessStateEventArgs(bool IsRunning, string StatusText);
