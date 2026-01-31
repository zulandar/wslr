# WSLR

A lightweight Windows desktop application for managing WSL (Windows Subsystem for Linux) distributions.

## Features

- **Distribution Management**: List, start, stop, and restart WSL distributions
- **Install & Clone**: Create new distros from online images or clone existing ones
- **Export & Import**: Backup and restore distributions as tar files
- **Real-time Monitoring**: Live CPU, memory, and disk usage per distribution
- **System Tray**: Quick access from the system tray with minimal footprint (~20MB)
- **Pinned Distributions**: Keep your frequently used distros at the top

## Requirements

- Windows 10 version 2004+ or Windows 11
- WSL 2 enabled
- .NET 8.0 Runtime

## Installation

### End User

1. Download the latest release from the [Releases](https://github.com/zulandar/wslr/releases) page
2. Extract the zip file to your preferred location
3. Run `Wslr.App.exe`

To run at startup, add a shortcut to `Wslr.App.exe` in your Startup folder:
```
%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup
```

### Contributor

#### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 or VS Code with C# extension

#### Building from Source

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

#### Running Tests

```bash
dotnet test
```

#### Project Structure

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

## License

MIT License - see [LICENSE](LICENSE) for details.
