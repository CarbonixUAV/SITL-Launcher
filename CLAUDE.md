# SITL Launcher V2

## Overview

Avalonia-based launcher for Carbonix's ArduPilot Software In The Loop (SITL) simulation, used internally for pilot training and beta testing. Replaces the previous tkinter GUI (V1) and batch file approach (V0).

## Key Decisions

- **Avalonia over Electron/Tauri/WinForms** - Learning investment for future projects; modern XAML/MVVM patterns
- **Windows-only target** - GCS and RealFlight are Windows-based, no need for cross-platform
- **Parse launch.bat for frame/model** - Keeps V0 configs as source of truth, no migration needed, existing setups work on day one
- **Runtime folder separate from Versions** - eeprom.bin, logs, and terrain cache persist across version switches
- **Sync config files on version change only** - When switching versions, clobber scripts/defaults/jsons and delete stale files, but don't touch if staying on same version (preserves user additions)
- **Last-used airport tracked per aircraft** - Keeps headless and RealFlight defaults separate naturally

## Reference Documents

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) - Code structure, data flow, key services (read before exploring)
- [docs/IMPLEMENTATION_PLAN.md](docs/IMPLEMENTATION_PLAN.md) - Development roadmap and future enhancements

## Conventions

- MVVM pattern for Avalonia
- Keep UI simple - this is a launcher, not a flight planner

## Code Changes

- When moving/renaming files, keep content identical to preserve git history
- Cosmetic cleanups (whitespace, comments, formatting) belong in separate commits

## Git

- Conventional Commits format
- Subject line: max 50 characters
- Body: hard wrap at 72 characters, keep terse, omit if not needed
- No AI co-author tags in commits; AI attribution goes in README instead
