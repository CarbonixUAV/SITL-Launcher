namespace SITLLauncher.Core.Models;

/// <summary>
/// Represents an airport/location for SITL launch.
/// </summary>
public record Airport
{
    /// <summary>
    /// Display name (e.g., "Riverstone South").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Location string passed to -O argument (e.g., "-33.8688,151.2093,10,90").
    /// Format: lat,lon,alt,heading
    /// </summary>
    public required string Location { get; init; }
}
