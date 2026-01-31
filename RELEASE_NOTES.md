## What's New in v0.0.5

### CI/CD & Updates
- Add GitHub Actions CI/CD pipeline for automated builds and releases
- Add in-app update notifications (checks GitHub releases on startup)
- Add MinVer for automatic versioning from git tags

### Per-Distribution Resource Monitoring
- Add per-distribution CPU monitoring with real-time tracking
- Add per-distribution memory monitoring with usage display
- Add per-distribution disk usage monitoring
- Fix vmmem process detection to include vmmemWSL

### Configuration Management
- Add .wslconfig INI parser/writer with comment preservation

### System Tray Integration
- Add system tray icon with minimize-to-tray support
- Add animated splash screen for startup experience
- Add pinned distributions for quick access
- ~20MB memory footprint

### Core Features
- Distribution management (start, stop, restart, terminate)
- Install distributions from Microsoft Store catalog
- Clone, export, and import distributions

## Requirements

- Windows 10 version 2004+ or Windows 11
- WSL 2 enabled
- .NET 8.0 Runtime
