# ChronoOverlay Aurora Control Dock — Design QA

The earlier GitHub Pages QA report is preserved at `docs/website-design-qa.md`.

- Source visual truth: `C:\Users\Liao\.codex\generated_images\019f5b03-3f59-7080-90bb-c031510529c0\exec-8ec898d2-12df-4db6-a17d-66095a9292d8.png`
- Implementation screenshot: `artifacts/aurora-dock-qa-final.png`
- Full-view comparison: `artifacts/aurora-dock-comparison-final.png`
- Viewport/state: Windows 11, 150% DPI, unlocked, Major Mono Display, 72px time, 31px date, 0% background concentration.
- Comparison normalization: the source dock was cropped to `1110×444` and scaled to `630×252`; the WPF dock was captured at native DPI as `630×249`.

## Findings

No actionable P0, P1, or P2 differences remain.

- [P3] Native text rasterization is slightly heavier than the generated reference.
  - Location: compact labels and the selected font preview.
  - Evidence: Segoe UI Variable and the embedded Major Mono Display are rendered by WPF/DirectWrite, while the reference is a generated raster.
  - Decision: accepted; font families, hierarchy, baseline alignment, and fit match the target, and native rendering remains sharper in the real app.
- [P3] Control states differ from the static mock where semantics require it.
  - Location: background slider, hue thumb, and auto-start toggle.
  - Evidence: the mock shows a 0% label with a mid-track opacity thumb, while the implementation correctly places 0% at the start; hue and auto-start reflect the saved setting instead of a decorative state.
  - Decision: accepted; layout geometry matches and runtime behavior remains truthful.

## Required Fidelity Surfaces

- Fonts and typography: passed. Hierarchy, compact numeric values, selected Major Mono Display preview, and Chinese labels are aligned and readable without clipping.
- Spacing and layout rhythm: passed. The visible dock is `420×166` DIP; normalized output is within 3 physical pixels of the reference height. Both row dividers, both top column dividers, control baselines, and footer center align with the target.
- Colors and visual tokens: passed. Midnight surface, blue-violet edge, muted gray tracks, purple-blue active tracks, rainbow hue rail, circular black/white swatches, and coral-violet lock action match the target palette.
- Image quality and asset fidelity: passed. The dock contains no raster assets requiring replacement; the lock mark uses the native Segoe Fluent Icons library and remains crisp at 150% DPI.
- Copy and content: passed. Every existing setting is present, “调整后自动保存” matches the selected target, and no unrelated feature was added.

## Comparison History

### Iteration 1

- P1: the font column was too wide and the time column too narrow.
- P2: slider thumbs, opacity dial, swatches, toggle, and lock action were oversized.
- Fix: remapped the first row to `184 / 1 / 110 / 1 / 96` DIP and reduced control geometry to the normalized target measurements.

### Iteration 2

- P2: time/date values were right-aligned instead of sitting beside their labels.
- P2: the opacity dial and color group were shifted right; the footer status was not centered on the dock.
- Fix: switched values to inline label groups, shifted the dial and color controls to the measured target positions, and centered the footer status across the full dock.

### Final pass

- Fixed the selected-font preview padding and optical size so `MAJOR MONO DISPLAY · 几何实` fits the same field width as the reference.
- Re-captured the WPF window at native 150% DPI and compared both dock regions in one normalized image.
- No P0/P1/P2 mismatch remains.

## Interaction Verification

- Existing bindings remain active for font, time size, date size, opacity, hue, black, white, auto-start, and lock.
- Keyboard focus visuals remain present for the dropdown, swatches, toggle, and lock action.
- Selective click-through, time-only hotspot, clock anchoring, panel flipping, tray behavior, and persistence code were not replaced.

## Follow-up Polish

- Recheck the edge glow against a very bright wallpaper and at 100%/200% DPI using the PR RC artifact.
- Treat any remaining DirectWrite hinting difference as native-platform rendering rather than design drift.

final result: passed
