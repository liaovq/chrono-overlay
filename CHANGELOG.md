# Changelog

All notable changes are documented here. ChronoOverlay follows semantic versioning.

## [0.1.3] - 2026-07-15

### Added

- Added Major Mono Display and VT323 as distinctive geometric and pixel-style clock font options.
- Bundled both new fonts with complete OFL 1.1 license texts and pinned source provenance.
- Refined the unlocked controls into a narrower bright-silver frosted panel with aligned labels, values, color controls, and matching buttons.

## [0.1.2] - 2026-07-14

### Added

- Added instant, persistent clock-font switching between the system monospace fallback, JetBrains Mono, IBM Plex Mono, and Space Mono.
- Bundled the three third-party fonts with their OFL 1.1 license texts and pinned source provenance.
- Added an offline, single-instance “第三方许可” window reachable from the tray, with complete embedded license text and copyable content.

### Fixed

- Restored the intended selective input behavior: locked background/date regions click through while only the time rectangle intercepts clicks and accepts double-click unlock.
- Automatically places the unlocked control panel above the clock when it would be clipped by the current monitor's lower work-area edge, while preserving the clock's visual anchor.

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
