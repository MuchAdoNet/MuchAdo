using System.Data;

namespace MuchAdo.Mappers;

internal sealed class ValueTupleMapperRest<T1, T2, T3, T4, T5, T6, T7, TRest>(DbDataMapper dataMapper, DbTypeMapper<T1> mapper1, DbTypeMapper<T2> mapper2, DbTypeMapper<T3> mapper3, DbTypeMapper<T4> mapper4, DbTypeMapper<T5> mapper5, DbTypeMapper<T6> mapper6, DbTypeMapper<T7> mapper7, DbTypeMapper<TRest> mapperRest)
	: ValueTupleMapperBase<ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>>(dataMapper, [mapper1, mapper2, mapper3, mapper4, mapper5, mapper6, mapper7, mapperRest])
	where TRest : struct
{
	protected override ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> MapCore(IDataRecord record, int index, int count, DbConnectorRecordState? state)
	{
		Span<(int Index, int Count)> valueRanges = stackalloc (int Index, int Count)[8];
		GetValueRanges(record, index, count, valueRanges);
		return new ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>(
			mapper1.Map(record, valueRanges[0].Index, valueRanges[0].Count, state),
			mapper2.Map(record, valueRanges[1].Index, valueRanges[1].Count, state),
			mapper3.Map(record, valueRanges[2].Index, valueRanges[2].Count, state),
			mapper4.Map(record, valueRanges[3].Index, valueRanges[3].Count, state),
			mapper5.Map(record, valueRanges[4].Index, valueRanges[4].Count, state),
			mapper6.Map(record, valueRanges[5].Index, valueRanges[5].Count, state),
			mapper7.Map(record, valueRanges[6].Index, valueRanges[6].Count, state),
			mapperRest.Map(record, valueRanges[7].Index, valueRanges[7].Count, state));
	}
}
