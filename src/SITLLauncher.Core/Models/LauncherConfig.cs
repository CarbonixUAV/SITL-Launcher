using System;
using System.Collections.Generic;
using System.IO;

namespace SITLLauncher.Core.Models;

/// <summary>
/// Root configuration stored in config.json.
/// </summary>
public record LauncherConfig
{
    private static string DefaultDataPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SITLLauncher");

    private static List<Airport> DefaultAirports =>
    [
        new Airport { Name = "Riverstone", Location = "-33.6671869,150.8543972,27.8,0" }
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
    public List<SerialPortConfig> SerialPorts { get; init; } = [];

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
