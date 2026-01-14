using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SITLLauncher.Core.Services;

/// <summary>
/// Handles installation of SITL versions from .7z archives.
/// </summary>
public class VersionInstaller(string versionsPath)
{
    private readonly string _versionsPath = versionsPath;

    /// <summary>
    /// Extracts a .7z archive to the Versions folder.
    /// The archive must contain a 'sitl' folder which will be renamed to match the archive name.
    /// </summary>
    /// <param name="archivePath">Path to the .7z file.</param>
    /// <param name="progress">Optional progress callback (0.0 to 1.0).</param>
    /// <returns>Path to the installed version folder.</returns>
    /// <exception cref="FileNotFoundException">Archive file doesn't exist.</exception>
    /// <exception cref="InvalidOperationException">Archive structure is invalid.</exception>
    public string Install(string archivePath, IProgress<double>? progress = null)
    {
        if (!File.Exists(archivePath))
            throw new FileNotFoundException("Archive file not found", archivePath);

        var archiveFileName = Path.GetFileNameWithoutExtension(archivePath);
        var tempExtractPath = Path.Combine(Path.GetTempPath(), $"sitl_extract_{Guid.NewGuid()}");
        var finalVersionPath = Path.Combine(_versionsPath, archiveFileName);

        try
        {
            // Check if version already exists first
            if (Directory.Exists(finalVersionPath))
            {
                throw new InvalidOperationException(
                    $"Version '{archiveFileName}' is already installed. Delete it first to reinstall.");
            }

            // Create temp extraction directory
            Directory.CreateDirectory(tempExtractPath);

            // Extract using Windows' built-in tar command (supports 7z in Windows 10+)
            progress?.Report(0.1);

            var startInfo = new ProcessStartInfo
            {
                FileName = "tar",
                Arguments = $"-xf \"{archivePath}\" -C \"{tempExtractPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // Get archive size for progress estimation
            var archiveSize = new FileInfo(archivePath).Length;

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                    throw new InvalidOperationException("Failed to start extraction process");

                // Monitor extraction progress by checking output directory size
                var cancellationSource = new System.Threading.CancellationTokenSource();
                var progressTask = System.Threading.Tasks.Task.Run(async () =>
                {
                    while (!process.HasExited && !cancellationSource.Token.IsCancellationRequested)
                    {
                        try
                        {
                            if (Directory.Exists(tempExtractPath))
                            {
                                var extractedSize = GetDirectorySize(tempExtractPath);
                                // Report progress between 10% and 80%, based on extracted size vs archive size
                                var ratio = Math.Min(1.0, (double)extractedSize / archiveSize);
                                progress?.Report(0.1 + (ratio * 0.7));
                            }
                        }
                        catch
                        {
                            // Ignore errors during progress monitoring
                        }
                        await System.Threading.Tasks.Task.Delay(200);
                    }
                }, cancellationSource.Token);

                process.WaitForExit();
                cancellationSource.Cancel();

                try
                {
                    progressTask.Wait();
                }
                catch
                {
                    // Ignore cancellation exceptions
                }

                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEnd();
                    throw new InvalidOperationException($"Extraction failed: {error}");
                }
            }

            progress?.Report(0.8);

            // Validate structure - must contain a 'sitl' folder
            var sitlFolder = Path.Combine(tempExtractPath, "sitl");
            if (!Directory.Exists(sitlFolder))
            {
                throw new InvalidOperationException(
                    "Archive does not contain a 'sitl' folder at the root level");
            }

            // Create Versions directory if it doesn't exist
            Directory.CreateDirectory(_versionsPath);

            // Move sitl folder to final location with correct name
            progress?.Report(0.9);
            Directory.Move(sitlFolder, finalVersionPath);

            // Validate installed version has at least one aircraft
            ValidateVersion(finalVersionPath);

            progress?.Report(1.0);
            return finalVersionPath;
        }
        finally
        {
            // Clean up temp directory
            if (Directory.Exists(tempExtractPath))
            {
                try
                {
                    Directory.Delete(tempExtractPath, recursive: true);
                }
                catch
                {
                    // Best effort cleanup
                }
            }
        }
    }

    internal static void ValidateVersion(string versionPath)
    {
        // Check for at least one valid aircraft configuration
        var aircraftDirs = Directory.GetDirectories(versionPath);
        if (aircraftDirs.Length == 0)
        {
            throw new InvalidOperationException(
                "No aircraft folders found in version directory");
        }

        var hasValidAircraft = false;

        foreach (var aircraftDir in aircraftDirs)
        {
            // Check for launch.bat - that's all we need
            if (File.Exists(Path.Combine(aircraftDir, "launch.bat")))
            {
                hasValidAircraft = true;
                break;
            }
        }

        if (!hasValidAircraft)
        {
            throw new InvalidOperationException(
                "No valid aircraft configurations found (missing launch.bat)");
        }
    }

    private static long GetDirectorySize(string path)
    {
        try
        {
            var dirInfo = new DirectoryInfo(path);
            return dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                .Sum(file => file.Length);
        }
        catch
        {
            return 0;
        }
    }
}
