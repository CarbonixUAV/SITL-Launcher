using SITLLauncher.Core.Services;
using Xunit;

namespace SITLLauncher.Core.Tests;

public class LaunchBatParserTests
{
    [Fact]
    public void Parse_HeadlessConfig_ExtractsModelArgAndDefaults()
    {
        var pathToLaunchBat = @"C:\SITL-Launcher-V2\Versions\20251124_0700_CxPilot-7.3.0_033302b0\ottano-headless\launch.bat";
        var content = """
            rem Launch at Eli Field
            cd %~dp0
            ..\CxPilot-7.3.0.exe -O 40.0594626,-88.5513292,206.0,0 --serial0 tcp:0 -M quadplane:ottano-headless.json --defaults=defaults.parm
            """;

        var result = LaunchBatParser.Parse(pathToLaunchBat, content);

        Assert.NotNull(result);
        Assert.Equal("quadplane:ottano-headless.json", result.ModelArg);
        Assert.Equal("defaults.parm", result.DefaultsFile);
        Assert.Equal(@"C:\SITL-Launcher-V2\Versions\20251124_0700_CxPilot-7.3.0_033302b0\CxPilot-7.3.0.exe", result.ExecutablePath);
    }

    [Fact]
    public void Parse_RealFlightConfig_ExtractsModelArgAndDefaults()
    {
        // Uses --model and --defaults with space instead of -M and = to cover both syntaxes
        var pathToLaunchBat = @"C:\SITL-Launcher-V2\Versions\20251124_0700_CxPilot-7.3.0_033302b0\ottano-headless\launch.bat";
        var content = """
            rem Launch at Eli Field
            cd %~dp0
            ..\CxPilot-7.3.0.exe -O 40.0594626,-88.5513292,206.0,0 --serial0 tcp:0 --model flightaxis --defaults defaults.parm
            """;

        var result = LaunchBatParser.Parse(pathToLaunchBat, content);

        Assert.NotNull(result);
        Assert.Equal("flightaxis", result.ModelArg);
        Assert.Equal("defaults.parm", result.DefaultsFile);
        Assert.Equal(@"C:\SITL-Launcher-V2\Versions\20251124_0700_CxPilot-7.3.0_033302b0\CxPilot-7.3.0.exe", result.ExecutablePath);
    }

    [Fact]
    public void Parse_CustomDefaultsFile_ExtractsCorrectly()
    {
        var content = """
            ..\CxPilot.exe -M flightaxis --defaults=custom-params.parm
            """;

        var result = LaunchBatParser.Parse("", content);

        Assert.NotNull(result);
        Assert.Equal("custom-params.parm", result.DefaultsFile);
    }

    [Fact]
    public void Parse_EmptyContent_ReturnsNull()
    {
        var result = LaunchBatParser.Parse("", "");
        Assert.Null(result);
    }

    [Fact]
    public void Parse_NoFrameArg_ReturnsNull()
    {
        var content = """
            rem Just a comment
            cd %~dp0
            ..\CxPilot.exe --defaults=defaults.parm
            """;

        var result = LaunchBatParser.Parse("", content);
        Assert.Null(result);
    }

    [Fact]
    public void Parse_NoExecutablePath_ReturnsNull()
    {
        var pathToLaunchBat = @"C:\SITL-Launcher-V2\Versions\20251124_0700_CxPilot-7.3.0_033302b0\ottano-headless\launch.bat";
        var content = """
            rem Just a comment
            cd %~dp0
            -M flightaxis --defaults=defaults.parm
            """;

        var result = LaunchBatParser.Parse(pathToLaunchBat, content);
        Assert.Null(result);
    }
}
