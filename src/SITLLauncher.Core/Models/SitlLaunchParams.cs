using System.Collections.Generic;

namespace SITLLauncher.Core.Models;

/// <summary>
/// Parameters needed to launch a SITL instance.
/// </summary>
public record SitlLaunchParams
{
    /// <summary>
    /// Full path to the SITL executable (e.g., Versions/.../CxPilot.exe).
    /// </summary>
    public required string ExecutablePath { get; init; }

    /// <summary>
    /// Working directory for the process (e.g., Runtime/ottano-headless/).
    /// </summary>
    public required string WorkingDirectory { get; init; }

    /// <summary>
    /// The -M argument value (e.g., "quadplane:ottano-headless.json" or "flightaxis").
    /// </summary>
    public required string ModelArg { get; init; }

    /// <summary>
    /// The --defaults file path (e.g., "defaults.parm").
    /// </summary>
    public required string DefaultsFile { get; init; }

    /// <summary>
    /// Location string for -O argument (e.g., "-33.8688,151.2093,10,90").
    /// </summary>
    public required string Location { get; init; }

    /// <summary>
    /// Serial port arguments (e.g., ["--serial0 tcp:0", "--serial1 udpclient:..."]).
    /// </summary>
    public required IReadOnlyList<string> SerialPortArgs { get; init; }
}
