using System.Collections.Generic;

namespace SITLLauncher.Core.Models;

/// <summary>
/// Represents a SITL version discovered in the Versions folder.
/// </summary>
public record SitlVersion
{
    /// <summary>
    /// Version folder name (e.g., "20251124_0700_CxPilot-7.3.0_033302b0").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Full path to the version folder.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Aircraft configurations found in this version.
    /// </summary>
    public required IReadOnlyList<Aircraft> Aircraft { get; init; }
}
