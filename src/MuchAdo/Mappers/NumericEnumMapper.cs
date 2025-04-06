using System.Data;

namespace MuchAdo.Mappers;

internal sealed class NumericEnumMapper<TEnum, TUnderlyingType> : NonNullableValueMapper<TEnum>
	where TEnum : struct
{
	public NumericEnumMapper(DbDataMapper dataMapper)
	{
		m_underlyingTypeMapper = dataMapper.GetTypeMapper<TUnderlyingType>();
	}

	public override TEnum MapNotNullField(IDataRecord record, int index) =>
		(TEnum) (object) m_underlyingTypeMapper.Map(record, index, state: null)!;

	private readonly DbTypeMapper<TUnderlyingType> m_underlyingTypeMapper;
}
