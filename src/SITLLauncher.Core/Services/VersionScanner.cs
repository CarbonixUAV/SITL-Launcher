using System.Collections.Generic;
using System.IO;
using SITLLauncher.Core.Models;

namespace SITLLauncher.Core.Services;

/// <summary>
/// Scans the Versions folder to discover available SITL versions and aircraft.
/// </summary>
public class VersionScanner(string versionsPath)
{
    private readonly string _versionsPath = versionsPath;

    /// <summary>
    /// Scans for all available versions and their aircraft configurations.
    /// </summary>
    public IReadOnlyList<SitlVersion> Scan()
    {
        if (!Directory.Exists(_versionsPath))
            return [];

        var versions = new List<SitlVersion>();

        foreach (var versionDir in Directory.GetDirectories(_versionsPath))
        {
            var aircraft = ScanAircraft(versionDir);
            if (aircraft.Count <= 0)
                continue;

            versions.Add(new SitlVersion
            {
                Name = Path.GetFileName(versionDir),
                Path = versionDir,
                Aircraft = aircraft
            });
        }

        return versions;
    }

    private static List<Aircraft> ScanAircraft(string versionDir)
    {
        var aircraft = new List<Aircraft>();

        foreach (var aircraftDir in Directory.GetDirectories(versionDir))
        {
            var launchBatPath = Path.Combine(aircraftDir, "launch.bat");
            if (!File.Exists(launchBatPath))
                continue;

            var content = File.ReadAllText(launchBatPath);
            var frameConfig = LaunchBatParser.Parse(launchBatPath, content);
            if (frameConfig is null)
                continue;

            aircraft.Add(new Aircraft
            {
                Name = Path.GetFileName(aircraftDir),
                Path = aircraftDir,
                FrameConfig = frameConfig
            });
        }

        return aircraft;
    }
}
