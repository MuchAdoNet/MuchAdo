using System.Data;

namespace MuchAdo.Mappers;

internal sealed class NumericEnumMapper<TEnum, TUnderlyingType>(DbDataMapper dataMapper, DbTypeMapper<TUnderlyingType> underlyingTypeMapper) : SingleFieldValueMapper<TEnum>(dataMapper)
	where TEnum : struct
{
	public override TEnum MapNotNullField(IDataRecord record, int index, DbConnectorRecordState? state) =>
		(TEnum) (object) underlyingTypeMapper.Map(record, index, state)!;
}
