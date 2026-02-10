# Architecture

## Project Structure

```plaintext
src/
├── SITLLauncher.Core/          # Business logic, services, and view models
│   ├── Models/                 # Data models (LauncherConfig, Airport, Aircraft, etc.)
│   ├── Services/               # Core services (ConfigService, LauncherService, etc.)
│   └── ViewModels/             # MVVM view models
│
└── SITLLauncher.Desktop/       # Avalonia UI layer
    ├── Views/                  # XAML views (MainWindow, SettingsWindow, etc.)
    ├── Services/               # Avalonia-specific services (AvaloniaUiDispatcher)
    ├── App.axaml[.cs]          # Application entry point
    └── Program.cs              # Main method
```

## Layer Separation

### Core Library (SITLLauncher.Core)

- Platform-agnostic business logic
- No Avalonia dependencies
- Uses CommunityToolkit.Mvvm for MVVM primitives
- Testable in isolation

### Desktop UI (SITLLauncher.Desktop)

- Avalonia-specific implementation
- References Core library
- Handles UI thread marshaling via `IUiDispatcher`
- Constructs and wires services in [App.axaml.cs](src/SITLLauncher.Desktop/App.axaml.cs)

## Key Services

### ConfigService

[ConfigService.cs](src/SITLLauncher.Core/Services/ConfigService.cs)

Owns launcher configuration state and persists to config.json:

- Airports list
- Serial port configurations
- Last-used selections (version, aircraft, airport per aircraft)

Raises `ConfigChanged` event when settings are modified.

### LauncherService

[LauncherService.cs](src/SITLLauncher.Core/Services/LauncherService.cs)

Orchestrates the launch flow:

- Syncs config files to Runtime/ if version changed (via `RuntimeSyncService`)
- Builds command line arguments from the aircraft's `FrameConfig`
- Launches SITL process (via `ProcessRunner`)

### RuntimeSyncService

[RuntimeSyncService.cs](src/SITLLauncher.Core/Services/RuntimeSyncService.cs)

Manages Runtime/ folder structure. Version folders contain config files that ship with a
build (e.g., defaults.parm, scripts/, JSON configs). Runtime folders hold persistent data
that should survive version switches (e.g., eeprom.bin, logs/, terrain cache).

- Syncs config files and scripts from Versions/{version}/{aircraft}/ to Runtime/{aircraft}/
- Only runs when version changes for an aircraft
- Clobbers existing synced files and deletes stale ones
- Never touches persistent runtime data

### ProcessRunner

[ProcessRunner.cs](src/SITLLauncher.Core/Services/ProcessRunner.cs)

Manages SITL process lifecycle:

- Starts CxPilot.exe with configured arguments
- Captures stdout/stderr and raises events
- Provides Kill() to terminate process
- Notifies when process exits

### LaunchBatParser

[LaunchBatParser.cs](src/SITLLauncher.Core/Services/LaunchBatParser.cs)

Parses launch.bat files to build a `FrameConfig`: executable path, model argument
(`-M`), and defaults file (`--defaults`). Keeps V0 batch file configs as source of truth.

### VersionScanner

[VersionScanner.cs](src/SITLLauncher.Core/Services/VersionScanner.cs)

Scans Versions/ folder at startup:

- Returns list of `SitlVersion` objects
- Each version contains list of `Aircraft` (subfolders with launch.bat)
- Parses each aircraft's launch.bat via `LaunchBatParser` to populate `FrameConfig`

## MVVM Pattern

### MainWindowViewModel

[MainWindowViewModel.cs](src/SITLLauncher.Core/ViewModels/MainWindowViewModel.cs)

Main UI state:

- Versions, Aircraft, Airports observable collections
- Selected values for dropdowns
- Launch/Kill commands
- SITL output lines (stdout/stderr capture)
- Status text and running state

Uses CommunityToolkit.Mvvm source generators for boilerplate:

- `[ObservableProperty]` generates property change notifications
- `[RelayCommand]` generates command implementations
- `[NotifyCanExecuteChangedFor]` updates command enabled state

### SettingsViewModel

[SettingsViewModel.cs](src/SITLLauncher.Core/ViewModels/SettingsViewModel.cs)

Settings UI state:

- Airports DataGrid binding
- Serial ports DataGrid binding
- Installed versions list
- Add/Remove/Save commands

## Data Flow

1. App starts → `VersionScanner` scans Versions/ and parses each aircraft's launch.bat
2. User selects Version → Aircraft dropdown updates
3. User selects Aircraft → Airport defaults to last-used for this aircraft
4. User clicks Launch → `LauncherService` orchestrates:
   - Check if version changed (compare to `ConfigService.GetLastVersionForAircraft()`)
   - If changed, `RuntimeSyncService` syncs config files
   - `ConfigService.RecordLaunch()` persists selections
   - Build arguments from the aircraft's already-parsed `FrameConfig`
   - `ProcessRunner` starts SITL executable
5. SITL output → `ProcessRunner` events → `MainWindowViewModel.OutputLines`

## Configuration Storage

config.json structure:

```json
{
  "airports": [
    {"name": "YMAV", "location": "-37.664,143.056,133,90"}
  ],
  "serialPorts": [
    {"argument": "--serial0 tcp:0"},
    {"argument": "--serial1 udpclient:127.0.0.1:14550"}
  ],
  "lastSelectedVersion": "CxPilot-8.0.0-dev-b525b03b",
  "lastSelectedAircraft": "ottano-headless",
  "lastAirportPerAircraft": {
    "ottano-headless": "YMAV",
    "ottano-realflight": "YMAV"
  },
  "lastVersionPerAircraft": {
    "ottano-headless": "CxPilot-8.0.0-dev-b525b03b"
  }
}
```

## Wiring

Services are manually constructed in [App.axaml.cs](src/SITLLauncher.Desktop/App.axaml.cs)
during startup — no DI container.

## Threading Model

- SITL process runs on background thread
- `ProcessRunner` raises events on background threads
- `LauncherService` marshals all events to the UI thread via `IUiDispatcher` before
  exposing them — ViewModels never need to marshal

## MSIX Packaging Notes

Data lives in `%LOCALAPPDATA%\SITLLauncher`:

- `config.json` - virtualized on Win11 (auto-removed on uninstall), unvirtualized on Win10
- `Versions/` - excluded from virtualization
- `Runtime/` - excluded from virtualization

The manifest (see [Package.appxmanifest](../Packaging/Package.appxmanifest)) uses two
declarations so child processes (SITL exe) get real filesystem access:

- `desktop6:FileSystemWriteVirtualization` — Win10 1903+, disables all virtualization
- `virtualization:ExcludedDirectories` — Win11+, excludes only Versions/ and Runtime/

Windows 11 uses the per-directory exclusions and ignores desktop6. Windows 10 only
understands desktop6 (the `virtualization` namespace is in `IgnorableNamespaces`).
