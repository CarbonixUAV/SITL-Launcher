using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SITLLauncher.Core.Services;

/// <summary>
/// Handles syncing config files from Versions to Runtime directory.
/// </summary>
public class RuntimeSyncService
{
    /// <summary>
    /// File patterns to sync from version to runtime.
    /// </summary>
    private static readonly string[] FilePatterns = ["*.parm", "*.param", "*.json"];

    /// <summary>
    /// Directory names to sync from version to runtime.
    /// </summary>
    private static readonly string[] DirectoryNames = ["scripts"];

    /// <summary>
    /// Ensures the runtime directory exists for an aircraft.
    /// </summary>
    public static void EnsureRuntimeDirectory(string runtimeRoot, string aircraftName)
    {
        var runtimePath = Path.Combine(runtimeRoot, aircraftName);
        Directory.CreateDirectory(runtimePath);
    }

    /// <summary>
    /// Syncs config files from a version's aircraft folder to the runtime folder.
    /// Clobbers existing synced files and removes stale ones.
    /// </summary>
    public static void SyncToRuntime(string versionAircraftPath, string runtimeAircraftPath)
    {
        Directory.CreateDirectory(runtimeAircraftPath);

        // Track what files should exist after sync
        var expectedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Sync directories
        foreach (var dirName in DirectoryNames)
        {
            var sourceDir = Path.Combine(versionAircraftPath, dirName);
            var destDir = Path.Combine(runtimeAircraftPath, dirName);
            if (Directory.Exists(sourceDir))
            {
                SyncDirectory(sourceDir, destDir);
                expectedFiles.Add(dirName);
            }
        }

        // Sync config files (*.parm, *.param, *.json)
        foreach (var sourceFile in FilePatterns.SelectMany(p => Directory.GetFiles(versionAircraftPath, p)))
        {
            var fileName = Path.GetFileName(sourceFile);
            var destFile = Path.Combine(runtimeAircraftPath, fileName);
            File.Copy(sourceFile, destFile, overwrite: true);
            expectedFiles.Add(fileName);
        }

        // Remove stale files (files that were synced before but no longer exist in version)
        CleanupStaleFiles(runtimeAircraftPath, expectedFiles);
    }

    private static void SyncDirectory(string sourceDir, string destDir)
    {
        // Delete destination and recreate to ensure clean sync
        if (Directory.Exists(destDir))
            Directory.Delete(destDir, recursive: true);

        Directory.CreateDirectory(destDir);

        // Copy all files
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile);
        }

        // Copy subdirectories recursively
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            SyncDirectory(dir, destSubDir);
        }
    }

    private static void CleanupStaleFiles(string runtimePath, HashSet<string> expectedFiles)
    {
        // Remove stale config files
        foreach (var file in FilePatterns.SelectMany(p => Directory.GetFiles(runtimePath, p)))
        {
            var fileName = Path.GetFileName(file);
            if (!expectedFiles.Contains(fileName))
                File.Delete(file);
        }

        // Remove stale directories
        foreach (var dirName in DirectoryNames)
        {
            var dirPath = Path.Combine(runtimePath, dirName);
            if (Directory.Exists(dirPath) && !expectedFiles.Contains(dirName))
                Directory.Delete(dirPath, recursive: true);
        }
    }
}
