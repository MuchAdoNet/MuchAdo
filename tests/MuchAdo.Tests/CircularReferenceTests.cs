using FluentAssertions;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace MuchAdo.Tests;

[TestFixture]
internal sealed class CircularReferenceTests
{
	[Test]
	public void SelfReferentialDtoCanMapSelectedProperties()
	{
		using var connector = new DbConnector(new SqliteConnection("Data Source=:memory:"));
		connector
			.Command("""
				create table Nodes (Name text not null);
				insert into Nodes (Name) values ('root');
				""")
			.Execute();

		var node = connector.Command("select Name from Nodes;").QueryFirst<TreeNode>();

		node.Name.Should().Be("root");
		node.Parent.Should().BeNull();
	}

	[Test]
	public void MutuallyReferentialDtoCanMapSelectedProperties()
	{
		using var connector = new DbConnector(new SqliteConnection("Data Source=:memory:"));
		connector
			.Command("""
				create table Orders (OrderId integer not null);
				insert into Orders (OrderId) values (42);
				""")
			.Execute();

		var order = connector.Command("select OrderId from Orders;").QueryFirst<OrderDto>();

		order.OrderId.Should().Be(42);
		order.Customer.Should().BeNull();
	}

	[Test]
	public void MutuallyReferentialDtosCanMapOrderCustomerLastOrder()
	{
		using var connector = new DbConnector(new SqliteConnection("Data Source=:memory:"));
		connector
			.Command("""
				create table Orders (OrderId integer not null, CustomerId integer not null);
				insert into Orders (OrderId, CustomerId) values (42, 7);
				""")
			.Execute();

		var result = connector
			.Command("""
				select OrderId, null, CustomerId, null, OrderId
				from Orders;
				""")
			.QueryFirst<(OrderDto Order, CustomerDto Customer, OrderDto LastOrder)>();

		result.Order.OrderId.Should().Be(42);
		result.Order.Customer.Should().BeNull();
		result.Customer.CustomerId.Should().Be(7);
		result.Customer.LastOrder.Should().BeNull();
		result.LastOrder.OrderId.Should().Be(42);
		result.LastOrder.Customer.Should().BeNull();
	}

	[Test]
	public void SelectedSelfReferentialPropertyThrowsMeaningfulException()
	{
		using var connector = new DbConnector(new SqliteConnection("Data Source=:memory:"));

		Invoking(() => connector.Command("select 1 as Parent;").QueryFirst<TreeNode>())
			.Should().Throw<InvalidOperationException>()
			.WithMessage("*Circular reference*Parent*TreeNode*");
	}

	private sealed class TreeNode
	{
		public string? Name { get; set; }
		public TreeNode? Parent { get; set; }
	}

	private sealed class OrderDto
	{
		public long OrderId { get; set; }
		public CustomerDto? Customer { get; set; }
	}

	private sealed class CustomerDto
	{
		public long CustomerId { get; set; }
		public OrderDto? LastOrder { get; set; }
	}
}
