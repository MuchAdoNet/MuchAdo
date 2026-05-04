# Agent Instructions

## Bumping the Package Version

When bumping the package version, check git history for the last change to `<VersionPrefix>` to determine what behavior has changed since the last package version bump. Use that information to decide whether to use a major, minor, or patch bump.

Update `Directory.Build.props`, but set `<PackageValidationBaselineVersion>` to the current value of `<VersionPrefix>` before bumping `<VersionPrefix>`. Then update `ReleaseNotes.md`.

If explicitly requested, treat a breaking change as a minor package version bump, set `<PackageValidationBaselineVersion>` to the same value as the new `<VersionPrefix>`, and make any necessary acknowledgments in `ReleaseNotes.md`.

## C# Code Style

For expression-bodied members, keep the lambda arrow token (`=>`) on the same line as the member declaration. If the expression body needs to wrap, put the wrapped expression on the following line.

Place private fields at the bottom of the class.
