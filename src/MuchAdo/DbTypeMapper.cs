using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace MuchAdo;

/// <summary>
/// Maps from data record values to an instance of the specified type.
/// </summary>
public abstract class DbTypeMapper
{
	/// <summary>
	/// The type to which the data record values are mapped.
	/// </summary>
	public abstract Type Type { get; }

	/// <summary>
	/// The number of fields used by the mapper, or null if the mapper can handle any number of fields.
	/// </summary>
	public abstract int? FieldCount { get; }
}

/// <summary>
/// Maps data record values to an instance of the specified type.
/// </summary>
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Same name.")]
public abstract class DbTypeMapper<T> : DbTypeMapper
{
	/// <inheritdoc />
	public override Type Type => typeof(T);

	/// <summary>
	/// Maps the data record values to an instance of the specified type.
	/// </summary>
	public T Map(IDataRecord record, DbRecordState? state) =>
		MapCore(record, index: 0, count: (record ?? throw new ArgumentNullException(nameof(record))).FieldCount, state);

	/// <summary>
	/// Maps the data record value to an instance of the specified type.
	/// </summary>
	public T Map(IDataRecord record, int index, DbRecordState? state)
	{
		var fieldCount = (record ?? throw new ArgumentNullException(nameof(record))).FieldCount;
		if (index < 0 || index >= fieldCount)
			throw new ArgumentException($"Index {index} is out of range for {fieldCount} fields.");
		return MapCore(record, index, count: 1, state);
	}

	/// <summary>
	/// Maps the data record values to an instance of the specified type.
	/// </summary>
	public T Map(IDataRecord record, int index, int count, DbRecordState? state)
	{
		var fieldCount = (record ?? throw new ArgumentNullException(nameof(record))).FieldCount;
		if (index < 0 || count < 0 || index > fieldCount - count)
			throw new ArgumentException($"Index {index} and count {count} are out of range for {fieldCount} fields.");
		return MapCore(record, index, count, state);
	}

	/// <summary>
	/// Maps the data record values to an instance of the specified type.
	/// </summary>
	protected abstract T MapCore(IDataRecord record, int index, int count, DbRecordState? state);
}
