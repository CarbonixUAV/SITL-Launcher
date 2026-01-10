using System;
using System.IO;
using System.Linq;
using SITLLauncher.Core.Services;
using Xunit;

namespace SITLLauncher.Core.Tests;

public class VersionScannerTests
{
    [Fact]
    public void Scan_NonexistentPath_ReturnsEmptyList()
    {
        var scanner = new VersionScanner(@"C:\nonexistent\path");
        var result = scanner.Scan();
        Assert.Empty(result);
    }

    [Fact]
    public void Scan_EmptyDirectory_ReturnsEmptyList()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var scanner = new VersionScanner(tempDir);
            var result = scanner.Scan();
            Assert.Empty(result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Scan_VersionWithAircraft_ReturnsCorrectStructure()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var versionDir = Path.Combine(tempDir, "CxPilot-1.0.0");
        var aircraftDir = Path.Combine(versionDir, "test-headless");
        Directory.CreateDirectory(aircraftDir);

        var launchBat = @"..\CxPilot.exe -M quadplane:test.json --defaults=defaults.parm";
        File.WriteAllText(Path.Combine(aircraftDir, "launch.bat"), launchBat);

        try
        {
            var scanner = new VersionScanner(tempDir);
            var result = scanner.Scan();

            Assert.Single(result);
            Assert.Equal("CxPilot-1.0.0", result[0].Name);
            Assert.Single(result[0].Aircraft);
            Assert.Equal("test-headless", result[0].Aircraft[0].Name);
            Assert.Equal("quadplane:test.json", result[0].Aircraft[0].FrameConfig.ModelArg);
            Assert.Equal("defaults.parm", result[0].Aircraft[0].FrameConfig.DefaultsFile);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Scan_VersionWithoutLaunchBat_SkipsAircraft()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var versionDir = Path.Combine(tempDir, "CxPilot-1.0.0");
        var aircraftDir = Path.Combine(versionDir, "no-launch-bat");
        Directory.CreateDirectory(aircraftDir);

        try
        {
            var scanner = new VersionScanner(tempDir);
            var result = scanner.Scan();

            // Version exists but has no valid aircraft, so it's not included
            Assert.Empty(result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Scan_MultipleVersionsAndAircraft_ReturnsAll()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Version 1 with 2 aircraft
        var v1Dir = Path.Combine(tempDir, "CxPilot-1.0.0");
        var v1a1 = Path.Combine(v1Dir, "ottano-headless");
        var v1a2 = Path.Combine(v1Dir, "ottano-realflight");
        Directory.CreateDirectory(v1a1);
        Directory.CreateDirectory(v1a2);
        File.WriteAllText(Path.Combine(v1a1, "launch.bat"), @"..\CxPilot.exe -M quadplane:ottano.json --defaults=defaults.parm");
        File.WriteAllText(Path.Combine(v1a2, "launch.bat"), @"..\CxPilot.exe -M flightaxis --defaults=defaults.parm");

        // Version 2 with 1 aircraft
        var v2Dir = Path.Combine(tempDir, "CxPilot-2.0.0");
        var v2a1 = Path.Combine(v2Dir, "volanti-headless");
        Directory.CreateDirectory(v2a1);
        File.WriteAllText(Path.Combine(v2a1, "launch.bat"), @"..\CxPilot.exe -M quadplane:volanti.json --defaults=defaults.parm");

        try
        {
            var scanner = new VersionScanner(tempDir);
            var result = scanner.Scan();

            Assert.Equal(2, result.Count);

            var v1 = result.First(v => v.Name == "CxPilot-1.0.0");
            Assert.Equal(2, v1.Aircraft.Count);

            var v2 = result.First(v => v.Name == "CxPilot-2.0.0");
            Assert.Single(v2.Aircraft);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
