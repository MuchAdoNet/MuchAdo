using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
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
			.Command("select TheText, TheInteger, TheReal, TheBlob from items;")
			.Query(
				record =>
				{
					if (index++ == 0)
						record.Get<string>(0, 1).Should().Be(s_dto.TheText);
					else
						record.Get<string>(0, 1).Should().BeNull();
					return 1;
				})
			.Sum().Should().Be(2);
	}

	[Test]
	public void NonNullableScalars()
	{
		using var connector = GetConnectorWithItems();
		var index = 0;
		connector
			.Command("select TheText, TheInteger, TheReal, TheBlob from items;")
			.Query(
				record =>
				{
					if (index++ == 0)
					{
						record.Get<long>(1, 1).Should().Be(s_dto.TheInteger);
						record.Get<double>(2, 1).Should().Be(s_dto.TheReal);
					}
					else
					{
						Invoking(() => record.Get<long>(1, 1)).Should().Throw<InvalidOperationException>();
						Invoking(() => record.Get<double>(2, 1)).Should().Throw<InvalidOperationException>();
					}
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
			.Command("select TheText, TheInteger, TheReal, TheBlob from items;")
			.Query(
				record =>
				{
					if (index++ == 0)
					{
						record.Get<long?>(1, 1).Should().Be(s_dto.TheInteger);
						record.Get<double?>(2, 1).Should().Be(s_dto.TheReal);
					}
					else
					{
						record.Get<long?>(1, 1).Should().BeNull();
						record.Get<double?>(2, 1).Should().BeNull();
					}
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
			.Command("select TheText, TheInteger, TheReal, TheBlob from items;")
			.Query(
				record =>
				{
					if (index++ == 0)
					{
						record.Get<Answer>(1, 1).Should().Be(Answer.FortyTwo);
						record.Get<Answer?>(1, 1).Should().Be(Answer.FortyTwo);
					}
					else
					{
						Invoking(() => record.Get<Answer>(1, 1)).Should().Throw<InvalidOperationException>();
						record.Get<Answer?>(1, 1).Should().BeNull();
					}
					return 1;
				})
			.Sum().Should().Be(2);
	}

	[Test]
	public void BadIndexCount()
	{
		using var connector = GetConnectorWithItems();
		connector
			.Command("select TheText, TheInteger, TheReal, TheBlob from items;")
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
			.Command("select TheText, TheInteger, TheReal, TheBlob from items;")
			.QueryFirst(
				record =>
				{
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
	public void ByteArrayTests()
	{
		using var connector = GetConnectorWithItems();
		var index = 0;
		connector
			.Command("select TheText, TheInteger, TheReal, TheBlob from items;")
			.Query(
				record =>
				{
					if (index++ == 0)
						record.Get<byte[]>(3, 1).Should().Equal(s_dto.TheBlob);
					else
						record.Get<byte[]>(3, 1).Should().BeNull();
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
			.Command("select TheText, TheInteger, TheReal, TheBlob from items;")
			.Query(
				record =>
				{
					if (index++ == 0)
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
					return 1;
				})
			.Sum().Should().Be(2);
	}

	[Test]
	public void TupleTests([Values(2, 3, 4, 5, 6, 7, 8, 9, 10)] int fieldCount)
	{
		using var connector = new DbConnector(new SqliteConnection("Data Source=:memory:"));
		var columns = string.Join(", ", Enumerable.Range(1, fieldCount).Select(x => $"Item{x}"));
		connector
			.Command($"""
				create table Tuples ({columns});
				insert into Tuples ({columns}) values ({string.Join(", ", Enumerable.Range(1, fieldCount).Select(x => x))});
				insert into Tuples ({columns}) values ({string.Join(", ", Enumerable.Range(1, fieldCount).Select(_ => "null"))});
				""")
			.Execute();

		var index = 0;
		connector
			.Command("select * from Tuples;")
			.Query(
				record =>
				{
					if (index++ == 0)
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
					return 1;
				})
			.Sum().Should().Be(2);
	}

	[Test]
	public void DtoTests()
	{
		using var connector = GetConnectorWithItems();
		var index = 0;
		connector
			.Command("select TheText, TheInteger, TheReal, TheBlob from items;")
			.Query(
				record =>
				{
					if (index++ == 0)
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
			.Command("select TheText, TheInteger, null, TheReal, TheBlob from items;")
			.Query(
				record =>
				{
					if (index++ == 0)
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
			.Command("select TheText, TheInteger from items;")
			.Query(
				record =>
				{
					if (index++ == 0)
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
			.Command("select TheText, TheInteger, TheReal, TheBlob from items;")
			.Query(
				record =>
				{
					if (index++ == 0)
					{
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
					}
					else
					{
						record.Get<ItemRecord>(0, 4).Should().BeNull();
					}
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
			.Command("select thetext, THEinteger from items;")
			.Query(
				record =>
				{
					if (index++ == 0)
					{
						record.Get<ItemDto>(0, 2)
							.Should().BeEquivalentTo(new ItemDto(s_dto.TheText) { TheInteger = s_dto.TheInteger });
					}
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
			.Command("select TheText as the_text, TheInteger as the_integer from items;")
			.Query(
				record =>
				{
					if (index++ == 0)
					{
						record.Get<ItemDto>(0, 2)
							.Should().BeEquivalentTo(new ItemDto(s_dto.TheText) { TheInteger = s_dto.TheInteger });
					}
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
			.Command("select TheText, TheInteger as Nope from items;")
			.Query(
				record =>
				{
					if (index++ == 0)
					{
						if (ignore)
							record.Get<ItemDto>(0, 2).Should().BeEquivalentTo(new ItemDto(s_dto.TheText));
						else
							Invoking(() => record.Get<ItemDto>(0, 2)).Should().Throw<InvalidOperationException>();
					}
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
			.Command("select TheText, TheInteger, TheReal, TheBlob from items;")
			.Query(
				record =>
				{
					if (index++ == 0)
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
			.Command("select TheText, TheInteger, TheReal, TheBlob from items;")
			.Query(
				record =>
				{
					if (index++ == 0)
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
			.Command("select TheText, TheInteger, TheReal, TheBlob from items;")
			.Query(
				record =>
				{
					if (index++ == 0)
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
					}
					else
					{
						// all nulls returns null dynamic
						record.Get<object>(0).Should().BeNull();
						((object) record.Get<dynamic>(0)).Should().BeNull();
					}
					return 1;
				})
			.Sum().Should().Be(2);
	}

	[Test]
	public void GetExtension()
	{
		using var connector = GetConnectorWithItems();
		connector
			.Command("select TheText, TheInteger, TheReal, TheBlob from items;")
			.QueryFirst(
				record =>
				{
					record.Get<ItemDto>().Should().BeEquivalentTo(s_dto);
					return 1;
				})
			.Should().Be(1);
	}

	[Test]
	public void GetAtExtension()
	{
		using var connector = GetConnectorWithItems();
		connector
			.Command("select TheText, TheInteger, TheReal, TheBlob from items;")
			.QueryFirst(
				record =>
				{
					record.Get<long>(1).Should().Be(s_dto.TheInteger);
					record.Get<long>("TheInteger").Should().Be(s_dto.TheInteger);
#if !NET472
					record.Get<long>(^3).Should().Be(s_dto.TheInteger);
#endif
					return 1;
				})
			.Should().Be(1);
	}

	[Test]
	public void GetRangeExtension()
	{
		using var connector = GetConnectorWithItems();
		connector
			.Command("select TheText, TheInteger, TheReal, TheBlob from items;")
			.QueryFirst(
				record =>
				{
					record.Get<(long, double)>(1, 2).Should().Be((s_dto.TheInteger, s_dto.TheReal));
					record.Get<(long, double)>("TheInteger", 2).Should().Be((s_dto.TheInteger, s_dto.TheReal));
					record.Get<(long, double)>("TheInteger", "TheReal").Should().Be((s_dto.TheInteger, s_dto.TheReal));
#if !NET472
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
			.Command("select TheText, TheInteger, TheReal, TheBlob from items;")
			.Query(
				record =>
				{
					if (index++ == 0)
						record.Get<CustomColumnDto>(0, 1).Should().BeEquivalentTo(new CustomColumnDto { Text = s_dto.TheText });
					else
						record.Get<CustomColumnDto>(0, 1).Should().BeNull();
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
			.Command("create table Items (TheText text null, TheInteger integer null, TheReal real null, TheBlob blob null);")
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
