// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Apm.Api;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.OpenTelemetry.Tests;

/// <summary>
/// Verifies that the OTel bridge correctly maps database activity attributes to the Elastic APM span model
/// (span.context.db.*, span.context.destination.*) for both old and new OTel semantic conventions.
/// </summary>
[Collection("OpenTelemetry")]
public class OTelDbSpanTests
{
	private static ApmAgent CreateAgent(MockPayloadSender payloadSender) =>
		new(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716,
			configuration: new MockConfiguration(openTelemetryBridgeEnabled: "true")));

	/// <summary>
	/// Old OTel semconv: db.system + db.name + db.statement + net.peer.name/port.
	/// Used by many older instrumentation libraries.
	/// </summary>
	[Fact]
	public void OldConvention_DbSystem_MapsDbContextAndDestination()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = CreateAgent(payloadSender))
		{
			var transaction = agent.Tracer.StartTransaction("test", "test");
			var src = new ActivitySource("Test.OldDbConvention");
			using (var activity = src.StartActivity("db-op", ActivityKind.Client))
			{
				activity!.SetTag("db.system", "mysql");
				activity.SetTag("db.name", "mydb");
				activity.SetTag("db.statement", "SELECT 1");
				activity.SetTag("net.peer.name", "db-host");
				activity.SetTag("net.peer.port", "3306");
			}
			transaction.End();
		}

		var span = payloadSender.FirstSpan;
		span.Type.Should().Be(ApiConstants.TypeDb);
		span.Subtype.Should().Be("mysql");

		span.Context.Db.Should().NotBeNull();
		span.Context.Db.Type.Should().Be("mysql");
		span.Context.Db.Instance.Should().Be("mydb");
		span.Context.Db.Statement.Should().Be("SELECT 1");

		span.Context.Destination.Should().NotBeNull();
		span.Context.Destination.Address.Should().Be("db-host");
		span.Context.Destination.Port.Should().Be(3306);
	}

	/// <summary>
	/// New OTel semconv: db.system.name + db.namespace + db.query.text + server.address/port.
	/// Used by MongoDB.Driver 3.7.0+ and libraries following the current OTel spec.
	/// </summary>
	[Fact]
	public void NewConvention_DbSystemName_MapsDbContextAndDestination()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = CreateAgent(payloadSender))
		{
			var transaction = agent.Tracer.StartTransaction("test", "test");
			var src = new ActivitySource("Test.NewDbConvention");
			using (var activity = src.StartActivity("db-op", ActivityKind.Client))
			{
				activity!.SetTag("db.system.name", "mongodb");
				activity.SetTag("db.namespace", "mydb");
				activity.SetTag("db.query.text", "{ \"find\" : \"things\" }");
				activity.SetTag("server.address", "mongo-host");
				activity.SetTag("server.port", 27017); // int tag, as MongoDB.Driver emits it
			}
			transaction.End();
		}

		var span = payloadSender.FirstSpan;
		span.Type.Should().Be(ApiConstants.TypeDb);
		span.Subtype.Should().Be("mongodb");

		span.Context.Db.Should().NotBeNull();
		span.Context.Db.Type.Should().Be("mongodb");
		span.Context.Db.Instance.Should().Be("mydb");
		span.Context.Db.Statement.Should().Be("{ \"find\" : \"things\" }");

		span.Context.Destination.Should().NotBeNull();
		span.Context.Destination.Address.Should().Be("mongo-host");
		span.Context.Destination.Port.Should().Be(27017);
	}

	/// <summary>
	/// db.system.name must be recognised even when db.system is absent. Without the fix,
	/// the span type would be "unknown" and no db context would be set.
	/// </summary>
	[Fact]
	public void NewConvention_DbSystemName_WithoutLegacyDbSystem_IsRecognisedAsDbSpan()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = CreateAgent(payloadSender))
		{
			var transaction = agent.Tracer.StartTransaction("test", "test");
			var src = new ActivitySource("Test.DbSystemName");
			using (var activity = src.StartActivity("db-op", ActivityKind.Client))
			{
				activity!.SetTag("db.system.name", "postgresql");
				// intentionally no db.system
			}
			transaction.End();
		}

		var span = payloadSender.FirstSpan;
		span.Type.Should().Be(ApiConstants.TypeDb);
		span.Subtype.Should().Be("postgresql");
		span.Context.Db.Should().NotBeNull();
		span.Context.Db.Type.Should().Be("postgresql");
	}

	/// <summary>
	/// db.namespace is used as the database instance name when db.name is absent.
	/// For MongoDB, db.namespace contains just the database name per the OTel MongoDB spec.
	/// </summary>
	[Fact]
	public void NewConvention_DbNamespace_UsedAsInstanceWhenDbNameAbsent()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = CreateAgent(payloadSender))
		{
			var transaction = agent.Tracer.StartTransaction("test", "test");
			var src = new ActivitySource("Test.DbNamespace");
			using (var activity = src.StartActivity("db-op", ActivityKind.Client))
			{
				activity!.SetTag("db.system.name", "mongodb");
				activity.SetTag("db.namespace", "orders");
				// intentionally no db.name
			}
			transaction.End();
		}

		var span = payloadSender.FirstSpan;
		span.Context.Db.Should().NotBeNull();
		span.Context.Db.Instance.Should().Be("orders");
		span.Context.Service.Target.Name.Should().Be("orders");
		span.Context.Destination.Service.Resource.Should().Be("mongodb/orders");
	}

	/// <summary>
	/// For systems where db.namespace has composite semantics (PostgreSQL: "{db}|{schema}",
	/// SQL Server: "{instance}|{db}", Elasticsearch: cluster name), db.namespace must NOT be
	/// used as span.db.instance. Only db.name should be used, to avoid incorrect service target grouping.
	/// </summary>
	[Theory]
	[InlineData("elasticsearch", "my-cluster", "logs-index", "logs-index")]  // db.namespace = cluster name — ignored; db.name used
	[InlineData("postgresql", "mydb|public", "mydb", "mydb")]        // db.namespace = composite "{db}|{schema}" — ignored; db.name used
	[InlineData("mssql", "inst1|mydb", "mydb", "mydb")]        // db.namespace = composite "{instance}|{db}" — ignored; db.name used
	public void ExcludedSystem_DbNamespace_NotUsedAsInstance_DbNameUsedInstead(
		string dbSystem, string dbNamespace, string dbName, string expectedInstance)
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = CreateAgent(payloadSender))
		{
			var transaction = agent.Tracer.StartTransaction("test", "test");
			var src = new ActivitySource("Test.ExcludedDbNamespace");
			using (var activity = src.StartActivity("db-op", ActivityKind.Client))
			{
				activity!.SetTag("db.system.name", dbSystem);
				activity.SetTag("db.namespace", dbNamespace);
				activity.SetTag("db.name", dbName);
			}
			transaction.End();
		}

		var span = payloadSender.FirstSpan;
		span.Context.Db.Instance.Should().Be(expectedInstance);
		span.Context.Service.Target.Name.Should().Be(expectedInstance);
	}

	/// <summary>
	/// For systems in the approved set (mongodb, mysql, cassandra, azure.cosmosdb), db.namespace
	/// takes precedence over db.name when both are present.
	/// </summary>
	[Theory]
	[InlineData("mongodb", "ns-value", "name-value")]
	[InlineData("mysql", "ns-value", "name-value")]
	[InlineData("cassandra", "ns-value", "name-value")]
	[InlineData("azure.cosmosdb", "ns-value", "name-value")]  // normalized to "cosmosdb" before approved-systems check
	[InlineData("cosmosdb", "ns-value", "name-value")]
	public void ApprovedSystem_DbNamespace_TakesPrecedenceOverDbName(
		string dbSystem, string dbNamespace, string dbName)
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = CreateAgent(payloadSender))
		{
			var transaction = agent.Tracer.StartTransaction("test", "test");
			var src = new ActivitySource("Test.ApprovedDbNamespace");
			using (var activity = src.StartActivity("db-op", ActivityKind.Client))
			{
				activity!.SetTag("db.system.name", dbSystem);
				activity.SetTag("db.namespace", dbNamespace);
				activity.SetTag("db.name", dbName);
			}
			transaction.End();
		}

		payloadSender.FirstSpan.Context.Db.Instance.Should().Be(dbNamespace);
	}

	/// <summary>
	/// server.port emitted as an int tag (as MongoDB.Driver does) must be parsed correctly
	/// into span.context.destination.port.
	/// </summary>
	[Fact]
	public void ServerPort_AsIntTag_IsMappedToDestinationPort()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = CreateAgent(payloadSender))
		{
			var transaction = agent.Tracer.StartTransaction("test", "test");
			var src = new ActivitySource("Test.IntPort");
			using (var activity = src.StartActivity("db-op", ActivityKind.Client))
			{
				activity!.SetTag("db.system.name", "mongodb");
				activity.SetTag("server.address", "localhost");
				activity.SetTag("server.port", 27017); // int, not string
			}
			transaction.End();
		}

		payloadSender.FirstSpan.Context.Destination.Port.Should().Be(27017);
	}

	/// <summary>
	/// network.peer.address / network.peer.port are the stable OTel network attributes.
	/// They must be recognised as destination address/port when the server.* attributes are absent.
	/// </summary>
	[Fact]
	public void NetworkPeer_AttributesUsedForDestination_WhenServerAttributesAbsent()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = CreateAgent(payloadSender))
		{
			var transaction = agent.Tracer.StartTransaction("test", "test");
			var src = new ActivitySource("Test.NetworkPeer");
			using (var activity = src.StartActivity("db-op", ActivityKind.Client))
			{
				activity!.SetTag("db.system.name", "postgresql");
				activity.SetTag("network.peer.address", "pg-host");
				activity.SetTag("network.peer.port", "5432");
			}
			transaction.End();
		}

		var span = payloadSender.FirstSpan;
		span.Context.Destination.Should().NotBeNull();
		span.Context.Destination.Address.Should().Be("pg-host");
		span.Context.Destination.Port.Should().Be(5432);
	}

	/// <summary>
	/// Full tag set matching what MongoDB.Driver 3.7.0+ actually emits — serves as an end-to-end
	/// regression guard for the bridge against the real driver output.
	/// </summary>
	[Fact]
	public void MongoDbDriver370_RealisticTagSet_MapsCorrectly()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = CreateAgent(payloadSender))
		{
			var transaction = agent.Tracer.StartTransaction("test", "test");
			var src = new ActivitySource("MongoDB.Driver");
			using (var activity = src.StartActivity("insert mydb.things", ActivityKind.Client))
			{
				activity!.SetTag("db.system.name", "mongodb");
				activity.SetTag("db.namespace", "mydb");
				activity.SetTag("db.collection.name", "things");
				activity.SetTag("db.operation.name", "insert");
				activity.SetTag("db.query.text", "{ \"insert\" : \"things\" }");
				activity.SetTag("db.query.summary", "insert mydb.things");
				activity.SetTag("server.address", "localhost");
				activity.SetTag("server.port", 27017);
			}
			transaction.End();
		}

		var span = payloadSender.FirstSpan;
		span.Type.Should().Be(ApiConstants.TypeDb);
		span.Subtype.Should().Be("mongodb");
		span.Action.Should().Be("query");

		span.Context.Db.Should().NotBeNull();
		span.Context.Db.Type.Should().Be("mongodb");
		span.Context.Db.Instance.Should().Be("mydb");
		span.Context.Db.Statement.Should().Be("{ \"insert\" : \"things\" }");

		span.Context.Destination.Should().NotBeNull();
		span.Context.Destination.Address.Should().Be("localhost");
		span.Context.Destination.Port.Should().Be(27017);
		span.Context.Destination.Service.Resource.Should().Be("mongodb/mydb");

		span.Context.Service.Target.Type.Should().Be("mongodb");
		span.Context.Service.Target.Name.Should().Be("mydb");
	}

	/// <summary>
	/// OTel uses "azure.cosmosdb" as the db.system.name value for Azure Cosmos DB.
	/// The ECS-mapped fields (span.subtype, service.target.type) must be normalized to "cosmosdb"
	/// so APM server / Kibana renders it consistently with the native CosmosDB integration.
	/// The raw OTel value must be preserved in otel.attributes.
	/// </summary>
	[Fact]
	public void AzureCosmosDb_OTelSystemName_NormalizedToCosmosDbSubtype_RawValuePreservedInOtelAttributes()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = CreateAgent(payloadSender))
		{
			var transaction = agent.Tracer.StartTransaction("test", "test");
			var src = new ActivitySource("Test.CosmosDbNormalization");
			using (var activity = src.StartActivity("db-op", ActivityKind.Client))
			{
				activity!.SetTag("db.system.name", "azure.cosmosdb");
				activity.SetTag("db.namespace", "ShopDb");
			}
			transaction.End();
		}

		var span = payloadSender.FirstSpan;
		// ECS fields: normalized to "cosmosdb" for APM server / Kibana compatibility
		span.Subtype.Should().Be("cosmosdb");
		span.Context.Service.Target.Type.Should().Be("cosmosdb");
		// Raw OTel attribute preserved as emitted by the instrumentation library
		span.Otel.Attributes["db.system.name"].Should().Be("azure.cosmosdb");
	}

	/// <summary>
	/// All DB spans get span.action = "query" regardless of the specific operation.
	/// This matches the behaviour of the NuGet MongoDB, Redis and CosmosDb integrations.
	/// </summary>
	[Theory]
	[InlineData("db.system", "mysql")]
	[InlineData("db.system.name", "mongodb")]
	[InlineData("db.system.name", "redis")]
	public void DbSpan_ActionIsAlwaysQuery(string systemKey, string systemValue)
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = CreateAgent(payloadSender))
		{
			var transaction = agent.Tracer.StartTransaction("test", "test");
			var src = new ActivitySource("Test.DbAction");
			using (var activity = src.StartActivity("db-op", ActivityKind.Client))
				activity!.SetTag(systemKey, systemValue);
			transaction.End();
		}

		payloadSender.FirstSpan.Action.Should().Be("query");
	}

	/// <summary>
	/// When no server address/port attributes are present, destination address and port are not set.
	/// </summary>
	[Fact]
	public void NoAddressAttributes_DestinationAddressAndPortNotSet()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = CreateAgent(payloadSender))
		{
			var transaction = agent.Tracer.StartTransaction("test", "test");
			var src = new ActivitySource("Test.NoAddr");
			using (var activity = src.StartActivity("db-op", ActivityKind.Client))
				activity!.SetTag("db.system", "redis");
			transaction.End();
		}

		var span = payloadSender.FirstSpan;
		span.Type.Should().Be(ApiConstants.TypeDb);
		if (span.Context.Destination != null)
		{
			span.Context.Destination.Address.Should().BeNullOrEmpty();
			span.Context.Destination.Port.Should().BeNull();
		}
	}
}
