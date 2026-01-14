using System;
using System.IO;
using SITLLauncher.Core.Services;
using Xunit;

namespace SITLLauncher.Core.Tests;

public class VersionInstallerTests
{
    [Fact]
    public void ValidateVersion_WithValidAircraft_Succeeds()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var aircraftDir = Path.Combine(tempDir, "ottano-headless");
        Directory.CreateDirectory(aircraftDir);
        File.WriteAllText(Path.Combine(aircraftDir, "launch.bat"), "@echo test");

        try
        {
            // Should not throw
            VersionInstaller.ValidateVersion(tempDir);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateVersion_EmptyDirectory_ThrowsInvalidOperation()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => VersionInstaller.ValidateVersion(tempDir));
            Assert.Contains("No aircraft folders found", ex.Message);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateVersion_AircraftWithoutLaunchBat_ThrowsInvalidOperation()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var aircraftDir = Path.Combine(tempDir, "ottano-headless");
        Directory.CreateDirectory(aircraftDir);
        // Create a file that's NOT launch.bat
        File.WriteAllText(Path.Combine(aircraftDir, "readme.txt"), "test");

        try
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => VersionInstaller.ValidateVersion(tempDir));
            Assert.Contains("No valid aircraft configurations found", ex.Message);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateVersion_MultipleAircraftOneValid_Succeeds()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // First aircraft - invalid (no launch.bat)
        var invalidDir = Path.Combine(tempDir, "invalid-aircraft");
        Directory.CreateDirectory(invalidDir);
        File.WriteAllText(Path.Combine(invalidDir, "readme.txt"), "test");

        // Second aircraft - valid (has launch.bat)
        var validDir = Path.Combine(tempDir, "valid-aircraft");
        Directory.CreateDirectory(validDir);
        File.WriteAllText(Path.Combine(validDir, "launch.bat"), "@echo test");

        try
        {
            // Should not throw - one valid aircraft is enough
            VersionInstaller.ValidateVersion(tempDir);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateVersion_MultipleValidAircraft_Succeeds()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        var aircraft1 = Path.Combine(tempDir, "ottano-headless");
        var aircraft2 = Path.Combine(tempDir, "ottano-realflight");
        Directory.CreateDirectory(aircraft1);
        Directory.CreateDirectory(aircraft2);
        File.WriteAllText(Path.Combine(aircraft1, "launch.bat"), "@echo test1");
        File.WriteAllText(Path.Combine(aircraft2, "launch.bat"), "@echo test2");

        try
        {
            // Should not throw
            VersionInstaller.ValidateVersion(tempDir);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateVersion_AllAircraftInvalid_ThrowsInvalidOperation()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        var aircraft1 = Path.Combine(tempDir, "aircraft1");
        var aircraft2 = Path.Combine(tempDir, "aircraft2");
        Directory.CreateDirectory(aircraft1);
        Directory.CreateDirectory(aircraft2);
        // Neither has launch.bat
        File.WriteAllText(Path.Combine(aircraft1, "other.txt"), "test");
        File.WriteAllText(Path.Combine(aircraft2, "config.json"), "{}");

        try
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => VersionInstaller.ValidateVersion(tempDir));
            Assert.Contains("No valid aircraft configurations found", ex.Message);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
