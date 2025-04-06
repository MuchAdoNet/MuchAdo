using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace MuchAdo.Mappers;

internal sealed class ValueTupleMapper<T1>(DbTypeMapper<T1> mapper1)
	: ValueTupleMapperBase<ValueTuple<T1>>([mapper1])
{
	protected override ValueTuple<T1> MapCore(IDataRecord record, int index, int count, DbConnectorRecordState? state)
	{
		Span<(int Index, int Count)> valueRanges = stackalloc (int Index, int Count)[1];
		GetValueRanges(record, index, count, valueRanges);
		return new ValueTuple<T1>(mapper1.Map(record, valueRanges[0].Index, valueRanges[0].Count, state));
	}
}

[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sme name.")]
internal sealed class ValueTupleMapper<T1, T2>(DbTypeMapper<T1> mapper1, DbTypeMapper<T2> mapper2)
	: ValueTupleMapperBase<(T1, T2)>([mapper1, mapper2])
{
	protected override (T1, T2) MapCore(IDataRecord record, int index, int count, DbConnectorRecordState? state)
	{
		Span<(int Index, int Count)> valueRanges = stackalloc (int Index, int Count)[2];
		GetValueRanges(record, index, count, valueRanges);
		return (
			mapper1.Map(record, valueRanges[0].Index, valueRanges[0].Count, state),
			mapper2.Map(record, valueRanges[1].Index, valueRanges[1].Count, state));
	}
}

[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sme name.")]
internal sealed class ValueTupleMapper<T1, T2, T3>(DbTypeMapper<T1> mapper1, DbTypeMapper<T2> mapper2, DbTypeMapper<T3> mapper3)
	: ValueTupleMapperBase<(T1, T2, T3)>([mapper1, mapper2, mapper3])
{
	protected override (T1, T2, T3) MapCore(IDataRecord record, int index, int count, DbConnectorRecordState? state)
	{
		Span<(int Index, int Count)> valueRanges = stackalloc (int Index, int Count)[3];
		GetValueRanges(record, index, count, valueRanges);
		return (
			mapper1.Map(record, valueRanges[0].Index, valueRanges[0].Count, state),
			mapper2.Map(record, valueRanges[1].Index, valueRanges[1].Count, state),
			mapper3.Map(record, valueRanges[2].Index, valueRanges[2].Count, state));
	}
}

[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sme name.")]
internal sealed class ValueTupleMapper<T1, T2, T3, T4>(DbTypeMapper<T1> mapper1, DbTypeMapper<T2> mapper2, DbTypeMapper<T3> mapper3, DbTypeMapper<T4> mapper4)
	: ValueTupleMapperBase<(T1, T2, T3, T4)>([mapper1, mapper2, mapper3, mapper4])
{
	protected override (T1, T2, T3, T4) MapCore(IDataRecord record, int index, int count, DbConnectorRecordState? state)
	{
		Span<(int Index, int Count)> valueRanges = stackalloc (int Index, int Count)[4];
		GetValueRanges(record, index, count, valueRanges);
		return (
			mapper1.Map(record, valueRanges[0].Index, valueRanges[0].Count, state),
			mapper2.Map(record, valueRanges[1].Index, valueRanges[1].Count, state),
			mapper3.Map(record, valueRanges[2].Index, valueRanges[2].Count, state),
			mapper4.Map(record, valueRanges[3].Index, valueRanges[3].Count, state));
	}
}

[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sme name.")]
internal sealed class ValueTupleMapper<T1, T2, T3, T4, T5>(DbTypeMapper<T1> mapper1, DbTypeMapper<T2> mapper2, DbTypeMapper<T3> mapper3, DbTypeMapper<T4> mapper4, DbTypeMapper<T5> mapper5)
	: ValueTupleMapperBase<(T1, T2, T3, T4, T5)>([mapper1, mapper2, mapper3, mapper4, mapper5])
{
	protected override (T1, T2, T3, T4, T5) MapCore(IDataRecord record, int index, int count, DbConnectorRecordState? state)
	{
		Span<(int Index, int Count)> valueRanges = stackalloc (int Index, int Count)[5];
		GetValueRanges(record, index, count, valueRanges);
		return (
			mapper1.Map(record, valueRanges[0].Index, valueRanges[0].Count, state),
			mapper2.Map(record, valueRanges[1].Index, valueRanges[1].Count, state),
			mapper3.Map(record, valueRanges[2].Index, valueRanges[2].Count, state),
			mapper4.Map(record, valueRanges[3].Index, valueRanges[3].Count, state),
			mapper5.Map(record, valueRanges[4].Index, valueRanges[4].Count, state));
	}
}

[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sme name.")]
internal sealed class ValueTupleMapper<T1, T2, T3, T4, T5, T6>(DbTypeMapper<T1> mapper1, DbTypeMapper<T2> mapper2, DbTypeMapper<T3> mapper3, DbTypeMapper<T4> mapper4, DbTypeMapper<T5> mapper5, DbTypeMapper<T6> mapper6)
	: ValueTupleMapperBase<(T1, T2, T3, T4, T5, T6)>([mapper1, mapper2, mapper3, mapper4, mapper5, mapper6])
{
	protected override (T1, T2, T3, T4, T5, T6) MapCore(IDataRecord record, int index, int count, DbConnectorRecordState? state)
	{
		Span<(int Index, int Count)> valueRanges = stackalloc (int Index, int Count)[6];
		GetValueRanges(record, index, count, valueRanges);
		return (
			mapper1.Map(record, valueRanges[0].Index, valueRanges[0].Count, state),
			mapper2.Map(record, valueRanges[1].Index, valueRanges[1].Count, state),
			mapper3.Map(record, valueRanges[2].Index, valueRanges[2].Count, state),
			mapper4.Map(record, valueRanges[3].Index, valueRanges[3].Count, state),
			mapper5.Map(record, valueRanges[4].Index, valueRanges[4].Count, state),
			mapper6.Map(record, valueRanges[5].Index, valueRanges[5].Count, state));
	}
}

[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sme name.")]
internal sealed class ValueTupleMapper<T1, T2, T3, T4, T5, T6, T7>(DbTypeMapper<T1> mapper1, DbTypeMapper<T2> mapper2, DbTypeMapper<T3> mapper3, DbTypeMapper<T4> mapper4, DbTypeMapper<T5> mapper5, DbTypeMapper<T6> mapper6, DbTypeMapper<T7> mapper7)
	: ValueTupleMapperBase<(T1, T2, T3, T4, T5, T6, T7)>([mapper1, mapper2, mapper3, mapper4, mapper5, mapper6, mapper7])
{
	protected override (T1, T2, T3, T4, T5, T6, T7) MapCore(IDataRecord record, int index, int count, DbConnectorRecordState? state)
	{
		Span<(int Index, int Count)> valueRanges = stackalloc (int Index, int Count)[7];
		GetValueRanges(record, index, count, valueRanges);
		return (
			mapper1.Map(record, valueRanges[0].Index, valueRanges[0].Count, state),
			mapper2.Map(record, valueRanges[1].Index, valueRanges[1].Count, state),
			mapper3.Map(record, valueRanges[2].Index, valueRanges[2].Count, state),
			mapper4.Map(record, valueRanges[3].Index, valueRanges[3].Count, state),
			mapper5.Map(record, valueRanges[4].Index, valueRanges[4].Count, state),
			mapper6.Map(record, valueRanges[5].Index, valueRanges[5].Count, state),
			mapper7.Map(record, valueRanges[6].Index, valueRanges[6].Count, state));
	}
}
