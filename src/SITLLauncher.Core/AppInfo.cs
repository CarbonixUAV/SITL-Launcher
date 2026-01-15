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
    /// Base directory for all app data (%LOCALAPPDATA%\SITLLauncher).
    /// </summary>
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppDataFolderName);
}
