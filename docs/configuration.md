# Configuration

ChronoOverlay stores user settings at:

```text
%LOCALAPPDATA%\ChronoOverlay\config.json
```

## Schema

| Field | Purpose |
| --- | --- |
| `schemaVersion` | Configuration schema revision |
| `appVersion` | Application version that last saved the file |
| `windowX`, `windowY` | Legacy schema 1 WPF coordinates retained for downgrade compatibility |
| `windowPhysicalX`, `windowPhysicalY` | Schema 2 native window origin in physical desktop pixels |
| `windowPhysicalWidth`, `windowPhysicalHeight` | Schema 2 native window size at capture time |
| `clockAnchorOffsetX`, `clockAnchorOffsetY` | Clock surface top-right anchor relative to the saved monitor work area, in physical pixels |
| `monitorDeviceName` | Last Windows monitor device identifier |
| `savedDpi` | DPI used when placement was saved |
| `timeFontSize` | Time font size, clamped to 32–128 |
| `dateFontSize` | Date font size, clamped to 16–64 |
| `textColor` | Normalized six-digit RGB hex color |
| `colorMode` | `Hue`, `Black`, or `White` |
| `hue` | Hue angle normalized to 0–360 |
| `backgroundOpacity` | Black background opacity from 0 to 1 |
| `isLocked` | Selective click-through state |
| `autoStartEnabled` | Current-user auto-start preference |

## Reliability

- Slider changes update the UI immediately and save after a 400ms debounce.
- Drag completion, lock changes, color mode changes, auto-start changes, and shutdown save immediately.
- Writes use a temporary file followed by an atomic replace/move.
- Missing fields use model defaults.
- Out-of-range values are clamped.
- Unknown fields are ignored for forward compatibility.
- Invalid JSON is renamed to `config.corrupt-yyyyMMdd-HHmmss.json`.
- If storage is unavailable, the application continues with in-memory settings and shows a concise warning.
- Schema 1 placement is migrated using its saved DPI, validated against physical monitor work areas, and safely falls back to the primary monitor when it cannot be restored.
- `appVersion` is always replaced with the running assembly version during load and save; `Directory.Build.props` remains the only release version source.
