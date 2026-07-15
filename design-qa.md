# ChronoOverlay Aurora Control Dock — Design QA

The earlier GitHub Pages QA report is preserved at `docs/website-design-qa.md`.

- Source visual truth: [`docs/design/aurora-control-dock-reference.png`](docs/design/aurora-control-dock-reference.png)
- Implementation screenshot: [`docs/design/aurora-control-dock-implementation-150.png`](docs/design/aurora-control-dock-implementation-150.png)
- Full-view comparison: [`docs/design/aurora-control-dock-comparison-150.png`](docs/design/aurora-control-dock-comparison-150.png)
- Keyboard-focus evidence: [`docs/design/aurora-control-dock-focus-150.png`](docs/design/aurora-control-dock-focus-150.png)
- Dynamic anchor evidence: [`docs/design/aurora-control-dock-anchor-results.txt`](docs/design/aurora-control-dock-anchor-results.txt)
- Viewport/state: Windows 11, 150% DPI, unlocked, Major Mono Display, 72px time, 31px date, 0% background concentration.
- Comparison normalization: the source dock was cropped from the selected concept and normalized to `630×252`; the WPF dock was captured with `PrintWindow` at native 150% DPI as `630×249`.

## Findings

No actionable P0, P1, or P2 differences remain.

- [P3] Native text rasterization is slightly heavier than the generated reference.
  - Location: compact labels and the selected-font preview.
  - Evidence: WPF/DirectWrite renders Segoe UI Variable and the embedded Major Mono Display, while the reference is a generated raster.
  - Decision: accepted; font hierarchy, baseline alignment, fit, and native sharpness remain correct.
- [P3] Runtime state intentionally differs from the static mock where the mock is internally inconsistent.
  - Location: background slider, hue thumb, and auto-start toggle.
  - Evidence: the mock shows a `0%` label with a mid-track opacity thumb; the implementation truthfully places `0%` at the start and reflects the saved hue and auto-start settings.
  - Decision: accepted; geometry matches the target without presenting false state.

## Required Fidelity Surfaces

- Fonts and typography: passed. Major Mono Display, compact numeric values, Chinese labels, weights, baselines, and truncation remain readable without clipping.
- Spacing and layout rhythm: passed. The visible dock is `420×166` DIP. Content is arranged by a full-size grid, while the 1 DIP outline is a non-layout overlay, so the content receives the full intended size.
- Colors and visual tokens: passed. Midnight surface, blue-violet edge, muted tracks, purple-blue active tracks, rainbow hue rail, circular swatches, and coral-violet lock action match the selected direction.
- Image quality and asset fidelity: passed. The dock has no raster UI assets. Chevron and lock glyphs use the Windows-provided Segoe MDL2 Assets font and remain aligned with the source.
- Copy and content: passed. Every existing setting is present, the selected target's hierarchy is preserved, and no unrelated feature was introduced.
- Accessibility and interaction states: passed for the implemented visual states. Black and white swatches have explicit automation names. Both custom slider templates expose a high-contrast keyboard focus ring, shown in the committed focus capture.

## Comparison History

### Iteration 1

- P1: the font column was too wide and the time column too narrow.
- P2: slider thumbs, opacity dial, swatches, toggle, and lock action were oversized.
- Fix: remapped the first row to `184 / 1 / 110 / 1 / 96` DIP and reduced control geometry to the normalized target measurements.

### Iteration 2

- P2: time/date values were right-aligned instead of sitting beside their labels.
- P2: the opacity dial and color group were shifted right; the footer status was not centered.
- Fix: switched values to inline label groups, shifted the dial and color controls to measured positions, and centered the footer status across the dock.

### Review remediation pass

- P1: continuous time/date resizing changed the `SizeToContent` window bounds and made the dock drift or repeatedly re-evaluate above/below placement.
- Fix: capture the visible dock's physical top-right coordinate before the bound clock property updates, coalesce render work, restore that coordinate after layout, and persist only the corrected final geometry through the debounced writer.
- Evidence: six time-size transitions and five date-size transitions, including minimum and maximum values, all recorded `delta=0,0` physical pixels.
- P1: Segoe Fluent Icons is not included by default on Windows 10.
- Fix: changed E70D ChevronDown and E72E Lock to Segoe MDL2 Assets, which ships with supported Windows versions.
- P1: a `420×166` content grid was nested inside a bordered `420×166` container, leaving only `418×164` DIP of layout space.
- Fix: changed the visible panel to a `420×166` grid with an independent background surface and non-layout outline overlay.
- P2: visual QA evidence was local-only and color swatches lacked accessible names.
- Fix: committed normalized reference, implementation, comparison, focus-state, and anchor evidence; added automation names and slider focus visuals.

### Final pass

- Re-captured the production WPF surface with embedded fonts at native 150% DPI.
- Opened the reference and implementation together in the committed comparison image and inspected typography, grid geometry, dividers, radii, outline, controls, icon alignment, palette, and copy.
- No P0/P1/P2 visual mismatch remains.

## Interaction Verification

- The isolated WPF run exercised time sizes `32, 72, 128, 72, 32, 128` and date sizes `16, 31, 64, 24, 64`; the dock top-right coordinate remained exactly fixed for every transition.
- Existing bindings remain active for font, time size, date size, opacity, hue, black, white, auto-start, and lock.
- The focused-slider capture confirms the custom template exposes a visible keyboard focus outline.
- Selective click-through, time-only hotspot, clock anchoring during lock transitions, panel flipping, tray behavior, and persistence remain intact.
- Windows 10 glyph presence and full keyboard traversal remain explicit RC manual-test items because the local visual run used Windows 11.

## Follow-up Polish

- Recheck the edge glow against a very bright wallpaper at 100% and 200% DPI using the latest PR RC artifact.
- Treat remaining DirectWrite hinting differences as native-platform rendering rather than design drift.

final result: passed
