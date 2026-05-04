using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace MuchAdo.Mappers;

internal sealed class ValueTupleMapper<T1>(DbDataMapper dataMapper, DbTypeMapper<T1> mapper1)
	: ValueTupleMapperBase<ValueTuple<T1>>(dataMapper, [mapper1])
{
	protected override ValueTuple<T1> MapCore(IDataRecord record, int index, int count, DbConnectorRecordState? state)
	{
		Span<(int Index, int Count)> valueRanges = stackalloc (int Index, int Count)[1];
		GetValueRanges(record, index, count, valueRanges);
		return new ValueTuple<T1>(MapValue(mapper1, record, valueRanges[0], state));
	}
}

[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Same name.")]
internal sealed class ValueTupleMapper<T1, T2>(DbDataMapper dataMapper, DbTypeMapper<T1> mapper1, DbTypeMapper<T2> mapper2)
	: ValueTupleMapperBase<(T1, T2)>(dataMapper, [mapper1, mapper2])
{
	protected override (T1, T2) MapCore(IDataRecord record, int index, int count, DbConnectorRecordState? state)
	{
		Span<(int Index, int Count)> valueRanges = stackalloc (int Index, int Count)[2];
		GetValueRanges(record, index, count, valueRanges);
		return (
			MapValue(mapper1, record, valueRanges[0], state),
			MapValue(mapper2, record, valueRanges[1], state));
	}
}

[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Same name.")]
internal sealed class ValueTupleMapper<T1, T2, T3>(DbDataMapper dataMapper, DbTypeMapper<T1> mapper1, DbTypeMapper<T2> mapper2, DbTypeMapper<T3> mapper3)
	: ValueTupleMapperBase<(T1, T2, T3)>(dataMapper, [mapper1, mapper2, mapper3])
{
	protected override (T1, T2, T3) MapCore(IDataRecord record, int index, int count, DbConnectorRecordState? state)
	{
		Span<(int Index, int Count)> valueRanges = stackalloc (int Index, int Count)[3];
		GetValueRanges(record, index, count, valueRanges);
		return (
			MapValue(mapper1, record, valueRanges[0], state),
			MapValue(mapper2, record, valueRanges[1], state),
			MapValue(mapper3, record, valueRanges[2], state));
	}
}

[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Same name.")]
internal sealed class ValueTupleMapper<T1, T2, T3, T4>(DbDataMapper dataMapper, DbTypeMapper<T1> mapper1, DbTypeMapper<T2> mapper2, DbTypeMapper<T3> mapper3, DbTypeMapper<T4> mapper4)
	: ValueTupleMapperBase<(T1, T2, T3, T4)>(dataMapper, [mapper1, mapper2, mapper3, mapper4])
{
	protected override (T1, T2, T3, T4) MapCore(IDataRecord record, int index, int count, DbConnectorRecordState? state)
	{
		Span<(int Index, int Count)> valueRanges = stackalloc (int Index, int Count)[4];
		GetValueRanges(record, index, count, valueRanges);
		return (
			MapValue(mapper1, record, valueRanges[0], state),
			MapValue(mapper2, record, valueRanges[1], state),
			MapValue(mapper3, record, valueRanges[2], state),
			MapValue(mapper4, record, valueRanges[3], state));
	}
}

[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Same name.")]
internal sealed class ValueTupleMapper<T1, T2, T3, T4, T5>(DbDataMapper dataMapper, DbTypeMapper<T1> mapper1, DbTypeMapper<T2> mapper2, DbTypeMapper<T3> mapper3, DbTypeMapper<T4> mapper4, DbTypeMapper<T5> mapper5)
	: ValueTupleMapperBase<(T1, T2, T3, T4, T5)>(dataMapper, [mapper1, mapper2, mapper3, mapper4, mapper5])
{
	protected override (T1, T2, T3, T4, T5) MapCore(IDataRecord record, int index, int count, DbConnectorRecordState? state)
	{
		Span<(int Index, int Count)> valueRanges = stackalloc (int Index, int Count)[5];
		GetValueRanges(record, index, count, valueRanges);
		return (
			MapValue(mapper1, record, valueRanges[0], state),
			MapValue(mapper2, record, valueRanges[1], state),
			MapValue(mapper3, record, valueRanges[2], state),
			MapValue(mapper4, record, valueRanges[3], state),
			MapValue(mapper5, record, valueRanges[4], state));
	}
}

[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Same name.")]
internal sealed class ValueTupleMapper<T1, T2, T3, T4, T5, T6>(DbDataMapper dataMapper, DbTypeMapper<T1> mapper1, DbTypeMapper<T2> mapper2, DbTypeMapper<T3> mapper3, DbTypeMapper<T4> mapper4, DbTypeMapper<T5> mapper5, DbTypeMapper<T6> mapper6)
	: ValueTupleMapperBase<(T1, T2, T3, T4, T5, T6)>(dataMapper, [mapper1, mapper2, mapper3, mapper4, mapper5, mapper6])
{
	protected override (T1, T2, T3, T4, T5, T6) MapCore(IDataRecord record, int index, int count, DbConnectorRecordState? state)
	{
		Span<(int Index, int Count)> valueRanges = stackalloc (int Index, int Count)[6];
		GetValueRanges(record, index, count, valueRanges);
		return (
			MapValue(mapper1, record, valueRanges[0], state),
			MapValue(mapper2, record, valueRanges[1], state),
			MapValue(mapper3, record, valueRanges[2], state),
			MapValue(mapper4, record, valueRanges[3], state),
			MapValue(mapper5, record, valueRanges[4], state),
			MapValue(mapper6, record, valueRanges[5], state));
	}
}

[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Same name.")]
internal sealed class ValueTupleMapper<T1, T2, T3, T4, T5, T6, T7>(DbDataMapper dataMapper, DbTypeMapper<T1> mapper1, DbTypeMapper<T2> mapper2, DbTypeMapper<T3> mapper3, DbTypeMapper<T4> mapper4, DbTypeMapper<T5> mapper5, DbTypeMapper<T6> mapper6, DbTypeMapper<T7> mapper7)
	: ValueTupleMapperBase<(T1, T2, T3, T4, T5, T6, T7)>(dataMapper, [mapper1, mapper2, mapper3, mapper4, mapper5, mapper6, mapper7])
{
	protected override (T1, T2, T3, T4, T5, T6, T7) MapCore(IDataRecord record, int index, int count, DbConnectorRecordState? state)
	{
		Span<(int Index, int Count)> valueRanges = stackalloc (int Index, int Count)[7];
		GetValueRanges(record, index, count, valueRanges);
		return (
			MapValue(mapper1, record, valueRanges[0], state),
			MapValue(mapper2, record, valueRanges[1], state),
			MapValue(mapper3, record, valueRanges[2], state),
			MapValue(mapper4, record, valueRanges[3], state),
			MapValue(mapper5, record, valueRanges[4], state),
			MapValue(mapper6, record, valueRanges[5], state),
			MapValue(mapper7, record, valueRanges[6], state));
	}
}
