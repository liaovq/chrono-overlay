# Architecture

## Desktop application

ChronoOverlay is a .NET 8 WPF application targeting `win-x64`. Releases are framework-dependent single-file executables and require the x64 .NET 8 Desktop Runtime on the target computer. Keeping the shared WPF and .NET frameworks outside the application reduces the download from roughly 146 MiB to a small application-only executable.

- `ClockWindow` renders the clock, date, background, and unlocked control panel.
- `LockedHotspotWindow` is a transparent, topmost, non-activating window that covers only the measured time text rectangle.
- `ClockViewModel` owns display state and settings bindings.
- `ClockFontCatalog` resolves the system fallback and the five OFL-licensed fonts packaged as WPF resources.
- `ClockService` aligns updates to system second boundaries.
- `SettingsService` validates, debounces, and atomically persists configuration.
- `WindowStyleService` applies Win32 extended styles for tool-window behavior, click-through, no-activate, and topmost placement.
- `DisplayPlacementService` uses `GetWindowRect`, `MonitorFromWindow`, `GetMonitorInfo`, and per-monitor DPI to persist and restore one explicit physical-pixel coordinate model.
- `TrayIconService`, `AutoStartService`, and `SingleInstanceService` isolate Windows lifecycle integrations.
- `ThirdPartyLicenseContentService` reads the notice and five complete OFL texts from embedded WPF resources; `ThirdPartyLicenseWindowService` owns a single reusable local license-window instance without touching clock state.

## Selective click-through

The locked state deliberately uses two native windows:

1. `ClockWindow` adds `WS_EX_TRANSPARENT | WS_EX_NOACTIVATE`, so the visual clock surface does not intercept mouse input.
2. `LockedHotspotWindow` remains input-enabled and receives only the time text rectangle. It uses `WS_EX_NOACTIVATE` and accepts a double-click to unlock.

The hotspot is synchronized after layout, position, size, DPI, monitor, and lock-state changes. This avoids a global mouse hook and keeps date/background clicks available to the application underneath.

The hotspot window deliberately renders a visually imperceptible but non-zero-alpha surface. Its native tool-window setup explicitly removes `WS_EX_TRANSPARENT`; otherwise a fully transparent layered WPF window can be omitted from Windows hit testing and accidentally degrade the behavior to whole-window click-through.

Lock and unlock transitions capture the physical top-right anchor of `ClockSurface`, change the `SizeToContent` layout, then compensate the native window position before the hotspot or settings are synchronized. This keeps the visible clock fixed even when the control panel is wider than the clock.

While unlocked, `ControlPanelPlacementMath` compares the measured physical panel height with the active monitor's physical work area. It keeps the panel below when possible, flips it above when the lower edge would be clipped, and preserves the clock anchor during the row swap.

## Startup and single instance

`StartupModeParser` separates interactive launch from `--autostart`. Auto-start uses WPF's one-time non-activating show behavior, while later tray or second-instance wake actions can still activate the unlocked window normally.

`SingleInstanceService` creates the named auto-reset wake event before acquiring the named mutex. A second process can therefore signal during primary startup; the signaled state remains pending until the primary registers its wait callback.

## Website

The static React/Vite site lives in `site/`. `vite.config.mjs` reads `VersionPrefix` directly from the root `Directory.Build.props`, then injects the version and matching GitHub Release asset URL at build time. The site has no backend, analytics, cookies, or runtime API calls.

## Version source

`Directory.Build.props` is authoritative. The .NET assembly, release tag, release filename, website version, website download URL, and workflows all derive from `VersionPrefix`.
