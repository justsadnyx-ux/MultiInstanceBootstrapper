# MultiInstance Bootstrapper

A sleek, dark-themed multi-instance Roblox bootstrapper that allows you to run up to 3 Roblox instances simultaneously with auto-update support.

![GitHub Release](https://img.shields.io/badge/version-1.2.2-blue)
![License](https://img.shields.io/badge/license-MIT-green)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)

## Features

- **Multiple Instances**: Launch up to 3 Roblox instances simultaneously
- **Instance Management**: Kill individual instances or all at once
- **Dark Sleek UI**: Modern dark theme with responsive design
- **Auto-Updates**: Silently checks for updates and downloads them automatically
- **User Profile**: Displays your Roblox display name and avatar
- **Instance Count**: Shows active instance count (X/3)
- **Version Display**: Shows current bootstrapper version in the UI
- **Auto Launch**: Seamless Roblox instance launching

## Requirements

- Windows 10/11
- .NET 8.0 or later (included in self-contained build)
- Roblox installed

## How to Run

### Download Release
1. Go to the [Releases](https://github.com/justsadnyx-ux/MultiInstanceBootstrapper/releases) page
2. Download the latest `MultiInstanceBootstrapper.zip`
3. Extract the ZIP file (you will get a `MultiInstanceBootstrapper` folder)
4. Run `MultiInstanceBootstrapper.exe` **from inside that folder** (do not move the exe alone — it needs the DLLs next to it)

### Build from Source
```bash
# Clone the repository
git clone https://github.com/justsadnyx-ux/MultiInstanceBootstrapper.git
cd MultiInstanceBootstrapper

# Build the solution
dotnet build MultiInstanceBootstrapper.sln

# Run the bootstrapper
dotnet run --project MultiInstanceBootstrapper/MultiInstanceBootstrapper.csproj

# Build a self-contained folder release
dotnet publish MultiInstanceBootstrapper/MultiInstanceBootstrapper.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false
```

## Usage

1. **Launch Instance**: Click "Launch Instance" to start a Roblox instance (up to 3)
2. **Kill Instance**: Click "Kill" on a running instance card to close it
3. **Kill All**: Click "Kill All" to close all running instances
4. **Check Updates**: Click "Check Updates" to manually check for new versions
5. **Auto-Update**: The bootstrapper automatically checks for updates every 5 minutes

## How It Works

1. The bootstrapper detects your Roblox installation path from the Windows Registry
2. It launches `RobloxPlayerBeta.exe` with instance-specific parameters
3. Each instance runs independently in its own process
4. The auto-update system checks GitHub Releases for newer versions
5. When an update is found, it downloads the new package and applies it on next launch

## Auto-Update System

- Checks GitHub Releases API every 5 minutes for updates
- Downloads the latest release silently in the background
- Notifies you when an update is available
- All update logic is built directly into the executable (no separate updater needed)
- The app launches a small hidden helper that replaces the old exe and restarts automatically

## Project Structure

```
MultiInstanceBootstrapper/
├── MultiInstanceBootstrapper/          # Main application (self-updating exe + DLLs)
│   ├── MainWindow.xaml                 # Main UI (dark theme)
│   ├── MainWindow.xaml.cs             # UI logic
│   ├── App.xaml                       # Application entry
│   ├── Models/
│   │   └── RobloxInstance.cs          # Instance model
│   ├── Services/
│   │   ├── RobloxService.cs           # Roblox detection & launch
│   │   ├── InstanceService.cs         # Instance management
│   │   └── UpdateService.cs           # Auto-update & self-replace logic
│   ├── Helpers/
│   │   ├── Constants.cs              # App constants
│   │   └── BoolToVisibilityConverter.cs
│   └── Resources/
│       └── Styles.xaml               # Dark theme styles
├── .github/
│   └── workflows/
│       └── release.yml               # GitHub Actions release
├── MultiInstanceBootstrapper.sln       # Solution file
└── README.md
```

## Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

MIT License - See [LICENSE](LICENSE) for details

## Disclaimer

This is **NOT** a Roblox exploit tool, no scripts, and no injections. This tool simply launches the official Roblox client multiple times with different instance parameters. Use at your own risk.
