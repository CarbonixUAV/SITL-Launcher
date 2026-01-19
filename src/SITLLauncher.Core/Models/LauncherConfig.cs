using System.Collections.Generic;

namespace SITLLauncher.Core.Models;

/// <summary>
/// Root configuration stored in config.json.
/// </summary>
public record LauncherConfig
{
    private static string DefaultDataPath => AppInfo.DataDirectory;

    private static List<Airport> DefaultAirports =>
    [
        new Airport { Name = "Riverstone", Location = "-33.6671869,150.8543972,27.8,0" }
    ];

    public static List<SerialPortConfig> DefaultSerialPorts =>
    [
        new SerialPortConfig { Argument = "--serial0 udpclient:127.0.0.1:14550" },
        new SerialPortConfig { Argument = "--serial1 tcp:5762" },
        new SerialPortConfig { Argument = "--serial2 tcp:5763" }
    ];

    /// <summary>
    /// Path where Versions and Runtime folders are stored.
    /// Defaults to %LOCALAPPDATA%\SITLLauncher.
    /// </summary>
    public string DataPath { get; init; } = DefaultDataPath;

    /// <summary>
    /// Available airports/locations.
    /// </summary>
    public List<Airport> Airports { get; init; } = DefaultAirports;

    /// <summary>
    /// Serial port arguments to pass to SITL.
    /// </summary>
    public List<SerialPortConfig> SerialPorts { get; init; } = DefaultSerialPorts;

    /// <summary>
    /// Last-used airport name per aircraft (key = aircraft name).
    /// </summary>
    public Dictionary<string, string> LastAirportPerAircraft { get; init; } = [];

    /// <summary>
    /// Last-used version name per aircraft (key = aircraft name).
    /// Used to detect version changes for runtime sync.
    /// </summary>
    public Dictionary<string, string> LastVersionPerAircraft { get; init; } = [];

    /// <summary>
    /// Last selected version name.
    /// </summary>
    public string? LastSelectedVersion { get; init; }

    /// <summary>
    /// Last selected aircraft name.
    /// </summary>
    public string? LastSelectedAircraft { get; init; }
}
