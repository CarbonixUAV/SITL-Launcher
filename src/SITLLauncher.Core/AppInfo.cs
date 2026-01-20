using System;
using System.IO;

namespace SITLLauncher.Core;

/// <summary>
/// Application-level constants.
/// </summary>
public static class AppInfo
{
    private const string AppDataFolderName = "SITLLauncher";

    /// <summary>
    /// Directory for Versions and Runtime folders.
    /// Always the real %LOCALAPPDATA%\SITLLauncher path so child processes can access it.
    /// </summary>
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppDataFolderName);
}
