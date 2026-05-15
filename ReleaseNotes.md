# Release Notes

## 1.2.2

* Fix pooled connector transaction disposal after attaching a transaction with `noDispose: true` so the flag doesn't persist across pool reuse.
* Stabilize repeated enumeration and rendering of SQL parameter sources created from deferred enumerables.
* Fix `ValueTuple` element mapping so null values for non-nullable tuple elements fail instead of being silently mapped to default values.

## 1.2.1

* Fix construction of DTO mappers for self-referential and mutually referential DTO types, allowing selected non-circular properties to map successfully and reporting selected circular properties with a meaningful exception.
* Stabilize repeated rendering of SQL sources created from deferred enumerables.
* Fix DTO mapping across multiple result sets with different field orders.
* Fix nullable multi-field mapping for nullable value tuples and all-null rows.
* Fix field-count validation for variable-length `ValueTuple` mappings.
* Fix double disposal of pooled connectors so disposing twice doesn't return the same connector to the pool twice.
* Validate unnamed parameter strategy strings so null and empty values fail at configuration time instead of producing invalid or misleading SQL.

## 1.2.0

* Change platform-specific connector constructors to require their provider-specific connection types instead of `DbConnection`. (This is a breaking change, but we're sticking with a minor version bump due to limited impact.)

## 1.1.0

* Change type of `Sql.Empty` from `SqlParamSource` to `SqlSource` so that it works better with `var`. (This is, strictly speaking, a breaking change, but there is too little actual impact to bump the major version.)
* Introduce `Sql.NoParams` for an empty `SqlParamSource`.

## 1.0.1

* Add .NET 10 targets.
* Fix SQL Server null parameter handling.

## 1.0.0

* Initial release. Adapted from [Faithlife.Data](https://github.com/Faithlife/FaithlifeData/) and [Faithlife.Reflection](https://github.com/Faithlife/FaithlifeReflection/).
