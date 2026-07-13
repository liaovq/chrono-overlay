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
| `windowX`, `windowY` | WPF device-independent window coordinates |
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
