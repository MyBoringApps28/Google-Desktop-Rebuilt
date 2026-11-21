# Google Desktop Revived

A Windows desktop sidebar inspired by classic desktop gadgets, with an Aero-style glass look. Built with WPF and .NET 8.

## Features

- Aero, dark, and custom themes with adjustable transparency, gradient backgrounds, presets, and optional light streaks.
- Clock, weather, photos, RSS/Atom news, calendar, sticky notes, calculator, and quick search.
- Screen-aware widget settings that prevent configurations from overflowing the sidebar.
- Detachable sticky notes, optional startup launch, a toggle for the double-Ctrl search shortcut, and an option to reset this account's app data.
- Choose whether the sidebar stays behind other windows or remains on top.

## Download and run

Download **`Google.Desktop.Rebuilt.exe`** from the latest GitHub Release. It is a self-contained, single-file app for Windows 10 or Windows 11 on 64-bit x64 PCs; no separate .NET installation is needed. Internet access is used for weather, news feeds, and web searches. The sidebar is about 200 pixels wide, and available screen height determines which widgets fit.

The GitHub **Source code (zip)** and **Source code (tar.gz)** are source archives, not app installers. Download the `.exe` release asset to run the app.

## Build and publish from source

Install Visual Studio 2022 or newer with the **.NET desktop development** workload and the .NET 8 SDK. Open `Google Desktop Revived.csproj` in Visual Studio to build and run the app.

To publish a self-contained, single-file Windows x64 release, run this from the repository root in PowerShell:

```powershell
dotnet publish ".\Google Desktop Revived.csproj" -p:PublishProfile=Windows-x64
```

The `Windows-x64` profile writes the executable to:

```text
bin\Release\net8.0-windows\win-x64\single-file\Google Desktop Revived.exe
```

## Local data and privacy

Settings, calendar events, and sticky notes are stored for the current Windows account under `%AppData%\GoogleDesktopRemake`, outside the project and source archive. First launch offers setup for preferences such as theme, weather location, and photo folder. **Reset all app data** removes that account's saved settings, events, and notes; startup registration is not removed.

Weather and location search use Open-Meteo (no API key). News headlines come from the RSS/Atom feeds configured in Settings.

## Usage notes

- By default, the sidebar stays on the desktop behind other windows. Enable **Keep on top** in Settings to keep it above them.
- **Double-tap Ctrl** opens Quick Search. Disable it in Settings > Sidebar > Behavior if it interferes with games.
- Click a calendar date to view or add events.
- Drag the red pin on a sticky note to detach it. Drag the note back over the sidebar to return it, or use its × button to delete it.
- Desktop icon repositioning requires Windows **Auto arrange icons** to be off (right-click the desktop > View).

