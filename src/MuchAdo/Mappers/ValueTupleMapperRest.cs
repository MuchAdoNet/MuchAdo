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
			MapValue(mapper1, record, valueRanges[0], state),
			MapValue(mapper2, record, valueRanges[1], state),
			MapValue(mapper3, record, valueRanges[2], state),
			MapValue(mapper4, record, valueRanges[3], state),
			MapValue(mapper5, record, valueRanges[4], state),
			MapValue(mapper6, record, valueRanges[5], state),
			MapValue(mapper7, record, valueRanges[6], state),
			MapValue(mapperRest, record, valueRanges[7], state));
	}
}
