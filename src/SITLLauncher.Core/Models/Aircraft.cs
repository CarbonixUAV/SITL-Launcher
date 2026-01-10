namespace SITLLauncher.Core.Models;

/// <summary>
/// Represents an aircraft configuration discovered in a version folder.
/// </summary>
public record Aircraft
{
    /// <summary>
    /// Aircraft name (folder name, e.g., "ottano-headless").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Full path to the aircraft folder.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Frame configuration parsed from launch.bat.
    /// </summary>
    public required FrameConfig FrameConfig { get; init; }
}
