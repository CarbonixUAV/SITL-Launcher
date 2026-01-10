using System;
using System.IO;
using System.Text.RegularExpressions;
using SITLLauncher.Core.Models;

namespace SITLLauncher.Core.Services;

/// <summary>
/// Parses launch.bat files to extract frame configuration.
/// </summary>
public static partial class LaunchBatParser
{
    // Matches: -M or --model followed by = or space, then the argument value
    [GeneratedRegex(@"(?:-M|--model)[=\s]+(\S+)")]
    private static partial Regex ModelArgRegex();

    // Matches: --defaults followed by = or space, then the filename
    [GeneratedRegex(@"--defaults[=\s]+(\S+)")]
    private static partial Regex DefaultsArgRegex();

    // Matches: the executable path at the start of the line (e.g., "..\CxPilot-7.3.0.exe")
    [GeneratedRegex(@"^[\s]*[""']?([^\s""']+\.exe)[""']?", RegexOptions.Multiline)]
    private static partial Regex ExecutablePathRegex();

    /// <summary>
    /// Parses the content of a launch.bat file and extracts the frame configuration.
    /// </summary>
    /// <param name="pathToFile">The path to the launch.bat file.</param>
    /// <param name="content">The full text content of the launch.bat file.</param>
    /// <returns>The parsed frame configuration, or null if required arguments not found.</returns>
    public static FrameConfig? Parse(string pathToFile, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        var modelMatch = ModelArgRegex().Match(content);
        var defaultsMatch = DefaultsArgRegex().Match(content);
        var executableMatch = ExecutablePathRegex().Match(content);

        if (!modelMatch.Success || !defaultsMatch.Success || !executableMatch.Success)
            return null;

        // Build a full path for the executable
        var executablePath = executableMatch.Groups[1].Value;
        if (!Path.IsPathRooted(executablePath))
        {
            var launchBatDir = Path.GetDirectoryName(pathToFile) ?? string.Empty;
            executablePath = Path.GetFullPath(
                Path.Combine(launchBatDir, executablePath)
            );
        }

        return new FrameConfig
        {
            ModelArg = modelMatch.Groups[1].Value,
            DefaultsFile = defaultsMatch.Groups[1].Value,
            ExecutablePath = executablePath
        };
    }
}
