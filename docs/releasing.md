# Releasing

## Versioning

Update `<VersionPrefix>` in `Directory.Build.props` for every application, website, workflow, or release-content change. Documentation-only changes do not require a version bump.

The version produces:

- Tag: `v{version}`
- Release title: `ChronoOverlay v{version}`
- Asset: `ChronoOverlay-v{version}-win-x64.exe`
- Website version and download URL

## Pull request validation

`.github/workflows/pr.yml` runs on `windows-latest` and validates:

1. version-bump rules against `origin/main`;
2. .NET restore, Release build, and unit tests;
3. self-contained single-file Windows publish;
4. deterministic website dependency installation and production build;
5. website/Release URL version consistency.

## Main publication

`.github/workflows/release.yml` runs after a push to `main`.

- Release-impacting changes require a new version and fail clearly if `v{version}` already exists.
- The workflow builds/tests/publishes on Windows, creates the tag and GitHub Release, and uploads the versioned EXE.
- Documentation-only changes skip duplicate Release creation.
- The website is always built and deployed through GitHub Pages Actions.

The repository must have **Settings → Pages → Source → GitHub Actions** selected. The workflow attempts no branch-based Pages publication.

## Merge policy

- Use a feature branch.
- Open a Draft PR while implementation or Windows manual QA remains.
- Wait for all checks.
- Squash merge into `main`.
- Never manually upload a Release executable built on a developer machine.
