# Black Screens

System-tray app for Windows that blacks out selected monitors with solid overlays without disabling displays, so your desktop layout (and fullscreen video) stays put.

Useful when watching video on a multi-monitor setup without being distracted by the other screens.

## Download

Grab the latest self-contained `BlackScreens.exe` from [Releases](https://github.com/MathieuLalonde/black_screens/releases) — no .NET install required. Windows 10/11 only.

On first run, the icon appears in the system tray (you may need to click the `^` overflow arrow to find it).

## Usage

**Tray icon**

- **Left-click** — toggle blackout on/off
- **Right-click** — menu:
  - Check the monitors to black out (named like `DISPLAY2 (1920×1080)`; primary is labeled)
  - **Activate blackout** — show or hide overlays
  - **Exit** — quit the app

**On a blacked-out screen**

- **Left-click** — deactivate blackout
- **Right-click** — open the same tray menu

Monitor selection and activate state are saved under `%AppData%\BlackScreens\settings.json` and restored next launch. Only one instance runs at a time.

To start with Windows, put a shortcut to `BlackScreens.exe` in your Startup folder (`Win+R` → `shell:startup`).

## Build from source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or newer that can target `net8.0-windows`).

```bash
dotnet run
```

Publish a self-contained single-file exe:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

Output: `publish/BlackScreens.exe`.
