namespace SITLLauncher.Core.Models;

/// <summary>
/// Represents the frame configuration parsed from launch.bat's -M argument.
/// </summary>
public record FrameConfig
{
    /// <summary>
    /// The raw -M argument value (e.g., "quadplane:ottano-headless.json" or "flightaxis").
    /// </summary>
    public required string ModelArg { get; init; }

    /// <summary>
    /// The --defaults argument value (e.g., "defaults.parm").
    /// </summary>
    public required string DefaultsFile { get; init; }

    /// <summary>
    /// Full path to the executable, parsed from launch.bat.
    /// </summary>
    public required string ExecutablePath { get; init; }
}
