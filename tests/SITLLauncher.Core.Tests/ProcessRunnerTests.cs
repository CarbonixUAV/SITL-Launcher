using System;
using System.Threading;
using SITLLauncher.Core.Services;
using Xunit;

namespace SITLLauncher.Core.Tests;

public class ProcessRunnerTests
{
    [Fact]
    public void Launch_EchoCommand_CapturesOutput()
    {
        using var runner = new ProcessRunner();
        var exitedEvent = new ManualResetEventSlim(false);
        int? exitCode = null;

        runner.Exited += (_, code) =>
        {
            exitCode = code;
            exitedEvent.Set();
        };

        runner.Launch("cmd.exe", "/c echo hello world");

        var exited = exitedEvent.Wait(TimeSpan.FromSeconds(5));

        Assert.True(exited, "Process did not exit in time");
        Assert.Equal(0, exitCode);
        Assert.Contains("hello world", runner.GetOutput());
    }

    [Fact]
    public void Launch_OutputReceivedEvent_Fires()
    {
        using var runner = new ProcessRunner();
        var exitedEvent = new ManualResetEventSlim(false);
        string? receivedLine = null;

        runner.OutputReceived += (_, line) => receivedLine ??= line;
        runner.Exited += (_, _) => exitedEvent.Set();

        runner.Launch("cmd.exe", "/c echo test output");

        exitedEvent.Wait(TimeSpan.FromSeconds(5));

        Assert.NotNull(receivedLine);
        Assert.Contains("test output", receivedLine);
    }

    [Fact]
    public void IsRunning_AfterExit_ReturnsFalse()
    {
        using var runner = new ProcessRunner();
        var exitedEvent = new ManualResetEventSlim(false);
        runner.Exited += (_, _) => exitedEvent.Set();

        runner.Launch("cmd.exe", "/c echo done");

        exitedEvent.Wait(TimeSpan.FromSeconds(5));
        Assert.False(runner.IsRunning);
    }
}
