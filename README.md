# Google Desktop Remake (WPF, .NET 8)

Open `Google Desktop Revived.slnx` in Visual Studio and press F5 to run the app.

For a self-contained x64 single-file Release build, publish with the `Windows-x64` profile. The output is
`Google Desktop Revived\bin\Release\net8.0-windows\win-x64\single-file\Google Desktop Revived.exe`.
Copy that executable to another Windows x64 PC and run it; no .NET installation is required. Each Windows account stores its own preferences under `%AppData%\GoogleDesktopRemake`, outside the executable. On the first run for a new account, choose a theme/color, weather location, and photo folder. Existing settings are preserved and do not trigger setup. Settings also lets each account add or remove the app from its own startup programs.

* Sidebar lives on the desktop below other windows by default; enable **Keep on top** in Settings to keep it above them.
* Settings include Aero/Dark/custom themes, background opacity in the Background tab (previewed over sample wallpaper), custom multi-stop gradients, direction/position controls, background presets, and optional theme-matched Windows 7 light streaks; they also include analog/flip/segment clocks, calendar events, sticky notes, calculator, photo/news visibility, photo folder, weather, and multiple RSS/Atom feeds. Gadget settings check the current screen's measured space and reject additions that would overflow the sidebar. A second launch while the app is running displays an already-running message. **Reset all app data** removes saved settings, events, and notes for this account, then closes the app so next launch starts setup again; it leaves startup registration unchanged.
* The news feed has smooth scroll arrows when more stories are available.
* Drag the red pin on a sticky note to detach it as a floating note. Drag it back over the sidebar to return it, or use × to delete it; deleting the last detached note restores a blank note in the sidebar.
* Click a calendar date to view or add events. Data is saved under %AppData%\GoogleDesktopRemake.
* Desktop files/folders under the sidebar are moved to its left (and restored on exit). Needs "Auto arrange icons" OFF
  (right-click desktop > View).
* **Double-tap Ctrl** anywhere = Quick Search Box (apps, files, "Search web for ..."). Disable this shortcut in Settings > Behavior if it interferes with games.
* The blue Aero glass tint remains darker on the left and lighter/more transparent on the right.

Weather + city search: Open-Meteo (free, no API key).
