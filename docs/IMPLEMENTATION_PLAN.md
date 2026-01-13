# Implementation Plan

## MVP

### Milestone 1: Core Architecture ✅ COMPLETED

- [x] Scaffold Avalonia MVVM project with Core/Desktop separation
- [x] Add launch.bat parser for frame type and model extraction
- [x] Add VersionScanner to enumerate Versions/ folder
- [x] Add config system (config.json) for airports, serial ports, and last-used selections
- [x] Add SitlProcessManager (now ProcessRunner) for SITL process lifecycle
- [x] Add RuntimeSyncService for syncing config files to Runtime/ on version change
- [x] Add LauncherService to orchestrate launch flow with UI thread dispatch
- [x] Write unit tests for core services

### Milestone 2: Main Launcher UI ✅ COMPLETED

- [x] Wire up MainWindowViewModel with dropdowns for Version/Aircraft/Airport
- [x] Implement Launch and Kill commands
- [x] Add status text and running state indicators
- [x] Add global exception error dialog with copy-to-clipboard
- [x] Add optional SITL stdout display (expandable output panel)
- [x] Implement dynamic MinHeight based on output panel state
- [x] Add ConfigChanged event for reactive UI updates

### Milestone 3: Settings UI ✅ COMPLETED

- [x] Create SettingsWindow with tabbed interface
- [x] Add Airports DataGrid (name, lat, lon, alt, heading)
- [x] Add Serial Ports DataGrid (argument, enabled)
- [x] Add Versions tab (list installed, delete action)
- [x] Add Maintenance tab (clear runtimes)
- [x] Wire up SettingsViewModel with Add/Remove/Save commands
- [x] Integrate SettingsWindow launch from MainWindow gear button

### Milestone 4: Version Installation (REMAINING FOR MVP)

**Goal:** Allow users to install new SITL versions by dragging a .7z archive onto the UI.

- [ ] Add drag-and-drop zone to MainWindow or SettingsWindow Versions tab
- [ ] Extract .7z file to Versions/ folder
  - [ ] Use SharpCompress or System.Formats.Tar + SharpZipLib for extraction
  - [ ] Show progress dialog during extraction
  - [ ] Handle errors (invalid archive, disk space, permissions)
- [ ] Validate extracted folder structure
  - [ ] Verify CxPilot.exe exists
  - [ ] Verify at least one aircraft subfolder with launch.bat
- [ ] Auto-refresh versions list after successful installation
- [ ] Add confirmation dialog if version already exists (overwrite/cancel)
- [ ] Write unit tests for extraction and validation logic

---

## Future Enhancements

### Milestone 5: Installer and Distribution

**Goal:** Package the launcher for easy distribution to Carbonix pilots.

- [ ] Create Windows installer (WiX Toolset or Inno Setup)
  - [ ] Install to Program Files
  - [ ] Create Start Menu shortcut
- [ ] Add versioning and auto-update mechanism (check GitHub releases)
- [ ] Create initial Versions/ bundle with latest CxPilot release
- [ ] Add README and user documentation
- [ ] Test installer on clean Windows VM

### Milestone 6: Channel-Based Version Selection

**Goal:** Replace version dropdown with Stable/Dev/Advanced channel selector for simplified workflow.

- [ ] Add `VersionChannel` enum (Stable, Dev, Advanced)
- [ ] Update config.json schema
  - [ ] Add `versionChannel` field (default: Stable)
  - [ ] Add `autoUpdateEnabled` bool (default: true)
  - [ ] Keep `lastSelectedVersion` for Advanced mode
- [ ] Update MainWindow UI
  - [ ] Replace version dropdown with channel radio buttons or combo
  - [ ] Show "Advanced" option that reveals manual version dropdown
  - [ ] Show current version name/tag when Stable/Dev selected (read-only)
- [ ] Add version metadata to track channels
  - [ ] Parse version naming convention (e.g., `CxPilot-8.0.0` = stable, `CxPilot-8.0.0-dev-abc123` = dev)
- [ ] Implement channel resolution logic
  - [ ] On launch, resolve Stable → latest stable version
  - [ ] Resolve Dev → latest dev version
  - [ ] Advanced → use selected version as before
- [ ] Update LauncherService to use resolved version
- [ ] Write unit tests for channel resolution and version comparison

### Milestone 7: S3 Integration

**Goal:** Fetch dev/stable builds directly from Carbonix CI artifacts on S3.

- [ ] Add AWS SDK for .NET (AWSSDK.S3)
- [ ] Create S3Service to list available builds by channel
- [ ] Add S3 credentials configuration (Settings → S3 tab)
- [ ] Add "Download from S3" UI in SettingsWindow Versions tab
  - [ ] Dropdown to select channel (dev/stable)
  - [ ] List available versions from S3 bucket
  - [ ] Download and extract in background with progress
- [ ] Cache downloaded .7z files (optional setting)
- [ ] Add auto-update check for dev/stable channels (on startup, if enabled)
- [ ] Write unit tests for S3Service

### Milestone 8: Logging and Diagnostics

**Goal:** Improve troubleshooting and telemetry capture.

- [ ] Add logging framework (Serilog)
- [ ] Log all launch attempts with arguments and results
- [ ] Add "View Logs" button in SettingsWindow Maintenance tab
- [ ] Add "Check Installation" diagnostic
  - [ ] Verify Versions/ and Runtime/ structure
  - [ ] Check for stale Runtime/ folders (versions no longer installed)
  - [ ] Validate config.json schema
- [ ] Write unit tests for diagnostic checks

### Milestone 9: RealFlight Control

**Goal:** Automate RealFlight startup by editing its INI file to select aircraft/airport.

- [ ] Research RealFlight INI file structure and location
- [ ] Create RealFlightService to manipulate INI
  - [ ] Parse existing INI
  - [ ] Update aircraft selection
  - [ ] Update airport/location (if supported)
- [ ] Add "Close RealFlight" action before launch (if running)
- [ ] Add "Launch RealFlight" checkbox in MainWindow (for -realflight configs)
- [ ] Relaunch RealFlight after INI edits
- [ ] Handle errors (RealFlight not installed, INI locked, process detection)
- [ ] Write unit tests for INI parsing and updates

---

## Notes

- **Testing Strategy**: Each milestone with code changes includes unit test action items. Integration tests can be added post-MVP for critical paths (launch flow, config persistence).
- **Architecture Preservation**: All enhancements maintain Core/Desktop separation. New services go in Core, UI components in Desktop.
- **Backward Compatibility**: config.json schema changes should migrate gracefully (add defaults for missing fields).
