# ChronoOverlay development rules

- Work on a branch and open a pull request for every change. Do not commit directly to `main`.
- Use squash merge for pull requests.
- Never merge a pull request on the user's behalf.
- Read the application version only from `Directory.Build.props`.
- Functional changes must include tests, or the pull request must explain why automated coverage is not practical.
- Application and website CI must pass before merge.
- Do not commit `bin`, `obj`, `dist`, local configuration, temporary artifacts, or generated executables.
- Release executables are produced only by GitHub Actions.
- Keep the desktop application offline: no telemetry, advertising, analytics, account system, or automatic network calls.
- Preserve the selective click-through contract: the locked visual window is click-through while the time-only hotspot accepts double-click to unlock.
