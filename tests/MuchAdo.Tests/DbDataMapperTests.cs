using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace MuchAdo.Tests;

[TestFixture]
internal sealed class DbDataMapperTests
{
	[Test]
	public void Strings()
	{
		using var connection = GetOpenConnectionWithItems();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();
		var record = WrapRecord(reader);

		// get non-nulls
		reader.Read().Should().BeTrue();

		record.Get<string>(0, 1).Should().Be(s_dto.TheText);

		// get nulls
		reader.Read().Should().BeTrue();

		record.Get<string>(0, 1).Should().BeNull();
	}

	[Test]
	public void NonNullableScalars()
	{
		using var connection = GetOpenConnectionWithItems();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();
		var record = WrapRecord(reader);

		// get non-nulls
		reader.Read().Should().BeTrue();

		record.Get<long>(1, 1).Should().Be(s_dto.TheInteger);
		record.Get<double>(2, 1).Should().Be(s_dto.TheReal);

		// get nulls
		reader.Read().Should().BeTrue();

		Invoking(() => record.Get<long>(1, 1)).Should().Throw<InvalidOperationException>();
		Invoking(() => record.Get<double>(2, 1)).Should().Throw<InvalidOperationException>();
	}

	[Test]
	public void NullableScalars()
	{
		using var connection = GetOpenConnectionWithItems();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();
		var record = WrapRecord(reader);

		// get non-nulls
		reader.Read().Should().BeTrue();

		record.Get<long?>(1, 1).Should().Be(s_dto.TheInteger);
		record.Get<double?>(2, 1).Should().Be(s_dto.TheReal);

		// get nulls
		reader.Read().Should().BeTrue();

		record.Get<long?>(1, 1).Should().BeNull();
		record.Get<double?>(2, 1).Should().BeNull();
	}

	[Test]
	public void Enums()
	{
		using var connection = GetOpenConnectionWithItems();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();
		var record = WrapRecord(reader);

		// get non-nulls
		reader.Read().Should().BeTrue();

		record.Get<Answer>(1, 1).Should().Be(Answer.FortyTwo);
		record.Get<Answer?>(1, 1).Should().Be(Answer.FortyTwo);

		// get nulls
		reader.Read().Should().BeTrue();

		Invoking(() => record.Get<Answer>(1, 1)).Should().Throw<InvalidOperationException>();
		record.Get<Answer?>(1, 1).Should().BeNull();
	}

	[Test]
	public void BadIndexCount()
	{
		using var connection = GetOpenConnectionWithItems();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();
		var record = WrapRecord(reader);

		reader.Read().Should().BeTrue();

		Invoking(() => record.Get<ItemDto>(-1, 2)).Should().Throw<ArgumentException>();
		Invoking(() => record.Get<ItemDto>(2, -1)).Should().Throw<ArgumentException>();
		Invoking(() => record.Get<ItemDto>(4, 1)).Should().Throw<ArgumentException>();
		Invoking(() => record.Get<ItemDto>(5, 0)).Should().Throw<ArgumentException>();
	}

	[Test]
	public void BadFieldCount()
	{
		using var connection = GetOpenConnectionWithItems();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();
		var record = WrapRecord(reader);

		reader.Read().Should().BeTrue();

		Invoking(() => record.Get<(string, long)>(0, 1)).Should().Throw<InvalidOperationException>();
		record.Get<(string?, long)>(0, 2).Should().Be((s_dto.TheText, s_dto.TheInteger));
		Invoking(() => record.Get<(string, long)>(0, 3)).Should().Throw<InvalidOperationException>();
	}

	[Test]
	public void ByteArrayTests()
	{
		using var connection = GetOpenConnectionWithItems();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();
		var record = WrapRecord(reader);

		// get non-nulls
		reader.Read().Should().BeTrue();

		record.Get<byte[]>(3, 1).Should().Equal(s_dto.TheBlob);

		// get nulls
		reader.Read().Should().BeTrue();

		record.Get<byte[]>(3, 1).Should().BeNull();
	}

	[Test]
	public void StreamTests()
	{
		using var connection = GetOpenConnectionWithItems();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();
		var record = WrapRecord(reader);

		// get non-nulls
		reader.Read().Should().BeTrue();

		var bytes = new byte[100];
		using (var stream = record.Get<Stream>(3, 1))
			stream.Read(bytes, 0, bytes.Length).Should().Be(s_dto.TheBlob!.Length);
		bytes.Take(s_dto.TheBlob!.Length).Should().Equal(s_dto.TheBlob);

		// get nulls
		reader.Read().Should().BeTrue();

		record.Get<Stream>(3, 1).Should().BeNull();
	}

	[Test]
	public void TupleTests([Values(2, 3, 4, 5, 6, 7, 8, 9, 10)] int fieldCount)
	{
		using var connection = GetOpenConnection();
		using var command = connection.CreateCommand();

		var columns = string.Join(", ", Enumerable.Range(1, fieldCount).Select(x => $"Item{x}"));
		command.CommandText = $"""
			create table Tuples ({columns});
			insert into Tuples ({columns}) values ({string.Join(", ", Enumerable.Range(1, fieldCount).Select(x => x))});
			insert into Tuples ({columns}) values ({string.Join(", ", Enumerable.Range(1, fieldCount).Select(_ => "null"))});
			""";
		command.ExecuteNonQuery();

		command.CommandText = "select * from Tuples;";
		using var reader = command.ExecuteReader();
		var record = WrapRecord(reader);

		// get non-nulls
		reader.Read().Should().BeTrue();
		if (fieldCount == 2)
			record.Get<(int, int)>().Should().Be((1, 2));
		else if (fieldCount == 3)
			record.Get<(int, int, int)>().Should().Be((1, 2, 3));
		else if (fieldCount == 4)
			record.Get<(int, int, int, int)>().Should().Be((1, 2, 3, 4));
		else if (fieldCount == 5)
			record.Get<(int, int, int, int, int)>().Should().Be((1, 2, 3, 4, 5));
		else if (fieldCount == 6)
			record.Get<(int, int, int, int, int, int)>().Should().Be((1, 2, 3, 4, 5, 6));
		else if (fieldCount == 7)
			record.Get<(int, int, int, int, int, int, int)>().Should().Be((1, 2, 3, 4, 5, 6, 7));
		else if (fieldCount == 8)
			record.Get<(int, int, int, int, int, int, int, int)>().Should().Be((1, 2, 3, 4, 5, 6, 7, 8));
		else if (fieldCount == 9)
			record.Get<(int, int, int, int, int, int, int, int, int)>().Should().Be((1, 2, 3, 4, 5, 6, 7, 8, 9));
		else if (fieldCount == 10)
			record.Get<(int, int, int, int, int, int, int, int, int, int)>().Should().Be((1, 2, 3, 4, 5, 6, 7, 8, 9, 10));

		// get nulls
		reader.Read().Should().BeTrue();
		if (fieldCount == 2)
			record.Get<(int?, int?)>().Should().Be((null, null));
		else if (fieldCount == 3)
			record.Get<(int?, int?, int?)>().Should().Be((null, null, null));
		else if (fieldCount == 4)
			record.Get<(int?, int?, int?, int?)>().Should().Be((null, null, null, null));
		else if (fieldCount == 5)
			record.Get<(int?, int?, int?, int?, int?)>().Should().Be((null, null, null, null, null));
		else if (fieldCount == 6)
			record.Get<(int?, int?, int?, int?, int?, int?)>().Should().Be((null, null, null, null, null, null));
		else if (fieldCount == 7)
			record.Get<(int?, int?, int?, int?, int?, int?, int?)>().Should().Be((null, null, null, null, null, null, null));
		else if (fieldCount == 8)
			record.Get<(int?, int?, int?, int?, int?, int?, int?, int?)>().Should().Be((null, null, null, null, null, null, null, null));
		else if (fieldCount == 9)
			record.Get<(int?, int?, int?, int?, int?, int?, int?, int?, int?)>().Should().Be((null, null, null, null, null, null, null, null, null));
		else if (fieldCount == 10)
			record.Get<(int?, int?, int?, int?, int?, int?, int?, int?, int?, int?)>().Should().Be((null, null, null, null, null, null, null, null, null, null));
		else
			throw new InvalidOperationException();
	}

	[Test]
	public void DtoTests()
	{
		using var connection = GetOpenConnectionWithItems();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();
		var record = WrapRecord(reader);

		// get non-nulls
		reader.Read().Should().BeTrue();

		// DTO
		record.Get<ItemDto>(0, 4).Should().BeEquivalentTo(s_dto);
		record.Get<ItemDto>(0, 1).Should().BeEquivalentTo(new ItemDto(s_dto.TheText));
		record.Get<ItemDto>(0, 0).Should().BeNull();
		record.Get<ItemDto>(4, 0).Should().BeNull();

		// tuple with DTO
		var tuple = record.Get<(string, ItemDto, byte[])>(0, 4);
		tuple.Item1.Should().Be(s_dto.TheText);
		tuple.Item2.Should().BeEquivalentTo(new ItemDto { TheInteger = s_dto.TheInteger, TheReal = s_dto.TheReal });
		tuple.Item3.Should().Equal(s_dto.TheBlob);

		// tuple with two DTOs (needs NULL terminator)
		Invoking(() => record.Get<(ItemDto, ItemDto)>(0, 3)).Should().Throw<InvalidOperationException>();

		// get nulls
		reader.Read().Should().BeTrue();

		// all nulls returns null DTO
		record.Get<ItemDto>(0, 4).Should().BeNull();
	}

	[Test]
	public void TwoDtos()
	{
		using var connection = GetOpenConnectionWithItems();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, null, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();
		var record = WrapRecord(reader);

		// get non-nulls
		reader.Read().Should().BeTrue();

		// two DTOs
		var tuple = record.Get<(ItemDto, ItemDto)>(0, 5);
		tuple.Item1.Should().BeEquivalentTo(new ItemDto(s_dto.TheText) { TheInteger = s_dto.TheInteger });
		tuple.Item2.Should().BeEquivalentTo(new ItemDto { TheReal = s_dto.TheReal, TheBlob = s_dto.TheBlob });

		// get nulls
		reader.Read().Should().BeTrue();

		// two DTOs
		tuple = record.Get<(ItemDto, ItemDto)>(0, 5);
		tuple.Item1.Should().BeNull();
		tuple.Item2.Should().BeNull();
	}

	[Test]
	public void TwoOneFieldDtos()
	{
		using var connection = GetOpenConnectionWithItems();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger from items;";
		using var reader = command.ExecuteReader();
		var record = WrapRecord(reader);

		// get non-nulls
		reader.Read().Should().BeTrue();

		// two DTOs
		var tuple = record.Get<(ItemDto, ItemDto)>(0, 2);
		tuple.Item1.Should().BeEquivalentTo(new ItemDto(s_dto.TheText));
		tuple.Item2.Should().BeEquivalentTo(new ItemDto { TheInteger = s_dto.TheInteger });

		// get nulls
		reader.Read().Should().BeTrue();

		// two DTOs
		tuple = record.Get<(ItemDto, ItemDto)>(0, 2);
		tuple.Item1.Should().BeNull();
		tuple.Item2.Should().BeNull();
	}

	[Test]
	public void RecordTests()
	{
		using var connection = GetOpenConnectionWithItems();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();
		var record = WrapRecord(reader);

		// get non-nulls
		reader.Read().Should().BeTrue();

		// record (compare by members because the TheBlob is compared by reference in .Equals())
		record.Get<ItemRecord>(0, 4).Should().BeEquivalentTo(s_record, x => x.ComparingByMembers<ItemRecord>());
		record.Get<ItemRecord>(0, 1).Should().BeEquivalentTo(new ItemRecord(s_record.TheText, default, default, default), x => x.ComparingByMembers<ItemRecord>());
		record.Get<ItemRecord>(0, 0).Should().BeNull();
		record.Get<ItemRecord>(4, 0).Should().BeNull();

		// tuple with record
		var tuple = record.Get<(string, ItemRecord, byte[])>(0, 4);
		tuple.Item1.Should().Be(s_record.TheText);
		tuple.Item2.Should().BeEquivalentTo(new ItemRecord(default, s_record.TheInteger, s_record.TheReal, default), x => x.ComparingByMembers<ItemRecord>());
		tuple.Item3.Should().Equal(s_record.TheBlob);

		// tuple with two records (needs NULL terminator)
		Invoking(() => record.Get<(ItemRecord, ItemRecord)>(0, 3)).Should().Throw<InvalidOperationException>();

		// get nulls
		reader.Read().Should().BeTrue();

		// all nulls returns null record
		record.Get<ItemRecord>(0, 4).Should().BeNull();
	}

	[Test]
	public void CaseInsensitivePropertyName()
	{
		using var connection = GetOpenConnectionWithItems();
		using var command = connection.CreateCommand();
		command.CommandText = "select thetext, THEinteger from items;";
		using var reader = command.ExecuteReader();
		var record = WrapRecord(reader);

		reader.Read().Should().BeTrue();
		record.Get<ItemDto>(0, 2)
			.Should().BeEquivalentTo(new ItemDto(s_dto.TheText) { TheInteger = s_dto.TheInteger });
	}

	[Test]
	public void UnderscorePropertyName()
	{
		using var connection = GetOpenConnectionWithItems();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText as the_text, TheInteger as the_integer from items;";
		using var reader = command.ExecuteReader();
		var record = WrapRecord(reader);

		reader.Read().Should().BeTrue();
		record.Get<ItemDto>(0, 2)
			.Should().BeEquivalentTo(new ItemDto(s_dto.TheText) { TheInteger = s_dto.TheInteger });
	}

	[Test]
	public void BadPropertyName()
	{
		using var connection = GetOpenConnectionWithItems();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger as Nope from items;";
		using var reader = command.ExecuteReader();
		var record = WrapRecord(reader);

		reader.Read().Should().BeTrue();
		Invoking(() => record.Get<ItemDto>(0, 2)).Should().Throw<InvalidOperationException>();
	}

	[Test]
	public void DynamicTests()
	{
		using var connection = GetOpenConnectionWithItems();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();
		var record = WrapRecord(reader);

		// get non-nulls
		reader.Read().Should().BeTrue();

		// dynamic
		((string) record.Get<dynamic>(0, 4).TheText).Should().Be(s_dto.TheText);
		((double) ((dynamic) record.Get<object>(0, 4)).TheReal).Should().Be(s_dto.TheReal);

		// tuple with dynamic
		var tuple = record.Get<(string, dynamic, byte[])>(0, 4);
		tuple.Item1.Should().Be(s_dto.TheText);
		((long) tuple.Item2.TheInteger).Should().Be(s_dto.TheInteger);
		tuple.Item3.Should().Equal(s_dto.TheBlob);

		// tuple with two dynamics (needs NULL terminator)
		Invoking(() => record.Get<(dynamic, dynamic)>(0, 3)).Should().Throw<InvalidOperationException>();

		// get nulls
		reader.Read().Should().BeTrue();

		// all nulls returns null dynamic
		((object) record.Get<dynamic>(0, 4)).Should().BeNull();
		record.Get<object>(0, 4).Should().BeNull();
	}

	[Test]
	public void DictionaryTests()
	{
		using var connection = GetOpenConnectionWithItems();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();
		var record = WrapRecord(reader);

		// get non-nulls
		reader.Read().Should().BeTrue();

		// dictionary
		((string) record.Get<Dictionary<string, object?>>(0, 4)["TheText"]!).Should().Be(s_dto.TheText);
		((long) record.Get<IDictionary<string, object?>>(0, 4)["TheInteger"]!).Should().Be(s_dto.TheInteger);
		((long) record.Get<IReadOnlyDictionary<string, object?>>(0, 4)["TheInteger"]!).Should().Be(s_dto.TheInteger);
		((double) record.Get<IDictionary>(0, 4)["TheReal"]!).Should().Be(s_dto.TheReal);

		// get nulls
		reader.Read().Should().BeTrue();

		// all nulls returns null dictionary
		record.Get<IDictionary>(0, 4).Should().BeNull();
	}

	[Test]
	public void ObjectTests()
	{
		using var connection = GetOpenConnectionWithItems();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();
		var record = WrapRecord(reader);

		// get non-nulls
		reader.Read().Should().BeTrue();

		// object/dynamic
		record.Get<object>(0).Should().Be(s_dto.TheText);
		((double) record.Get<dynamic>(2)).Should().Be(s_dto.TheReal);

		// tuple with object
		var tuple = record.Get<(string, object, double)>(0, 3);
		tuple.Item1.Should().Be(s_dto.TheText);
		tuple.Item2.Should().Be(s_dto.TheInteger);
		tuple.Item3.Should().Be(s_dto.TheReal);

		// tuple with three objects (doesn't need NULL terminator when the field count matches exactly)
		var tuple2 = record.Get<(object, object, object)>(0, 3);
		tuple2.Item1.Should().Be(s_dto.TheText);
		tuple2.Item2.Should().Be(s_dto.TheInteger);
		tuple2.Item3.Should().Be(s_dto.TheReal);

		// get nulls
		reader.Read().Should().BeTrue();

		// all nulls returns null dynamic
		record.Get<object>(0).Should().BeNull();
		((object) record.Get<dynamic>(0)).Should().BeNull();
	}

	[Test]
	public void GetExtension()
	{
		using var connection = GetOpenConnectionWithItems();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();
		var record = WrapRecord(reader);

		reader.Read().Should().BeTrue();
		record.Get<ItemDto>().Should().BeEquivalentTo(s_dto);
	}

	[Test]
	public void GetAtExtension()
	{
		using var connection = GetOpenConnectionWithItems();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();
		var record = WrapRecord(reader);

		reader.Read().Should().BeTrue();
		record.Get<long>(1).Should().Be(s_dto.TheInteger);
		record.Get<long>("TheInteger").Should().Be(s_dto.TheInteger);
#if !NET472
		record.Get<long>(^3).Should().Be(s_dto.TheInteger);
#endif
	}

	[Test]
	public void GetRangeExtension()
	{
		using var connection = GetOpenConnectionWithItems();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();
		var record = WrapRecord(reader);

		reader.Read().Should().BeTrue();
		record.Get<(long, double)>(1, 2).Should().Be((s_dto.TheInteger, s_dto.TheReal));
		record.Get<(long, double)>("TheInteger", 2).Should().Be((s_dto.TheInteger, s_dto.TheReal));
		record.Get<(long, double)>("TheInteger", "TheReal").Should().Be((s_dto.TheInteger, s_dto.TheReal));
#if !NET472
		record.Get<(long, double)>(1..3).Should().Be((s_dto.TheInteger, s_dto.TheReal));
#endif
	}

	[Test]
	public void CustomDtoTests()
	{
		using var connection = GetOpenConnectionWithItems();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();
		var record = WrapRecord(reader);

		reader.Read().Should().BeTrue();
		record.Get<CustomColumnDto>(0, 1).Should().BeEquivalentTo(new CustomColumnDto { Text = s_dto.TheText });
		reader.Read().Should().BeTrue();
		record.Get<CustomColumnDto>(0, 1).Should().BeNull();
	}

	private static IDbConnection GetOpenConnection()
	{
		var connection = new SqliteConnection("Data Source=:memory:");
		connection.Open();
		return connection;
	}

	private static IDbConnection GetOpenConnectionWithItems()
	{
		var connection = GetOpenConnection();

		using (var command = connection.CreateCommand())
		{
			command.CommandText = "create table Items (TheText text null, TheInteger integer null, TheReal real null, TheBlob blob null);";
			command.ExecuteNonQuery();
		}

		using (var command = connection.CreateCommand())
		{
			command.CommandText = "insert into Items (TheText, TheInteger, TheReal, TheBlob) values ('hey', 42, 3.1415, X'01FE');";
			command.ExecuteNonQuery();
		}

		using (var command = connection.CreateCommand())
		{
			command.CommandText = "insert into Items (TheText, TheInteger, TheReal, TheBlob) values (null, null, null, null);";
			command.ExecuteNonQuery();
		}

		return connection;
	}

	private static DbRecord WrapRecord(IDataRecord record) => new(record, DbDataMapper.Default, new DbRecordState());

	private sealed class ItemDto
	{
		public ItemDto() => TheText = null;
		public ItemDto(string? theText) => TheText = theText;

		public string? TheText { get; }
		public long TheInteger { get; init; }
		public double TheReal { get; init; }
		public byte[]? TheBlob { get; init; }
	}

	private sealed class CustomColumnDto
	{
		[Column("TheText")]
		public string? Text { get; set; }
	}

#pragma warning disable CA1801, SA1313
	private sealed record ItemRecord(string? TheText, long TheInteger, double TheReal, byte[]? TheBlob, long TheOptionalInteger = 42);
#pragma warning restore CA1801, SA1313

	private enum Answer
	{
		FortyTwo = 42,
	}

	private static readonly ItemDto s_dto = new("hey")
	{
		TheInteger = 42L,
		TheReal = 3.1415,
		TheBlob = new byte[] { 0x01, 0xFE },
	};

	private static readonly ItemRecord s_record = new(
		TheText: "hey",
		TheInteger: 42L,
		TheReal: 3.1415,
		TheBlob: new byte[] { 0x01, 0xFE });
}
