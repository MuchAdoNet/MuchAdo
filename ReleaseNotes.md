# Release Notes

## 1.1.0

* Change type of `Sql.Empty` from `SqlParamSource` to `SqlSource` so that it works better with `var`. (This is, strictly speaking, a breaking change, but there is too little actual impact to bump the major version. )
* Introduce `Sql.NoParams` for an empty `SqlParamSource`.

## 1.0.1

* Add .NET 10 targets.
* Fix SQL Server null parameter handling.

## 1.0.0

* Initial release. Adapted from [Faithlife.Data](https://github.com/Faithlife/FaithlifeData/) and [Faithlife.Reflection](https://github.com/Faithlife/FaithlifeReflection/).
