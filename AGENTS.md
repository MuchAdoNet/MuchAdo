# Agent Instructions

When bumping the package version, check git history for the last change to `<VersionPrefix>` to determine what behavior has changed since the last package version bump. Use that information to decide whether to use a minor bump or a patch bump. Update `Directory.Build.props`, but set `<PackageValidationBaselineVersion>` to the current value of `<VersionPrefix>` before bumping `<VersionPrefix>`. Then update `ReleaseNotes.md`.
