# Google Desktop Revived/Rebuilt

A Windows desktop sidebar inspired by classic desktop gadgets, with an Aero-style glass look. Built with WPF and .NET 8.

## Features

- Aero blue, dark, and custom color themes; adjustable transparency, custom gradient backgrounds, presets, and optional light streaks.
- Clock, weather, photo frame, RSS/Atom news feed, calendar, sticky notes, calculator, and quick search.
- Optional widgets are checked against the available screen height so a selection that would overflow the sidebar is rejected.
- Sticky notes can be detached and moved around the desktop.
- Optional startup registration, a double-Ctrl quick-search shortcut toggle, and a reset option for this Windows account's app data.
- The sidebar can stay on the desktop behind other windows or be configured to stay on top.

## Run from Visual Studio

1. Install Visual Studio 2022 or newer with the **.NET desktop development** workload and a .NET 8 SDK.
2. Open `Google Desktop Revived.slnx`.
3. Build and run the `Google Desktop Revived` project.

## Publish a single-file app

The repository includes a `Windows-x64` publish profile configured for a self-contained, single-file Windows x64 release. From the repository root, run:

```powershell
dotnet publish ".\Google Desktop Revived\Google Desktop Revived.csproj" -p:PublishProfile=Windows-x64
```

The executable is written to:

```text
Google Desktop Revived\bin\Release\net8.0-windows\win-x64\single-file\Google.Desktop.Rebuilt.exe
```

The published EXE includes the .NET runtime and can run on a compatible Windows x64 PC without a separate .NET installation. 

## Local data and privacy

The app stores settings, calendar events, and sticky notes for the current Windows account under `%AppData%\GoogleDesktopRemake`. This data is outside the project and is not included when you publish the source repository. On first launch for an account, the app offers setup for preferences such as theme, weather location, and photo folder. The **Reset all app data** setting removes that account's saved settings, events, and notes. Startup registration is a separate Windows setting and is not removed by the reset.

Weather and location search use Open-Meteo (no API key). News headlines are fetched from the RSS/Atom feeds configured in Settings. Quick Search can open web searches when requested.

## Usage notes

- By default, the sidebar stays on the desktop behind other windows. Enable **Keep on top** in Settings to keep it above them.
- **Double-tap Ctrl** opens Quick Search. Turn it off under Settings > Sidebar > Behavior if it interferes with games.
- Click a calendar date to view or add events.
- Drag the red pin on a sticky note to detach it. Drag the note back over the sidebar to return it, or use its × button to delete it.
- Desktop icon repositioning requires Windows **Auto arrange icons** to be off (right-click the desktop > View).
