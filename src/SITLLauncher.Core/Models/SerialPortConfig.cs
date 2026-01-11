namespace SITLLauncher.Core.Models;

/// <summary>
/// Represents a serial port argument for SITL launch.
/// </summary>
public record SerialPortConfig
{
    /// <summary>
    /// The command-line argument (e.g., "--serial0 tcp:0" and/or "--serial1 udpclient:127.0.0.1:14550").
    /// </summary>
    public required string Argument { get; init; }
}
