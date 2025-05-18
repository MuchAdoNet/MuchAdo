using System.Data;

namespace MuchAdo.Mappers;

internal sealed class FlexibleEnumMapper<T>(DbDataMapper dataMapper) : SingleFieldValueMapper<T>(dataMapper)
	where T : struct
{
	public override T MapNotNullField(IDataRecord record, int index, DbConnectorRecordState? state)
	{
		var value = record.GetValue(index);
		try
		{
			return value switch
			{
				T enumValue => enumValue,
#if NET
				string stringValue => Enum.Parse<T>(stringValue, ignoreCase: true),
#else
				string stringValue => (T) Enum.Parse(typeof(T), stringValue, ignoreCase: true),
#endif
				_ => (T) Enum.ToObject(typeof(T), value),
			};
		}
		catch (Exception exception) when (exception is ArgumentException or InvalidCastException)
		{
			throw BadCast(value.GetType(), exception);
		}
	}
}
