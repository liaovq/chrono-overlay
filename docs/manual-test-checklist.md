# Manual Windows test checklist

> 人工验收应优先下载最新成功 PR workflow 中的 RC artifact，并记录 workflow run ID、提交 SHA 和文件名。

Record Windows version, monitor topology, DPI, executable path, and result for each run.

## Runtime and package

- [ ] Record the RC executable size; it is below 10 MiB and no WPF/.NET DLLs are distributed beside it.
- [ ] On a clean Windows Sandbox without .NET 8 Desktop Runtime, launch the RC and confirm Windows presents the missing-runtime guidance instead of silently crashing.
- [ ] Install the x64 [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0), relaunch the same RC, and confirm the clock and tray icon appear.
- [ ] Confirm the embedded PerMonitorV2 manifest still passes `scripts/Test-EmbeddedManifest.ps1`.
- [ ] In File Explorer large, medium, and small icon views, confirm the EXE uses the warm sun-and-horizon artwork with no white square, checkerboard, clipped edge, or legacy `C` icon.
- [ ] At 100%, 150%, and 200% display scaling, confirm the notification-area icon remains recognizable and uses the same artwork.
- [ ] Check the notification-area icon on both light and dark Windows taskbar themes; its silhouette and warm color layers remain visible.
- [ ] Disconnect the network, open the tray menu, and confirm “第三方许可” opens a local WPF window without launching a browser.
- [ ] Confirm the license window lists JetBrains Mono, IBM Plex Mono, and Space Mono with copyright holders and upstream URLs.
- [ ] Search/select/copy text from the read-only license area and confirm each font section contains the complete “SIL OPEN FONT LICENSE Version 1.1” text.
- [ ] Click “第三方许可” repeatedly and confirm only one license window exists; closing it leaves the clock and tray process running.
- [ ] Repeat while the clock is locked and unlocked; opening/closing the license window does not change lock state, selective click-through, position, or topmost behavior.

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
- [ ] Drag the unlocked clock close to the bottom edge; the entire control panel flips above the clock and the lock button remains reachable.
- [ ] Drag it back near the top edge; the panel returns below the clock without moving the clock surface's top-right anchor.
- [ ] Repeat panel placement on a left-side negative-coordinate monitor and at 100%, 150%, and 200% DPI; the panel chooses the current monitor work area and does not cross the taskbar.
- [ ] Select 系统等宽, JetBrains Mono, IBM Plex Mono, and Space Mono; each option changes the clock immediately, keeps the digits tabular, and still renders the Chinese date correctly through font fallback.
- [ ] Restart after selecting each bundled font and confirm the same font is restored.

## Locked selection and input

- [ ] Control panel collapses and clock background shrinks around content.
- [ ] Place Notepad or Paint underneath and confirm background, date, and empty regions receive clicks in that underlying application.
- [ ] Over the same underlying target, confirm a single click on the time digits is intercepted and does not reach the application below.
- [ ] Double-clicking time digits unlocks and restores the panel.
- [ ] Repeat the previous three checks with background concentration at 0%, 60%, and 100%; input behavior does not depend on visual opacity.
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
