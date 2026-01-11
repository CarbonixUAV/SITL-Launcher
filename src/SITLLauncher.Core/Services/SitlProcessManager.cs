using System;
using System.Diagnostics;
using System.Text;
using SITLLauncher.Core.Models;

namespace SITLLauncher.Core.Services;

/// <summary>
/// Manages the lifecycle of a SITL process.
/// </summary>
public class SitlProcessManager : IDisposable
{
    private Process? _process;
    private readonly object _lock = new();
    private readonly StringBuilder _outputBuffer = new();

    /// <summary>
    /// Whether a SITL process is currently running.
    /// </summary>
    public bool IsRunning
    {
        get
        {
            lock (_lock)
            {
                return _process is not null && !_process.HasExited;
            }
        }
    }

    /// <summary>
    /// Raised when output is received from stdout or stderr.
    /// </summary>
    public event EventHandler<string>? OutputReceived;

    /// <summary>
    /// Raised when the process exits.
    /// </summary>
    public event EventHandler<int>? Exited;

    /// <summary>
    /// Gets all captured output so far.
    /// </summary>
    public string GetOutput()
    {
        lock (_lock)
        {
            return _outputBuffer.ToString();
        }
    }

    /// <summary>
    /// Launches SITL with the given parameters.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if a process is already running.</exception>
    public void Launch(SitlLaunchParams launchParams)
    {
        var args = BuildArguments(launchParams);
        LaunchRaw(launchParams.ExecutablePath, args, launchParams.WorkingDirectory);
    }

    /// <summary>
    /// Launches a process with raw executable and arguments.
    /// </summary>
    internal void LaunchRaw(string executable, string arguments, string? workingDirectory = null)
    {
        lock (_lock)
        {
            if (IsRunning)
                throw new InvalidOperationException("A process is already running.");

            _outputBuffer.Clear();

            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                WorkingDirectory = workingDirectory ?? "",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

            _process.OutputDataReceived += OnOutputDataReceived;
            _process.ErrorDataReceived += OnOutputDataReceived;
            _process.Exited += OnProcessExited;

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }
    }

    /// <summary>
    /// Kills the running process if any.
    /// </summary>
    public void Kill()
    {
        lock (_lock)
        {
            if (_process is null || _process.HasExited)
                return;

            _process.Kill(entireProcessTree: true);
        }
    }

    /// <summary>
    /// Builds the command-line arguments for SITL from launch parameters.
    /// </summary>
    public static string BuildArguments(SitlLaunchParams p)
    {
        var sb = new StringBuilder();

        // Model (frame type)
        sb.Append($"-M {p.ModelArg}");

        // Location
        sb.Append($" -O {p.Location}");

        // Defaults file
        sb.Append($" --defaults {p.DefaultsFile}");

        // Serial ports
        foreach (var serialArg in p.SerialPortArgs)
        {
            sb.Append($" {serialArg}");
        }

        return sb.ToString();
    }

    private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data is null)
            return;

        lock (_lock)
        {
            _outputBuffer.AppendLine(e.Data);
        }

        OutputReceived?.Invoke(this, e.Data);
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        int exitCode;
        lock (_lock)
        {
            exitCode = _process?.ExitCode ?? -1;
        }

        Exited?.Invoke(this, exitCode);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_process is not null)
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                    _process.WaitForExit(1000); // Allow streams to flush
                }

                _process.OutputDataReceived -= OnOutputDataReceived;
                _process.ErrorDataReceived -= OnOutputDataReceived;
                _process.Exited -= OnProcessExited;
                _process.Dispose();
                _process = null;
            }
        }
    }
}
