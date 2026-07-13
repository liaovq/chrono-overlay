# Manual Windows test checklist

Record Windows version, monitor topology, DPI, executable path, and result for each run.

## Clock and controls

- [ ] Time is `HH:mm:ss`, updates on second boundaries, and does not shift horizontally.
- [ ] Date is `yyyy.MM.dd 星期X` with the correct Chinese weekday.
- [ ] Time and date remain strictly right-aligned at minimum and maximum sizes.
- [ ] Time/date size sliders, background concentration, hue, pure black, and pure white update immediately.
- [ ] Slider interaction never starts a window drag.
- [ ] Non-control areas drag only after the Windows drag threshold.
- [ ] Double-clicking time while unlocked locks without starting a drag.
- [ ] At 32px, 72px, and 128px time sizes, place a visible reference at the clock surface's top-right corner, lock/unlock 10 times, and confirm the corner does not visibly move.
- [ ] Repeat the anchor test while the control panel is wider than the clock content; no horizontal jump is visible.

## Locked selection and input

- [ ] Control panel collapses and clock background shrinks around content.
- [ ] Background, date, and empty regions click through to an application underneath.
- [ ] Time digits intercept a single click.
- [ ] Double-clicking time digits unlocks and restores the panel.
- [ ] Hotspot matches the time rectangle at 100%, 125%, 150%, and 200% DPI.
- [ ] No global mouse hook is present or running.

## Windows integration

- [ ] Main and hotspot windows are absent from taskbar and Alt+Tab.
- [ ] Clock stays above normal windows without stealing focus while locked.
- [ ] Tray double-click unlocks and shows the panel.
- [ ] Every tray menu item works and checked states stay synchronized.
- [ ] Explorer restart restores the tray icon.
- [ ] Rapidly double-click the EXE or launch it three times in succession; exactly one process and one tray icon remain, and the primary unlocks, shows the panel, and activates.
- [ ] Exit from tray removes the icon and process.

## Persistence and auto-start

- [ ] Restart restores position, sizes, color mode/color, hue, opacity, and lock state.
- [ ] Current-user auto-start can be enabled and disabled without elevation.
- [ ] With Notepad focused, run the EXE with `--autostart`; the clock appears without changing the foreground window or flashing active chrome.
- [ ] Repeat `--autostart` with a saved locked state; the first visible frame is already click-through with only the time hotspot active.
- [ ] Repeat `--autostart` with a saved unlocked state; the panel is visible without activation, then activates normally after a direct click or tray restore.
- [ ] Moving the EXE repairs the enabled auto-start path on the next launch.
- [ ] Corrupt JSON is backed up and defaults are loaded.
- [ ] A denied configuration directory shows a warning but does not exit.

## Displays and resources

- [ ] Configure two monitors at different scales (test 100%/150%, 125%/200%, and reversed primary roles), move the clock across them, restart, and confirm the same monitor and visual position are restored without blur.
- [ ] Place a secondary monitor to the left of the primary, confirm its physical coordinates are negative, restart on that display, and verify the clock remains there.
- [ ] While the clock is locked, move it between differently scaled monitors and verify the `WM_DPICHANGED` suggested rectangle is honored and the time hotspot immediately realigns.
- [ ] Disconnecting the saved monitor returns the window to a visible primary work area.
- [ ] Disconnect the saved monitor while running; the corrected primary-monitor position is immediately persisted and survives restart.
- [ ] Change resolution and move the taskbar to each screen edge; the window keeps at least 32 physical pixels visible and the corrected position survives restart.
- [ ] Sleep/resume restores topmost behavior.
- [ ] Idle CPU is normally below 1%; memory is normally below 100MB.
- [ ] Eight-hour run shows no sustained memory growth or repeated disk writes.
