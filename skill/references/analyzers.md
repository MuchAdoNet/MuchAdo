# Analyzers

The [MuchAdo.Analyzers](https://www.nuget.org/packages/MuchAdo.Analyzers) NuGet package provides analyzer warnings for potentially incorrect uses of MuchAdo.

## MUCH0001

This warning is issued when `Command` is called with an interpolated string. Normally `CommandFormat` is used with interpolated strings; this analyzer catches potentially accidental calls to `Command` when `CommandFormat` was intended.

If you actually do want to use an interpolated string with raw SQL, wrap the interpolated string in a call to `Sql.Raw`, or assign the interpolated string to a string variable and call `Command` on that.
