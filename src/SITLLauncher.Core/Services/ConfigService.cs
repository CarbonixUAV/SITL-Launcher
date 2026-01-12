using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using SITLLauncher.Core.Models;

namespace SITLLauncher.Core.Services;

/// <summary>
/// Owns launcher configuration state and persists to config.json.
/// </summary>
public class ConfigService
{
    private readonly string _configPath;
    private LauncherConfig _config;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ConfigService(string configPath)
    {
        _configPath = configPath;
        _config = Load();
    }

    /// <summary>
    /// Path where Versions and Runtime folders are stored.
    /// </summary>
    public string DataPath => _config.DataPath;

    /// <summary>
    /// Configured airports.
    /// </summary>
    public IReadOnlyList<Airport> Airports => _config.Airports;

    /// <summary>
    /// Configured serial port arguments.
    /// </summary>
    public IReadOnlyList<SerialPortConfig> SerialPorts => _config.SerialPorts;

    /// <summary>
    /// Last selected version name.
    /// </summary>
    public string? LastSelectedVersion => _config.LastSelectedVersion;

    /// <summary>
    /// Last selected aircraft name.
    /// </summary>
    public string? LastSelectedAircraft => _config.LastSelectedAircraft;

    /// <summary>
    /// Gets the last-used airport for a given aircraft.
    /// </summary>
    public string? GetLastAirportForAircraft(string aircraftName)
    {
        return _config.LastAirportPerAircraft.TryGetValue(aircraftName, out var airport)
            ? airport
            : null;
    }

    /// <summary>
    /// Gets the last-used version for a given aircraft.
    /// </summary>
    public string? GetLastVersionForAircraft(string aircraftName)
    {
        return _config.LastVersionPerAircraft.TryGetValue(aircraftName, out var version)
            ? version
            : null;
    }

    /// <summary>
    /// Records a launch, updating all last-used selections and persisting.
    /// </summary>
    public void RecordLaunch(string versionName, string aircraftName, string airportName)
    {
        _config = _config with
        {
            LastSelectedVersion = versionName,
            LastSelectedAircraft = aircraftName,
            LastAirportPerAircraft = new Dictionary<string, string>(_config.LastAirportPerAircraft)
            {
                [aircraftName] = airportName
            },
            LastVersionPerAircraft = new Dictionary<string, string>(_config.LastVersionPerAircraft)
            {
                [aircraftName] = versionName
            }
        };
        Save();
    }

    private LauncherConfig Load()
    {
        if (!File.Exists(_configPath))
            return new LauncherConfig();

        var json = File.ReadAllText(_configPath);
        return JsonSerializer.Deserialize<LauncherConfig>(json, JsonOptions) ?? new LauncherConfig();
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_config, JsonOptions);
        File.WriteAllText(_configPath, json);
    }
}
