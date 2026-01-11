using System;
using System.Threading;
using SITLLauncher.Core.Models;
using SITLLauncher.Core.Services;
using Xunit;

namespace SITLLauncher.Core.Tests;

public class SitlProcessManagerTests
{
    [Fact]
    public void BuildArguments_BasicParams_FormatsCorrectly()
    {
        var launchParams = new SitlLaunchParams
        {
            ExecutablePath = @"C:\SITL\CxPilot.exe",
            WorkingDirectory = @"C:\Runtime\ottano",
            ModelArg = "quadplane:ottano.json",
            DefaultsFile = "defaults.parm",
            Location = "-33.8688,151.2093,10,90",
            SerialPortArgs = []
        };

        var args = SitlProcessManager.BuildArguments(launchParams);

        Assert.Equal("-M quadplane:ottano.json -O -33.8688,151.2093,10,90 --defaults defaults.parm", args);
    }

    [Fact]
    public void BuildArguments_WithSerialPorts_IncludesAll()
    {
        var launchParams = new SitlLaunchParams
        {
            ExecutablePath = @"C:\SITL\CxPilot.exe",
            WorkingDirectory = @"C:\Runtime\ottano",
            ModelArg = "flightaxis",
            DefaultsFile = "defaults.parm",
            Location = "-33.0,151.0,0,0",
            SerialPortArgs = ["--serial0 tcp:0", "--serial1 udpclient:127.0.0.1:14550"]
        };

        var args = SitlProcessManager.BuildArguments(launchParams);

        Assert.Contains("--serial0 tcp:0", args);
        Assert.Contains("--serial1 udpclient:127.0.0.1:14550", args);
    }

    [Fact]
    public void LaunchRaw_EchoCommand_CapturesOutput()
    {
        using var manager = new SitlProcessManager();
        var exitedEvent = new ManualResetEventSlim(false);
        int? exitCode = null;

        manager.Exited += (_, code) =>
        {
            exitCode = code;
            exitedEvent.Set();
        };

        manager.LaunchRaw("cmd.exe", "/c echo hello world");

        var exited = exitedEvent.Wait(TimeSpan.FromSeconds(5));

        Assert.True(exited, "Process did not exit in time");
        Assert.Equal(0, exitCode);
        Assert.Contains("hello world", manager.GetOutput());
    }

    [Fact]
    public void LaunchRaw_OutputReceivedEvent_Fires()
    {
        using var manager = new SitlProcessManager();
        var exitedEvent = new ManualResetEventSlim(false);
        string? receivedLine = null;

        manager.OutputReceived += (_, line) => receivedLine ??= line;
        manager.Exited += (_, _) => exitedEvent.Set();

        manager.LaunchRaw("cmd.exe", "/c echo test output");

        exitedEvent.Wait(TimeSpan.FromSeconds(5));

        Assert.NotNull(receivedLine);
        Assert.Contains("test output", receivedLine);
    }

    [Fact]
    public void IsRunning_AfterExit_ReturnsFalse()
    {
        using var manager = new SitlProcessManager();
        var exitedEvent = new ManualResetEventSlim(false);
        manager.Exited += (_, _) => exitedEvent.Set();

        manager.LaunchRaw("cmd.exe", "/c echo done");

        exitedEvent.Wait(TimeSpan.FromSeconds(5));
        Assert.False(manager.IsRunning);
    }
}
