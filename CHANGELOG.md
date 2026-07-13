# Changelog

All notable changes are documented here. ChronoOverlay follows semantic versioning.

## [0.1.1] - 2026-07-13

### Fixed

- Replaced the incomplete self-contained Release asset, which omitted required WPF native libraries and crashed at startup.
- Added a CI launch smoke test for the exact executable uploaded as the PR RC and GitHub Release asset.

### Changed

- Releases are now framework-dependent single-file executables that require the x64 .NET 8 Desktop Runtime, substantially reducing download size.
- The website and documentation now state the runtime prerequisite and link to Microsoft's official download page.

## [0.1.0] - 2026-07-13

### Added

- Minimal always-on-top WPF clock with right-aligned time and Chinese date.
- Live font, hue, black/white color, and background concentration controls.
- Drag threshold, lock state, and time-only double-click unlock hotspot.
- System tray, current-user auto-start, single-instance wake-up, and persistent settings.
- Multi-monitor/DPI placement recovery and atomic configuration storage.
- Responsive Product Design-based GitHub Pages site.
- Windows CI, versioned GitHub Release, and Pages deployment workflows.
