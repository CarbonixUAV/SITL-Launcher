using System;
using System.IO;
using SITLLauncher.Core.Services;
using Xunit;

namespace SITLLauncher.Core.Tests;

public class RuntimeSyncServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _versionPath;
    private readonly string _runtimePath;

    public RuntimeSyncServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _versionPath = Path.Combine(_tempDir, "Versions", "v1.0", "ottano-headless");
        _runtimePath = Path.Combine(_tempDir, "Runtime", "ottano-headless");
        Directory.CreateDirectory(_versionPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void EnsureRuntimeDirectory_CreatesDirectory()
    {
        var runtimeRoot = Path.Combine(_tempDir, "Runtime");

        RuntimeSyncService.EnsureRuntimeDirectory(runtimeRoot, "ottano-headless");

        Assert.True(Directory.Exists(Path.Combine(runtimeRoot, "ottano-headless")));
    }

    [Fact]
    public void SyncToRuntime_CopiesParmFiles()
    {
        File.WriteAllText(Path.Combine(_versionPath, "defaults.parm"), "PARAM1 100");
        File.WriteAllText(Path.Combine(_versionPath, "extra.parm"), "PARAM2 200");

        RuntimeSyncService.SyncToRuntime(_versionPath, _runtimePath);

        Assert.True(File.Exists(Path.Combine(_runtimePath, "defaults.parm")));
        Assert.True(File.Exists(Path.Combine(_runtimePath, "extra.parm")));
        Assert.Equal("PARAM1 100", File.ReadAllText(Path.Combine(_runtimePath, "defaults.parm")));
    }

    [Fact]
    public void SyncToRuntime_CopiesParamFiles()
    {
        File.WriteAllText(Path.Combine(_versionPath, "settings.param"), "SETTING1 50");

        RuntimeSyncService.SyncToRuntime(_versionPath, _runtimePath);

        Assert.True(File.Exists(Path.Combine(_runtimePath, "settings.param")));
        Assert.Equal("SETTING1 50", File.ReadAllText(Path.Combine(_runtimePath, "settings.param")));
    }

    [Fact]
    public void SyncToRuntime_CopiesJsonFiles()
    {
        File.WriteAllText(Path.Combine(_versionPath, "ottano.json"), "{\"weight\": 5}");
        File.WriteAllText(Path.Combine(_versionPath, "other.json"), "{\"test\": 1}");

        RuntimeSyncService.SyncToRuntime(_versionPath, _runtimePath);

        Assert.True(File.Exists(Path.Combine(_runtimePath, "ottano.json")));
        Assert.True(File.Exists(Path.Combine(_runtimePath, "other.json")));
    }

    [Fact]
    public void SyncToRuntime_CopiesScriptsFolder()
    {
        var scriptsDir = Path.Combine(_versionPath, "scripts");
        Directory.CreateDirectory(scriptsDir);
        File.WriteAllText(Path.Combine(scriptsDir, "test.lua"), "-- lua script");

        RuntimeSyncService.SyncToRuntime(_versionPath, _runtimePath);

        var destScript = Path.Combine(_runtimePath, "scripts", "test.lua");
        Assert.True(File.Exists(destScript));
        Assert.Equal("-- lua script", File.ReadAllText(destScript));
    }

    [Fact]
    public void SyncToRuntime_OverwritesExistingFiles()
    {
        // Setup existing runtime file with old content
        Directory.CreateDirectory(_runtimePath);
        File.WriteAllText(Path.Combine(_runtimePath, "defaults.parm"), "OLD CONTENT");

        // Version has new content
        File.WriteAllText(Path.Combine(_versionPath, "defaults.parm"), "NEW CONTENT");

        RuntimeSyncService.SyncToRuntime(_versionPath, _runtimePath);

        Assert.Equal("NEW CONTENT", File.ReadAllText(Path.Combine(_runtimePath, "defaults.parm")));
    }

    [Fact]
    public void SyncToRuntime_RemovesStaleJsonFiles()
    {
        // Setup runtime with a json file that won't exist in version
        Directory.CreateDirectory(_runtimePath);
        File.WriteAllText(Path.Combine(_runtimePath, "stale.json"), "{}");

        // Version has a different json file
        File.WriteAllText(Path.Combine(_versionPath, "current.json"), "{}");

        RuntimeSyncService.SyncToRuntime(_versionPath, _runtimePath);

        Assert.False(File.Exists(Path.Combine(_runtimePath, "stale.json")));
        Assert.True(File.Exists(Path.Combine(_runtimePath, "current.json")));
    }

    [Fact]
    public void SyncToRuntime_PreservesEepromBin()
    {
        // Setup runtime with eeprom.bin (user data)
        Directory.CreateDirectory(_runtimePath);
        File.WriteAllText(Path.Combine(_runtimePath, "eeprom.bin"), "user data");

        // Version has defaults.parm
        File.WriteAllText(Path.Combine(_versionPath, "defaults.parm"), "PARAM1 100");

        RuntimeSyncService.SyncToRuntime(_versionPath, _runtimePath);

        // eeprom.bin should still exist
        Assert.True(File.Exists(Path.Combine(_runtimePath, "eeprom.bin")));
        Assert.Equal("user data", File.ReadAllText(Path.Combine(_runtimePath, "eeprom.bin")));
    }

    [Fact]
    public void SyncToRuntime_PreservesLogsFolder()
    {
        // Setup runtime with logs folder
        Directory.CreateDirectory(_runtimePath);
        var logsDir = Path.Combine(_runtimePath, "logs");
        Directory.CreateDirectory(logsDir);
        File.WriteAllText(Path.Combine(logsDir, "flight.log"), "log data");

        // Sync
        File.WriteAllText(Path.Combine(_versionPath, "defaults.parm"), "PARAM1 100");
        RuntimeSyncService.SyncToRuntime(_versionPath, _runtimePath);

        // logs should still exist
        Assert.True(Directory.Exists(logsDir));
        Assert.True(File.Exists(Path.Combine(logsDir, "flight.log")));
    }

    [Fact]
    public void SyncToRuntime_PreservesTerrainFolder()
    {
        // Setup runtime with terrain folder
        Directory.CreateDirectory(_runtimePath);
        var terrainDir = Path.Combine(_runtimePath, "terrain");
        Directory.CreateDirectory(terrainDir);
        File.WriteAllText(Path.Combine(terrainDir, "tile.dat"), "terrain data");

        // Sync
        File.WriteAllText(Path.Combine(_versionPath, "defaults.parm"), "PARAM1 100");
        RuntimeSyncService.SyncToRuntime(_versionPath, _runtimePath);

        // terrain should still exist
        Assert.True(Directory.Exists(terrainDir));
        Assert.True(File.Exists(Path.Combine(terrainDir, "tile.dat")));
    }
}
