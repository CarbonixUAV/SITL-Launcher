using System.IO;
using System.Text.Json;
using SITLLauncher.Core.Models;

namespace SITLLauncher.Core.Services;

/// <summary>
/// Loads and saves launcher configuration from config.json.
/// </summary>
public class ConfigService(string configPath)
{
    private readonly string _configPath = configPath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Loads configuration from disk. Returns default config if file doesn't exist.
    /// </summary>
    public LauncherConfig Load()
    {
        if (!File.Exists(_configPath))
            return new LauncherConfig();

        var json = File.ReadAllText(_configPath);
        return JsonSerializer.Deserialize<LauncherConfig>(json, JsonOptions) ?? new LauncherConfig();
    }

    /// <summary>
    /// Saves configuration to disk.
    /// </summary>
    public void Save(LauncherConfig config)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(_configPath, json);
    }
}
