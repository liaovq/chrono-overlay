# Contributing

1. Create a branch from `main`.
2. Keep the version in `Directory.Build.props`; bump it for release-impacting changes.
3. Add or update tests for behavioral changes.
4. Run the .NET and website checks documented in `README.md`.
5. Open a pull request using the repository template.
6. Do not commit generated EXEs, application configuration, or build directories.

Pull requests are squash-merged after Windows and website CI pass.
