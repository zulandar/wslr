# WSLR

A lightweight Windows desktop application for managing WSL (Windows Subsystem for Linux) distributions.

![Windows](https://img.shields.io/badge/Windows-10%2F11-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/License-MIT-green)

## Features

### Distribution Management
- **Start, Stop, Restart, Terminate**: Full lifecycle control of WSL distributions
- **Install from Store**: Browse and install distributions from Microsoft's online catalog
- **Clone Distributions**: Duplicate existing distributions for testing or development
- **Export & Import**: Backup and restore distributions as .tar files
- **Set Default**: Quickly set any distribution as your default WSL instance

### Real-time Resource Monitoring
- **Per-Distribution CPU**: Track CPU usage for each running distribution
- **Per-Distribution Memory**: Monitor memory consumption with live updates
- **Per-Distribution Disk**: View disk space usage for each distribution's virtual disk

### System Integration
- **System Tray**: Minimize to tray with quick access menu (~20MB memory footprint)
- **Pinned Distributions**: Keep frequently used distributions at the top of the list
- **Update Notifications**: Automatic check for new releases on startup
- **Start Minimized**: Optional setting to start the app minimized to tray

### Configuration
- **.wslconfig Support**: Parse and preserve WSL global configuration with comments
- **View Modes**: Switch between list and grid views for distribution display
- **Debug Logging**: Toggle detailed logging for troubleshooting

## Requirements

- Windows 10 version 2004+ or Windows 11
- WSL 2 enabled ([Installation Guide](https://learn.microsoft.com/en-us/windows/wsl/install))
- .NET 8.0 Runtime ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))

## Installation

### From Release (Recommended)

1. Download the latest release from the [Releases](https://github.com/zulandar/wslr/releases) page
2. Extract the zip file to your preferred location (e.g., `C:\Program Files\WSLR`)
3. Run `Wslr.App.exe`

### Run at Startup

To have WSLR start automatically with Windows, add a shortcut to `Wslr.App.exe` in your Startup folder:

**Option 1**: Press `Win+R`, type `shell:startup`, and create a shortcut to `Wslr.App.exe`

**Option 2**: Navigate to this folder and create the shortcut:
```
%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup
```

## Troubleshooting

See [TROUBLESHOOTING.md](TROUBLESHOOTING.md) for common issues and solutions.

## Contributing

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 or VS Code with C# extension

### Building from Source

```bash
# Clone the repository
git clone https://github.com/zulandar/wslr.git
cd wslr

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run --project src/Wslr.App
```

### Running Tests

```bash
dotnet test
```

### Project Structure

```
wslr/
├── src/
│   ├── Wslr.Core/           # Core domain models and interfaces
│   ├── Wslr.Infrastructure/ # WSL command execution and parsing
│   ├── Wslr.UI/             # ViewModels and UI services
│   └── Wslr.App/            # WPF application entry point
├── tests/
│   ├── Wslr.Core.Tests/
│   ├── Wslr.Infrastructure.Tests/
│   └── Wslr.UI.Tests/
└── Wslr.sln
```

## Data & Logs

WSLR stores its data in `%LOCALAPPDATA%\WSLR\`:

| File/Folder | Purpose |
|-------------|---------|
| `settings.json` | User preferences and pinned distributions |
| `Logs/` | Application logs (7-day rolling, ~50MB max) |

## License

MIT License - see [LICENSE](LICENSE) for details.
