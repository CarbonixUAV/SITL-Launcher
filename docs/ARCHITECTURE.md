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
    └── Program.cs              # Main method, DI container setup
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
- Sets up dependency injection in [Program.cs](src/SITLLauncher.Desktop/Program.cs)

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

1. Validates selections (version, aircraft, airport)
2. Syncs config files to Runtime/ if version changed (via `RuntimeSyncService`)
3. Parses launch.bat for frame type and model (via `LaunchBatParser`)
4. Builds command line arguments
5. Launches SITL process (via `ProcessRunner`)
6. Records launch in `ConfigService`

### RuntimeSyncService

[RuntimeSyncService.cs](src/SITLLauncher.Core/Services/RuntimeSyncService.cs)

Manages Runtime/ folder structure:

- Syncs scripts/, defaults.parm, *.json from Versions/{version}/{aircraft}/ to Runtime/{aircraft}/
- Only runs when version changes for an aircraft
- Clobbers existing files and deletes stale ones
- Preserves eeprom.bin, logs/, and terrain/ (persistent data)

### ProcessRunner

[ProcessRunner.cs](src/SITLLauncher.Core/Services/ProcessRunner.cs)

Manages SITL process lifecycle:

- Starts CxPilot.exe with configured arguments
- Captures stdout/stderr and raises events
- Provides Kill() to terminate process
- Notifies when process exits

### LaunchBatParser

[LaunchBatParser.cs](src/SITLLauncher.Core/Services/LaunchBatParser.cs)

Parses launch.bat files to extract:

- Frame type (`quadplane` or `flightaxis`)
- Model parameter (JSON file for quadplane, empty for flightaxis)

Keeps V0 batch file configs as source of truth.

### VersionScanner

[VersionScanner.cs](src/SITLLauncher.Core/Services/VersionScanner.cs)

Scans Versions/ folder:

- Returns list of `SitlVersion` objects
- Each version contains list of `Aircraft` (subfolders with launch.bat)

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

1. User selects Version → Aircraft dropdown updates
2. User selects Aircraft → Airport defaults to last-used for this aircraft
3. User clicks Launch → `LauncherService` orchestrates:
   - Check if version changed (compare to `ConfigService.GetLastVersionForAircraft()`)
   - If changed, `RuntimeSyncService` syncs config files
   - `LaunchBatParser` reads frame type from launch.bat
   - `ProcessRunner` starts CxPilot.exe
   - `ConfigService.RecordLaunch()` persists selections
4. SITL output → `ProcessRunner` events → `MainWindowViewModel.OutputLines`

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

## Dependency Injection

Set up in [Program.cs](src/SITLLauncher.Desktop/Program.cs):

- Services registered as singletons
- ViewModels registered as transient
- Avalonia's built-in DI container used

## Threading Model

- SITL process runs on background thread
- UI updates marshaled via `IUiDispatcher.InvokeAsync()`
- `ProcessRunner` events raised on background thread, ViewModel handles marshaling

## MSIX Packaging Notes

Data lives in `%LOCALAPPDATA%\SITLLauncher`:

- `config.json` - virtualized (auto-removed on uninstall)
- `Versions/` - excluded from virtualization
- `Runtime/` - excluded from virtualization

The Versions and Runtime folders are excluded from MSIX file system virtualization
(see [Package.appxmanifest](../Packaging/Package.appxmanifest)) because child processes
(SITL exe) are not subject to virtualization and need real filesystem access to these
folders.
