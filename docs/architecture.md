# Architecture

## Desktop application

ChronoOverlay is a .NET 8 WPF application targeting `win-x64`.

- `ClockWindow` renders the clock, date, background, and unlocked control panel.
- `LockedHotspotWindow` is a transparent, topmost, non-activating window that covers only the measured time text rectangle.
- `ClockViewModel` owns display state and settings bindings.
- `ClockService` aligns updates to system second boundaries.
- `SettingsService` validates, debounces, and atomically persists configuration.
- `WindowStyleService` applies Win32 extended styles for tool-window behavior, click-through, no-activate, and topmost placement.
- `DisplayPlacementService` converts WPF DIP placement to screen-aware physical coordinates and recovers off-screen windows.
- `TrayIconService`, `AutoStartService`, and `SingleInstanceService` isolate Windows lifecycle integrations.

## Selective click-through

The locked state deliberately uses two native windows:

1. `ClockWindow` adds `WS_EX_TRANSPARENT | WS_EX_NOACTIVATE`, so the visual clock surface does not intercept mouse input.
2. `LockedHotspotWindow` remains input-enabled and receives only the time text rectangle. It uses `WS_EX_NOACTIVATE` and accepts a double-click to unlock.

The hotspot is synchronized after layout, position, size, DPI, monitor, and lock-state changes. This avoids a global mouse hook and keeps date/background clicks available to the application underneath.

## Website

The static React/Vite site lives in `site/`. `vite.config.mjs` reads `VersionPrefix` directly from the root `Directory.Build.props`, then injects the version and matching GitHub Release asset URL at build time. The site has no backend, analytics, cookies, or runtime API calls.

## Version source

`Directory.Build.props` is authoritative. The .NET assembly, release tag, release filename, website version, website download URL, and workflows all derive from `VersionPrefix`.
