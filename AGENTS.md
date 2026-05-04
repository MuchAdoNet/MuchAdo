# Agent Instructions

## Bumping the Package Version

When bumping the package version, complete this history review before editing any version or release notes files:

* Find the last commit that changed `<VersionPrefix>`.
* Enumerate every commit since that commit, for example with `git log --reverse --oneline <version-commit>..HEAD`.
* Enumerate every changed file since that commit, for example with `git diff --stat <version-commit>..HEAD` and `git diff --name-status <version-commit>..HEAD`.
* Inspect the relevant diffs and tests for those commits.
* Summarize the behavior changes that have happened since the last package version bump.

Use that complete history review to decide whether to use a major, minor, or patch bump.

`ReleaseNotes.md` must account for every release-relevant behavior change since the previous `<VersionPrefix>` change, not just the change from the current branch. Do not omit fixes that already landed on the base branch after the previous package version bump.

Update `Directory.Build.props`, but set `<PackageValidationBaselineVersion>` to the current value of `<VersionPrefix>` before bumping `<VersionPrefix>`. Then update `ReleaseNotes.md`.

If explicitly requested, treat a breaking change as a minor package version bump, set `<PackageValidationBaselineVersion>` to the same value as the new `<VersionPrefix>`, and make any necessary acknowledgments in `ReleaseNotes.md`.

## C# Code Style

For expression-bodied members, keep the lambda arrow token (`=>`) on the same line as the member declaration. If the expression body needs to wrap, put the wrapped expression on the following line.

Place private fields at the bottom of the class.
