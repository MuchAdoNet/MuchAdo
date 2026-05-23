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
		using var connector = GetConnectorWithItems();
		var index = 0;
		connector
			.Command("select TheText, TheInteger, TheReal, TheBlob from Items order by Id;")
			.Query(
				record =>
				{
					if (index == 0)
						record.Get<string>(0, 1).Should().Be(s_dto.TheText);
					else
						record.Get<string>(0, 1).Should().BeNull();
					index++;
					return 1;
				})
			.Sum().Should().Be(3);
	}

	[Test]
	public void NonNullableScalars()
	{
		using var connector = GetConnectorWithItems();
		var index = 0;
		connector
			.Command("select TheText, TheInteger, TheReal, TheBlob from Items order by Id;")
			.Query(
				record =>
				{
					if (index == 0)
					{
						record.Get<long>(1, 1).Should().Be(s_dto.TheInteger);
						record.Get<double>(2, 1).Should().Be(s_dto.TheReal);
					}
					else
					{
						Invoking(() => record.Get<long>(1, 1)).Should().Throw<InvalidOperationException>();
						Invoking(() => record.Get<double>(2, 1)).Should().Throw<InvalidOperationException>();
					}
					index++;
					return 1;
				})
			.Sum().Should().Be(2);
	}

	[Test]
	public void NullableScalars()
	{
		using var connector = GetConnectorWithItems();
		var index = 0;
		connector
			.Command("select TheText, TheInteger, TheReal, TheBlob from Items order by Id;")
			.Query(
				record =>
				{
					if (index == 0)
					{
						record.Get<long?>(1, 1).Should().Be(s_dto.TheInteger);
						record.Get<double?>(2, 1).Should().Be(s_dto.TheReal);
					}
					else
					{
						record.Get<long?>(1, 1).Should().BeNull();
						record.Get<double?>(2, 1).Should().BeNull();
					}
					index++;
					return 1;
				})
			.Sum().Should().Be(2);
	}

	[Test]
	public void Enums()
	{
		using var connector = GetConnectorWithItems();
		var index = 0;
		connector
			.Command("select TheText, TheInteger, TheReal, TheBlob from Items order by Id;")
			.Query(
				record =>
				{
					if (index == 0)
					{
						record.Get<Answer>(1, 1).Should().Be(Answer.FortyTwo);
						record.Get<Answer?>(1, 1).Should().Be(Answer.FortyTwo);
					}
					else
					{
						Invoking(() => record.Get<Answer>(1, 1)).Should().Throw<InvalidOperationException>();
						record.Get<Answer?>(1, 1).Should().BeNull();
					}
					index++;
					return 1;
				})
			.Sum().Should().Be(2);
	}

	[Test]
	public void PrimitiveMapperMatrix()
	{
		var dateTime = new DateTime(2026, 5, 11, 12, 30, 45, DateTimeKind.Utc);
		var dateTimeOffset = new DateTimeOffset(dateTime);
		var guid = Guid.Parse("7b36e5fd-d4a1-418f-85bd-9ab3905c16cf");
		short int16 = -12;
		var timeSpan = TimeSpan.FromSeconds(123);
		var table = new DataTable();
		table.Columns.Add("Bool", typeof(bool));
		table.Columns.Add("Byte", typeof(byte));
		table.Columns.Add("Char", typeof(char));
		table.Columns.Add("DateTime", typeof(DateTime));
		table.Columns.Add("Float", typeof(float));
		table.Columns.Add("Guid", typeof(Guid));
		table.Columns.Add("Int16", typeof(short));
		table.Columns.Add("DateTimeOffset", typeof(DateTimeOffset));
		table.Columns.Add("TimeSpan", typeof(TimeSpan));
		table.Columns.Add("UInt32", typeof(uint));
		table.Rows.Add(true, (byte) 7, 'z', dateTime, 1.25f, guid, int16, dateTimeOffset, timeSpan, 123U);
		table.Rows.Add(DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value);

		using var reader = table.CreateDataReader();
		reader.Read().Should().BeTrue();
		var mapper = DbDataMapper.Default;
		mapper.GetTypeMapper<bool>().Map(reader, 0, null).Should().BeTrue();
		mapper.GetTypeMapper<byte>().Map(reader, 1, null).Should().Be(7);
		mapper.GetTypeMapper<char>().Map(reader, 2, null).Should().Be('z');
		mapper.GetTypeMapper<DateTime>().Map(reader, 3, null).Should().Be(dateTime);
		mapper.GetTypeMapper<float>().Map(reader, 4, null).Should().Be(1.25f);
		mapper.GetTypeMapper<Guid>().Map(reader, 5, null).Should().Be(guid);
		mapper.GetTypeMapper<short>().Map(reader, 6, null).Should().Be(-12);
		mapper.GetTypeMapper<DateTimeOffset>().Map(reader, 7, null).Should().Be(dateTimeOffset);
		mapper.GetTypeMapper<TimeSpan>().Map(reader, 8, null).Should().Be(timeSpan);
		mapper.GetTypeMapper<uint>().Map(reader, 9, null).Should().Be(123U);

		reader.Read().Should().BeTrue();
		Invoking(() => mapper.GetTypeMapper<bool>().Map(reader, 0, null)).Should().Throw<InvalidOperationException>();
		mapper.GetTypeMapper<bool?>().Map(reader, 0, null).Should().BeNull();
		Invoking(() => mapper.GetTypeMapper<byte>().Map(reader, 1, null)).Should().Throw<InvalidOperationException>();
		mapper.GetTypeMapper<byte?>().Map(reader, 1, null).Should().BeNull();
		Invoking(() => mapper.GetTypeMapper<char>().Map(reader, 2, null)).Should().Throw<InvalidOperationException>();
		mapper.GetTypeMapper<char?>().Map(reader, 2, null).Should().BeNull();
		Invoking(() => mapper.GetTypeMapper<DateTime>().Map(reader, 3, null)).Should().Throw<InvalidOperationException>();
		mapper.GetTypeMapper<DateTime?>().Map(reader, 3, null).Should().BeNull();
		Invoking(() => mapper.GetTypeMapper<float>().Map(reader, 4, null)).Should().Throw<InvalidOperationException>();
		mapper.GetTypeMapper<float?>().Map(reader, 4, null).Should().BeNull();
		Invoking(() => mapper.GetTypeMapper<Guid>().Map(reader, 5, null)).Should().Throw<InvalidOperationException>();
		mapper.GetTypeMapper<Guid?>().Map(reader, 5, null).Should().BeNull();
		Invoking(() => mapper.GetTypeMapper<short>().Map(reader, 6, null)).Should().Throw<InvalidOperationException>();
		mapper.GetTypeMapper<short?>().Map(reader, 6, null).Should().BeNull();
	}

	[Test]
	public void FallbackReferenceMappers()
	{
		var mapper = DbDataMapper.Default;
		var bytes = new byte[] { 1, 2, 3 };

		mapper.GetTypeMapper<TextReader>().Map(new FakeDataRecord("hello"), 0, null).ReadToEnd().Should().Be("hello");
		mapper.GetTypeMapper<byte[]>().Map(new FakeDataRecord(bytes, returnBytesFromGetValue: false), 0, null).Should().Equal(bytes);
		using var stream = mapper.GetTypeMapper<Stream>().Map(new FakeDataRecord(bytes), 0, null);
		stream.ReadByte().Should().Be(1);
		stream.ReadByte().Should().Be(2);
		stream.ReadByte().Should().Be(3);
		stream.ReadByte().Should().Be(-1);

		Invoking(() => mapper.GetTypeMapper<DateTimeOffset>().Map(new FakeDataRecord(DateTimeOffset.Now), 0, null))
			.Should().Throw<InvalidOperationException>()
			.WithMessage("Record must be a DbDataRecord.");
	}

	[Test]
	public void BadIndexCount()
	{
		using var connector = GetConnectorWithItems();
		connector
			.Command("select TheText, TheInteger, TheReal, TheBlob from Items order by Id;")
			.Query(
				record =>
				{
					Invoking(() => record.Get<ItemDto>(-1, 2)).Should().Throw<ArgumentException>();
					Invoking(() => record.Get<ItemDto>(2, -1)).Should().Throw<ArgumentException>();
					Invoking(() => record.Get<ItemDto>(4, 1)).Should().Throw<ArgumentException>();
					Invoking(() => record.Get<ItemDto>(5, 0)).Should().Throw<ArgumentException>();
					return 1;
				})
			.Sum().Should().Be(2);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void BadFieldCount(bool ignore)
	{
		using var connector = GetConnectorWithItems(ignoreUnusedFields: ignore);
		connector
			.Command("select TheText, TheInteger, TheReal, TheBlob from Items order by Id;")
			.QueryFirst(
				record =>
				{
					record.FieldCount.Should().Be(4);
					Invoking(() => record.Get<(string, long)>(0, 1)).Should().Throw<InvalidOperationException>();
					record.Get<(string?, long)>(0, 2).Should().Be((s_dto.TheText, s_dto.TheInteger));
					if (ignore)
						record.Get<(string?, long)>(0, 3).Should().Be((s_dto.TheText, s_dto.TheInteger));
					else
						Invoking(() => record.Get<(string?, long)>(0, 3)).Should().Throw<InvalidOperationException>();
					return 1;
				})
			.Should().Be(1);
	}

	[Test]
	public void BadVariableLengthTupleFieldCount()
	{
		using var connector = GetConnectorWithItems();
		connector
			.Command("select TheText, TheInteger, TheReal, TheBlob from Items order by Id;")
			.QueryFirst(
				record =>
				{
					Invoking(() => record.Get<(object, string)>(0, 1))
						.Should().Throw<InvalidOperationException>()
						.WithMessage("Not enough fields for tuple item 0 in System.ValueTuple`2*");
					Invoking(() => record.Get<(object, string, int)>(0, 1))
						.Should().Throw<InvalidOperationException>()
						.WithMessage("Not enough fields for tuple item 0 in System.ValueTuple`3*");
					Invoking(() => record.Get<(string, int)>(0, 1))
						.Should().Throw<InvalidOperationException>()
						.WithMessage("System.ValueTuple`2* must be read from 2 fields but is being read from 1 fields.");
					return 1;
				})
			.Should().Be(1);
	}

	[Test]
	public void ByteArrayTests()
	{
		using var connector = GetConnectorWithItems();
		var index = 0;
		connector
			.Command("select TheText, TheInteger, TheReal, TheBlob from Items order by Id;")
			.Query(
				record =>
				{
					if (index == 0)
						record.Get<byte[]>(3, 1).Should().Equal(s_dto.TheBlob);
					else
						record.Get<byte[]>(3, 1).Should().BeNull();
					index++;
					return 1;
				})
			.Sum().Should().Be(2);
	}

	[Test]
	public void StreamTests()
	{
		using var connector = GetConnectorWithItems();
		var index = 0;
		connector
			.Command("select TheText, TheInteger, TheReal, TheBlob from Items order by Id;")
			.Query(
				record =>
				{
					if (index == 0)
					{
						var bytes = new byte[100];
						using (var stream = record.Get<Stream>(3, 1))
							stream.Read(bytes, 0, bytes.Length).Should().Be(s_dto.TheBlob!.Length);
						bytes.Take(s_dto.TheBlob!.Length).Should().Equal(s_dto.TheBlob);
					}
					else
					{
						record.Get<Stream>(3, 1).Should().BeNull();
					}
					index++;
					return 1;
				})
			.Sum().Should().Be(2);
	}

	[Test]
	public void TupleTests([Values(2, 3, 4, 5, 6, 7, 8, 9, 10)] int fieldCount)
	{
		using var connector = new DbConnector(new SqliteConnection("Data Source=:memory:"));
		var fieldIndices = Enumerable.Range(1, fieldCount).ToList();
		var columns = Sql.Intersperse(", ", fieldIndices.Select(x => Sql.Raw($"Item{x}")));
		connector
			.CommandFormat($"""
				create table Tuples (Id integer primary key,
					{Sql.Intersperse(", ", fieldIndices.Select(x => Sql.Raw($"Item{x} integer")))});
				insert into Tuples ({columns}) values
					({Sql.Intersperse(", ", fieldIndices.Select(x => Sql.Raw($"{x}")))}),
					({Sql.Intersperse(", ", fieldIndices.Select(_ => Sql.Raw("null")))});
				""")
			.Execute();

		var index = 0;
		connector
			.CommandFormat($"select {columns} from Tuples order by Id;")
			.Query(
				record =>
				{
					if (index == 0)
					{
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
					}
					else
					{
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
					index++;
					return 1;
				})
			.Sum().Should().Be(2);
	}

	[Test]
	public void NullableTuples()
	{
		using var connector = new DbConnector(new SqliteConnection("Data Source=:memory:"));
		connector
			.Command("""
				create table Tuples (Id integer primary key, Item1 integer, Item2 integer, Item3 integer);
				insert into Tuples (Item1, Item2, Item3) values (1, 2, 3), (null, null, null), (null, 2, 3);
				""")
			.Execute();

		var index = 0;
		connector
			.Command("select Item1, Item2, Item3 from Tuples order by Id;")
			.Query(record =>
			{
				if (index == 0)
				{
					record.Get<(int, int, int)>().Should().Be((1, 2, 3));
					record.Get<(int, int, int)?>().Should().Be((1, 2, 3));
					(int?, int?, int?) expected = (1, 2, 3);
					record.Get<(int?, int?, int?)>().Should().Be(expected);
					record.Get<(int?, int?, int?)?>().Should().Be(expected);
				}
				else if (index == 1)
				{
					Invoking(record.Get<(int, int, int)>).Should().Throw<InvalidOperationException>();
					record.Get<(int, int, int)?>().Should().Be(null);
					record.Get<(int?, int?, int?)>().Should().Be((null, null, null));
					record.Get<(int?, int?, int?)?>().Should().Be(null);
				}
				else
				{
					Invoking(record.Get<(int, int, int)>).Should().Throw<InvalidOperationException>();
					Invoking(record.Get<(int, int, int)?>).Should().Throw<InvalidOperationException>();
					(int?, int?, int?) expected = (null, 2, 3);
					record.Get<(int?, int?, int?)>().Should().Be(expected);
					record.Get<(int?, int?, int?)?>().Should().Be(expected);
				}
				index++;
				return 1;
			})
			.Sum().Should().Be(3);
	}

	[Test]
	public void ReadonlyStructs()
	{
		var item = new NameValue("one", "two");

		using var connector = new DbConnector(new SqliteConnection("Data Source=:memory:"));
		connector.Command("create table Items (ItemId integer primary key, Name text not null, Value text not null)").Execute();
		connector.CommandFormat($"insert into Items (Name, Value) values ({item.Name}, {item.Value})").Execute();

		connector.CommandFormat($"select Name, Value from Items where Name = {item.Name}").QueryFirstOrDefault<NameValue>().Should().Be(item);
		connector.CommandFormat($"select Name, Value from Items where Name = {item.Value}").QueryFirstOrDefault<NameValue>().Should().Be(default(NameValue));

		connector.CommandFormat($"select Name, Value from Items where Name = {item.Name}").QueryFirstOrDefault<NameValue?>().Should().Be(item);
		connector.CommandFormat($"select Name, Value from Items where Name = {item.Value}").QueryFirstOrDefault<NameValue?>().Should().Be(null);
	}

	[Test]
	public void DtoTests()
	{
		using var connector = GetConnectorWithItems();
		var index = 0;
		connector
			.Command("select TheText, TheInteger, TheReal, TheBlob from Items order by Id;")
			.Query(
				record =>
				{
					if (index == 0)
					{
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
					}
					else
					{
						record.Get<ItemDto>(0, 4).Should().BeNull();
					}
					index++;
					return 1;
				})
			.Sum().Should().Be(2);
	}

	[Test]
	public void TwoDtos()
	{
		using var connector = GetConnectorWithItems();
		var index = 0;
		connector
			.Command("select TheText, TheInteger, null, TheReal, TheBlob from Items order by Id;")
			.Query(
				record =>
				{
					if (index == 0)
					{
						var tuple = record.Get<(ItemDto, ItemDto)>(0, 5);
						tuple.Item1.Should().BeEquivalentTo(new ItemDto(s_dto.TheText) { TheInteger = s_dto.TheInteger });
						tuple.Item2.Should().BeEquivalentTo(new ItemDto { TheReal = s_dto.TheReal, TheBlob = s_dto.TheBlob });
					}
					else
					{
						var tuple = record.Get<(ItemDto, ItemDto)>(0, 5);
						tuple.Item1.Should().BeNull();
						tuple.Item2.Should().BeNull();
					}
					index++;
					return 1;
				})
			.Sum().Should().Be(2);
	}

	[Test]
	public void TwoOneFieldDtos()
	{
		using var connector = GetConnectorWithItems();
		var index = 0;
		connector
			.Command("select TheText, TheInteger from Items order by Id;")
			.Query(
				record =>
				{
					if (index == 0)
					{
						var tuple = record.Get<(ItemDto, ItemDto)>(0, 2);
						tuple.Item1.Should().BeEquivalentTo(new ItemDto(s_dto.TheText));
						tuple.Item2.Should().BeEquivalentTo(new ItemDto { TheInteger = s_dto.TheInteger });
					}
					else
					{
						var tuple = record.Get<(ItemDto, ItemDto)>(0, 2);
						tuple.Item1.Should().BeNull();
						tuple.Item2.Should().BeNull();
					}
					index++;
					return 1;
				})
			.Sum().Should().Be(2);
	}

	[Test]
	public void RecordTests()
	{
		using var connector = GetConnectorWithItems();
		var index = 0;
		connector
			.Command("select TheText, TheInteger, TheReal, TheBlob from Items order by Id;")
			.Query(
				record =>
				{
					if (index == 0)
					{
						// record (compare by members because the TheBlob is compared by reference in .Equals())
						record.Get<ItemRecord>(0, 4).Should().BeEquivalentTo(s_record, x => x.ComparingByMembers<ItemRecord>());
						record.Get<ItemRecord>(0, 1).Should().BeEquivalentTo(new ItemRecord(s_record.TheText, 0, 0, null), x => x.ComparingByMembers<ItemRecord>());
						record.Get<ItemRecord>(0, 0).Should().BeNull();
						record.Get<ItemRecord>(4, 0).Should().BeNull();

						// tuple with record
						var tuple = record.Get<(string, ItemRecord, byte[])>(0, 4);
						tuple.Item1.Should().Be(s_record.TheText);
						tuple.Item2.Should().BeEquivalentTo(new ItemRecord(null, s_record.TheInteger, s_record.TheReal, null), x => x.ComparingByMembers<ItemRecord>());
						tuple.Item3.Should().Equal(s_record.TheBlob);

						// tuple with two records (needs NULL terminator)
						Invoking(() => record.Get<(ItemRecord, ItemRecord)>(0, 3)).Should().Throw<InvalidOperationException>();
					}
					else
					{
						record.Get<ItemRecord>(0, 4).Should().BeNull();
					}
					index++;
					return 1;
				})
			.Sum().Should().Be(2);
	}

	[Test]
	public void CaseInsensitivePropertyName()
	{
		using var connector = GetConnectorWithItems();
		var index = 0;
		connector
			.Command("select thetext, THEinteger from Items order by Id;")
			.Query(
				record =>
				{
					if (index == 0)
					{
						record.Get<ItemDto>(0, 2)
							.Should().BeEquivalentTo(new ItemDto(s_dto.TheText) { TheInteger = s_dto.TheInteger });
					}
					index++;
					return 1;
				})
			.Sum().Should().Be(2);
	}

	[Test]
	public void UnderscorePropertyName()
	{
		using var connector = GetConnectorWithItems();
		var index = 0;
		connector
			.Command("select TheText as the_text, TheInteger as the_integer from Items order by Id;")
			.Query(
				record =>
				{
					if (index == 0)
					{
						record.Get<ItemDto>(0, 2)
							.Should().BeEquivalentTo(new ItemDto(s_dto.TheText) { TheInteger = s_dto.TheInteger });
					}
					index++;
					return 1;
				})
			.Sum().Should().Be(2);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void BadPropertyName(bool ignore)
	{
		using var connector = GetConnectorWithItems(ignoreUnusedFields: ignore);
		var index = 0;
		connector
			.Command("select TheText, TheInteger as Nope from Items order by Id;")
			.Query(
				record =>
				{
					if (index == 0)
					{
						if (ignore)
							record.Get<ItemDto>(0, 2).Should().BeEquivalentTo(new ItemDto(s_dto.TheText));
						else
							Invoking(() => record.Get<ItemDto>(0, 2)).Should().Throw<InvalidOperationException>();
					}
					index++;
					return 1;
				})
			.Sum().Should().Be(2);
	}

	[Test]
	public void DynamicTests()
	{
		using var connector = GetConnectorWithItems();
		var index = 0;
		connector
			.Command("select TheText, TheInteger, TheReal, TheBlob from Items order by Id;")
			.Query(
				record =>
				{
					if (index == 0)
					{
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
					}
					else
					{
						// all nulls returns null dynamic
						((object) record.Get<dynamic>(0, 4)).Should().BeNull();
						record.Get<object>(0, 4).Should().BeNull();
					}
					index++;
					return 1;
				})
			.Sum().Should().Be(2);
	}

	[Test]
	public void DictionaryTests()
	{
		using var connector = GetConnectorWithItems();
		var index = 0;
		connector
			.Command("select TheText, TheInteger, TheReal, TheBlob from Items order by Id;")
			.Query(
				record =>
				{
					if (index == 0)
					{
						// dictionary
						((string) record.Get<Dictionary<string, object?>>(0, 4)["TheText"]!).Should().Be(s_dto.TheText);
						((long) record.Get<IDictionary<string, object?>>(0, 4)["TheInteger"]!).Should().Be(s_dto.TheInteger);
						((long) record.Get<IReadOnlyDictionary<string, object?>>(0, 4)["TheInteger"]!).Should().Be(s_dto.TheInteger);
						((double) record.Get<IDictionary>(0, 4)["TheReal"]!).Should().Be(s_dto.TheReal);
						record.Get<Dictionary<string, double>>(1, 2)["TheInteger"].Should().Be(s_dto.TheInteger);
						record.Get<Dictionary<string, double>>(1, 2)["TheReal"].Should().Be(s_dto.TheReal);
					}
					else
					{
						// all nulls returns null dictionary
						record.Get<IDictionary>(0, 4).Should().BeNull();
					}
					index++;
					return 1;
				})
			.Sum().Should().Be(2);
	}

	[Test]
	public void ObjectTests()
	{
		using var connector = GetConnectorWithItems();
		var index = 0;
		connector
			.Command("select TheText, TheInteger, TheReal, TheBlob from Items order by Id;")
			.Query(
				record =>
				{
					if (index == 0)
					{
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

						var tuple3 = record.Get<(object, double)>(1, 2);
						tuple3.Item1.Should().Be(s_dto.TheInteger);
						tuple3.Item2.Should().Be(s_dto.TheReal);
					}
					else
					{
						// all nulls returns null dynamic
						record.Get<object>(0).Should().BeNull();
						((object) record.Get<dynamic>(0)).Should().BeNull();
					}
					index++;
					return 1;
				})
			.Sum().Should().Be(2);
	}

	[Test]
	public void Get()
	{
		using var connector = GetConnectorWithItems();
		connector
			.Command("select TheText, TheInteger, TheReal, TheBlob from Items order by Id;")
			.QueryFirst(
				record =>
				{
					record.Get<ItemDto>().Should().BeEquivalentTo(s_dto);
					return 1;
				})
			.Should().Be(1);
	}

	[Test]
	public void GetAt()
	{
		using var connector = GetConnectorWithItems();
		connector
			.Command("select TheText, TheInteger, TheReal, TheBlob from Items order by Id;")
			.QueryFirst(
				record =>
				{
					record.Get<long>(1).Should().Be(s_dto.TheInteger);
					record.Get<long>("TheInteger").Should().Be(s_dto.TheInteger);
#if NET
					record.Get<long>(^3).Should().Be(s_dto.TheInteger);
#endif
					return 1;
				})
			.Should().Be(1);
	}

	[Test]
	public void As()
	{
		using var connector = GetConnectorWithItems();
		connector
			.Command("select TheText, TheInteger, TheReal, TheBlob from Items order by Id;")
			.QueryFirst(
				record =>
				{
					record.GetName(1).Should().Be("TheInteger");
					record.GetOrdinal("TheInteger").Should().Be(1);
					record.As<IDataRecord>().GetFieldType(0).FullName.Should().Be("System.String");
					record.As<SqliteDataReader>().Handle.Should().NotBe(0);
					return 1;
				})
			.Should().Be(1);
	}

	[Test]
	public void GetRange()
	{
		using var connector = GetConnectorWithItems();
		connector
			.Command("select TheText, TheInteger, TheReal, TheBlob from Items order by Id;")
			.QueryFirst(
				record =>
				{
					record.Get<(long, double)>(1, 2).Should().Be((s_dto.TheInteger, s_dto.TheReal));
					record.Get<(long, double)>("TheInteger", 2).Should().Be((s_dto.TheInteger, s_dto.TheReal));
#if NET
					record.Get<(long, double)>(1..3).Should().Be((s_dto.TheInteger, s_dto.TheReal));
#endif
					return 1;
				})
			.Should().Be(1);
	}

	[Test]
	public void CustomDtoTests()
	{
		using var connector = GetConnectorWithItems();
		var index = 0;
		connector
			.Command("select TheText, TheInteger, TheReal, TheBlob from Items order by Id;")
			.Query(
				record =>
				{
					if (index == 0)
						record.Get<CustomColumnDto>(0, 1).Should().BeEquivalentTo(new CustomColumnDto { Text = s_dto.TheText });
					else
						record.Get<CustomColumnDto>(0, 1).Should().BeNull();
					index++;
					return 1;
				})
			.Sum().Should().Be(2);
	}

	private static DbConnector GetConnectorWithItems(bool ignoreUnusedFields = false)
	{
		var settings = new DbConnectorSettings
		{
			DataMapper = DbDataMapper.Default.WithIgnoreUnusedFields(ignoreUnusedFields),
		};

		var connector = new DbConnector(new SqliteConnection("Data Source=:memory:"), settings);
		connector
			.Command("create table Items (Id integer primary key, TheText text null, TheInteger integer null, TheReal real null, TheBlob blob null);")
			.Execute();
		connector
			.Command("insert into Items (TheText, TheInteger, TheReal, TheBlob) values ('hey', 42, 3.1415, X'01FE');")
			.Execute();
		connector
			.Command("insert into Items (TheText, TheInteger, TheReal, TheBlob) values (null, null, null, null);")
			.Execute();
		return connector;
	}

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

	private sealed class FakeDataRecord(object value, bool returnBytesFromGetValue = true) : IDataRecord
	{
		public int FieldCount => 1;
		public object this[int index] => GetValue(index);
		public object this[string name] => GetValue(GetOrdinal(name));
		public string GetName(int index) => "Value";
		public string GetDataTypeName(int index) => value.GetType().Name;
		public Type GetFieldType(int index) => value.GetType();
		public object GetValue(int index) => returnBytesFromGetValue ? value : new object();
		public int GetValues(object[] values)
		{
			values[0] = GetValue(0);
			return 1;
		}
		public int GetOrdinal(string name) => name == "Value" ? 0 : -1;
		public bool GetBoolean(int index) => (bool) value;
		public byte GetByte(int index) => (byte) value;
		public long GetBytes(int index, long fieldOffset, byte[]? buffer, int bufferoffset, int length)
		{
			var bytes = (byte[]) value;
			if (buffer is not null)
				Array.Copy(bytes, (int) fieldOffset, buffer, bufferoffset, length);
			return bytes.Length;
		}
		public char GetChar(int index) => (char) value;
		public long GetChars(int index, long fieldoffset, char[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
		public Guid GetGuid(int index) => (Guid) value;
		public short GetInt16(int index) => (short) value;
		public int GetInt32(int index) => (int) value;
		public long GetInt64(int index) => (long) value;
		public float GetFloat(int index) => (float) value;
		public double GetDouble(int index) => (double) value;
		public string GetString(int index) => (string) value;
		public decimal GetDecimal(int index) => (decimal) value;
		public DateTime GetDateTime(int index) => (DateTime) value;
		public IDataReader GetData(int index) => throw new NotSupportedException();
		public bool IsDBNull(int index) => value is DBNull;
	}

#pragma warning disable CA1801, SA1313
	private sealed record ItemRecord(string? TheText, long TheInteger, double TheReal, byte[]? TheBlob, long TheOptionalInteger = 42);
#pragma warning restore CA1801, SA1313

	private enum Answer
	{
		FortyTwo = 42,
	}

	private readonly struct NameValue
	{
		public NameValue(string name, string value) => (Name, Value) = (name, value);
		public string Name { get; init; }
		public string Value { get; init; }
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
