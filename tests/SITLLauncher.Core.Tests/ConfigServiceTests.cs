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
    public void Load_FileDoesNotExist_ReturnsDefaultConfig()
    {
        var service = new ConfigService(_configPath);

        var config = service.Load();

        Assert.NotNull(config);
        Assert.Single(config.Airports);
        Assert.Equal("Riverstone", config.Airports[0].Name);
        Assert.Empty(config.SerialPorts);
    }

    [Fact]
    public void SaveAndLoad_RoundTrips()
    {
        var service = new ConfigService(_configPath);
        var config = new LauncherConfig
        {
            Airports =
            [
                new Airport { Name = "Test Airport", Location = "-33.8,151.2,10,90" }
            ],
            SerialPorts =
            [
                new SerialPortConfig { Argument = "-A tcp:0" }
            ],
            LastAirportPerAircraft = { ["ottano-headless"] = "Test Airport" },
            LastVersionPerAircraft = { ["ottano-headless"] = "v1.0.0" },
            LastSelectedVersion = "v1.0.0",
            LastSelectedAircraft = "ottano-headless"
        };

        service.Save(config);
        var loaded = service.Load();

        Assert.Single(loaded.Airports);
        Assert.Equal("Test Airport", loaded.Airports[0].Name);
        Assert.Equal("-33.8,151.2,10,90", loaded.Airports[0].Location);
        Assert.Single(loaded.SerialPorts);
        Assert.Equal("-A tcp:0", loaded.SerialPorts[0].Argument);
        Assert.Equal("Test Airport", loaded.LastAirportPerAircraft["ottano-headless"]);
        Assert.Equal("v1.0.0", loaded.LastVersionPerAircraft["ottano-headless"]);
        Assert.Equal("v1.0.0", loaded.LastSelectedVersion);
        Assert.Equal("ottano-headless", loaded.LastSelectedAircraft);
    }

    [Fact]
    public void Save_CreatesFormattedJson()
    {
        var service = new ConfigService(_configPath);
        var config = new LauncherConfig
        {
            Airports = [new Airport { Name = "HQ", Location = "1,2,3,4" }]
        };

        service.Save(config);
        var json = File.ReadAllText(_configPath);

        Assert.Contains("\"airports\"", json);
        Assert.Contains("\"name\"", json);
        Assert.Contains("\n", json); // Indented
    }
}
