using System;
using System.IO;
using SITLLauncher.Core.Models;
using SITLLauncher.Core.Services;
using Xunit;

namespace SITLLauncher.Core.Tests;

public class ConfigServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;

    public ConfigServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
        _configPath = Path.Combine(_tempDir, "config.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Constructor_FileDoesNotExist_LoadsDefaultConfig()
    {
        var service = new ConfigService(_configPath);

        Assert.Single(service.Airports);
        Assert.Equal("Riverstone", service.Airports[0].Name);
        Assert.Empty(service.SerialPorts);
        Assert.Null(service.LastSelectedVersion);
        Assert.Null(service.LastSelectedAircraft);
    }

    [Fact]
    public void RecordLaunch_UpdatesSelections()
    {
        var service = new ConfigService(_configPath);

        service.RecordLaunch("v1.0.0", "ottano-headless", "Riverstone");

        Assert.Equal("v1.0.0", service.LastSelectedVersion);
        Assert.Equal("ottano-headless", service.LastSelectedAircraft);
        Assert.Equal("Riverstone", service.GetLastAirportForAircraft("ottano-headless"));
        Assert.Equal("v1.0.0", service.GetLastVersionForAircraft("ottano-headless"));
    }

    [Fact]
    public void RecordLaunch_PersistsToDisk()
    {
        var service1 = new ConfigService(_configPath);
        service1.RecordLaunch("v2.0.0", "volanti-realflight", "Sydney");

        // Create new instance to verify persistence
        var service2 = new ConfigService(_configPath);

        Assert.Equal("v2.0.0", service2.LastSelectedVersion);
        Assert.Equal("volanti-realflight", service2.LastSelectedAircraft);
        Assert.Equal("Sydney", service2.GetLastAirportForAircraft("volanti-realflight"));
        Assert.Equal("v2.0.0", service2.GetLastVersionForAircraft("volanti-realflight"));
    }

    [Fact]
    public void RecordLaunch_TracksMultipleAircraft()
    {
        var service = new ConfigService(_configPath);

        service.RecordLaunch("v1.0.0", "ottano-headless", "Riverstone");
        service.RecordLaunch("v2.0.0", "volanti-realflight", "Sydney");

        Assert.Equal("Riverstone", service.GetLastAirportForAircraft("ottano-headless"));
        Assert.Equal("v1.0.0", service.GetLastVersionForAircraft("ottano-headless"));
        Assert.Equal("Sydney", service.GetLastAirportForAircraft("volanti-realflight"));
        Assert.Equal("v2.0.0", service.GetLastVersionForAircraft("volanti-realflight"));
        // Last launch wins for global selection
        Assert.Equal("v2.0.0", service.LastSelectedVersion);
        Assert.Equal("volanti-realflight", service.LastSelectedAircraft);
    }

    [Fact]
    public void GetLastAirportForAircraft_UnknownAircraft_ReturnsNull()
    {
        var service = new ConfigService(_configPath);

        Assert.Null(service.GetLastAirportForAircraft("unknown-aircraft"));
    }

    [Fact]
    public void GetLastVersionForAircraft_UnknownAircraft_ReturnsNull()
    {
        var service = new ConfigService(_configPath);

        Assert.Null(service.GetLastVersionForAircraft("unknown-aircraft"));
    }
}
