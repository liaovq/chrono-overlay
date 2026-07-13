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
- [ ] A second launch wakes/unlocks the existing instance and adds no second tray icon.
- [ ] Exit from tray removes the icon and process.

## Persistence and auto-start

- [ ] Restart restores position, sizes, color mode/color, hue, opacity, and lock state.
- [ ] Current-user auto-start can be enabled and disabled without elevation.
- [ ] Auto-start launches without stealing focus and restores locked state.
- [ ] Moving the EXE repairs the enabled auto-start path on the next launch.
- [ ] Corrupt JSON is backed up and defaults are loaded.
- [ ] A denied configuration directory shows a warning but does not exit.

## Displays and resources

- [ ] Window moves between monitors with different DPI without blur or jumps.
- [ ] Negative coordinates restore correctly.
- [ ] Disconnecting the saved monitor returns the window to a visible primary work area.
- [ ] Resolution and taskbar work-area changes keep at least 32px visible.
- [ ] Sleep/resume restores topmost behavior.
- [ ] Idle CPU is normally below 1%; memory is normally below 100MB.
- [ ] Eight-hour run shows no sustained memory growth or repeated disk writes.
