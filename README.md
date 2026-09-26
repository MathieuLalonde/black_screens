# Black Screens

System-tray app for Windows that blacks out selected monitors with solid overlays without disabling displays, so your desktop layout (and fullscreen video) stays put.

Useful when watching video on multi-monitor systems without being disturbed by other monitors.

## Download

Grab the latest self-contained `BlackScreens.exe` from [Releases](https://github.com/MathieuLalonde/black_screens/releases) — no .NET install required.

(The binary is shipped via Releases rather than committed to git; GitHub rejects files over 100 MB, and the self-contained build is ~154 MB.)

## Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) to build (or any newer SDK that can target `net8.0-windows`)

## Run (development)

```bash
dotnet run
```

Right-click the tray icon:

- Check the monitors you want blacked out
- Check **Activate blackout** to show the overlays
- Uncheck **Activate blackout** to clear them
- **Exit** to quit

Left-click the tray icon to toggle the blackout.

Settings are saved under `%AppData%\BlackScreens\settings.json`.

## Publish a single .exe

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

The executable is at `publish/BlackScreens.exe` (self-contained; copy that file alone). No install required. To start with Windows, put a shortcut in your Startup folder.
