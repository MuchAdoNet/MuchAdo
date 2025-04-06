using System.Data;

namespace MuchAdo.Mappers;

internal sealed class EnumMapper<T> : NonNullableValueMapper<T>
	where T : struct
{
	public override T MapNotNullField(IDataRecord record, int index)
	{
		var value = record.GetValue(index);
		try
		{
			return value switch
			{
				T enumValue => enumValue,
#if !NETSTANDARD2_0
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
