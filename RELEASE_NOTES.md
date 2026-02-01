## What's New in v0.1.0

### Embedded Terminal
- Add multi-tab embedded terminal with WebView2 + xterm.js
- Terminal context menu for copy/paste operations
- Terminal theming support

### WSL Configuration Management
- Add WSL configuration management UI for .wslconfig editing
- Add configuration templates (Development, Server, Isolated, LowMemory)
- Add configuration profiles for quick WSL switching
- Template import/export for sharing configurations

### Script Editor & Automation
- Add Script Editor UI for creating and managing setup scripts
- Built-in script templates (Dev environment, Node.js, Python, Docker, etc.)
- Setup script integration with import/export operations
- Execute scripts with variable substitution and progress reporting
- Sidecar file support (.wslr-template.json, .setup.sh) for distribution bundles

### UI Improvements
- Add resizable left panel to Templates and Profiles views
- Fix splash screen animations freezing during app initialization
- Disable action buttons during import/install operations
- Add version display to status bar

### Build & Release
- Add framework-dependent release alongside self-contained build
- Bundle xterm.js from npm instead of CDN files
- Add CI status badge to README
- Security update: esbuild 0.24.2 → 0.25.0 (CORS vulnerability fix)

### Testing & Quality
- Add code coverage tooling with coverlet and ReportGenerator
- Add Wslr.App.Tests project with converter tests
- Add ViewModel unit tests to Wslr.UI.Tests
- Add Core model and Infrastructure service tests

### Bug Fixes
- Fix import/export error messages being silently discarded
- Fix template selection not showing details panel
- Fix template loading on single-click in Script Editor
- Fix start distribution error by using 'true' command
- Fix disk usage calculation to match per-distribution totals
- Fix embedded terminal and clean up debug logging
- Reduce log file size limit to stay under 50MB total

---

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
