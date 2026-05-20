// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Apm.Api;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.XUnit;
using FluentAssertions;
using OpenTelemetrySample;
using Xunit;

namespace Elastic.Apm.OpenTelemetry.Tests;

public class OpenTelemetryTests
{
	[Fact]
	public void MixApisTest1()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716,
				   configuration: new MockConfiguration(openTelemetryBridgeEnabled: "true"))))
			OTSamples.Sample2(agent.Tracer);

		payloadSender.FirstTransaction.Name.Should().Be("Sample2");
		payloadSender.Spans.Should().HaveCount(2);

		payloadSender.FirstSpan.Name.Should().Be("foo");
		payloadSender.Spans.ElementAt(1).Name.Should().Be("ElasticApmSpan");

		payloadSender.FirstSpan.ParentId.Should().Be(payloadSender.FirstTransaction.Id);
		payloadSender.Spans.ElementAt(1).ParentId.Should().Be(payloadSender.FirstTransaction.Id);

		AssertOnTraceIds(payloadSender);
	}

	private void AssertOnTraceIds(MockPayloadSender payloadSender)
	{
		foreach (var span in payloadSender.Spans)
			span.TraceId.Should().Be(payloadSender.FirstTransaction.TraceId);
	}

	[Fact]
	public void MixApisTest2()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716,
				   configuration: new MockConfiguration(openTelemetryBridgeEnabled: "true"))))
			OTSamples.Sample3(agent.Tracer);

		payloadSender.FirstTransaction.Name.Should().Be("Sample3");
		payloadSender.Spans.Should().HaveCount(2);

		payloadSender.FirstSpan.Name.Should().Be("ElasticApmSpan");
		payloadSender.Spans.ElementAt(1).Name.Should().Be("foo");

		payloadSender.Spans.ElementAt(1).ParentId.Should().Be(payloadSender.FirstTransaction.Id);
		payloadSender.FirstSpan.ParentId.Should().Be(payloadSender.Spans.ElementAt(1).Id);

		AssertOnTraceIds(payloadSender);
	}

	[DisabledTestFact(
		"Sometimes fails in CI with 'Expected payloadSender.FirstTransaction.Name to be `Sample4` with a length of 7, "
		+ "but`UnitTestActivity` has a length of 16, differs near `Uni` (index 0).'")]
	public void MixApisTest3()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716,
				   configuration: new MockConfiguration(openTelemetryBridgeEnabled: "true"))))
			OTSamples.Sample4(agent.Tracer);

		payloadSender.FirstTransaction.Name.Should().Be("Sample4");
		payloadSender.Spans.Should().HaveCount(2);

		payloadSender.FirstSpan.Name.Should().Be("ElasticApmSpan");
		payloadSender.Spans.ElementAt(1).Name.Should().Be("foo");

		payloadSender.Spans.ElementAt(1).ParentId.Should().Be(payloadSender.FirstTransaction.Id);
		payloadSender.FirstSpan.ParentId.Should().Be(payloadSender.Spans.ElementAt(1).Id);

		AssertOnTraceIds(payloadSender);
	}

	[Fact]
	public void TestOtelFieldsWith1Span()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716,
				   configuration: new MockConfiguration(openTelemetryBridgeEnabled: "true"))))
			OTSamples.OneSpanWithAttributes();

		payloadSender.FirstTransaction.Name.Should().Be("foo");
		payloadSender.FirstTransaction.Otel.Should().NotBeNull();
		payloadSender.FirstTransaction.Otel.SpanKind.Should().Be("Server");
		payloadSender.FirstTransaction.Otel.Attributes.Should().NotBeNull();
		payloadSender.FirstTransaction.Otel.Attributes.Should().Contain("foo", "bar");
	}

	[Fact]
	public void TestOtelFieldsWith3Spans()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716,
				   configuration: new MockConfiguration(openTelemetryBridgeEnabled: "true"))))
			OTSamples.TwoSpansWithAttributes();

		payloadSender.FirstTransaction.Name.Should().Be("foo");
		payloadSender.FirstTransaction.Otel.Should().NotBeNull();
		payloadSender.FirstTransaction.Otel.SpanKind.Should().Be("Server");
		payloadSender.FirstTransaction.Otel.Attributes.Should().NotBeNull();
		payloadSender.FirstTransaction.Otel.Attributes.Should().Contain("foo1", "bar1");

		payloadSender.FirstSpan.Name.Should().Be("bar");
		payloadSender.FirstSpan.Otel.Should().NotBeNull();
		payloadSender.FirstSpan.Otel.SpanKind.Should().Be("Internal");
		payloadSender.FirstSpan.Otel.Attributes.Should().NotBeNull();
		payloadSender.FirstSpan.Otel.Attributes.Should().Contain("foo2", "bar2");
	}

	[Fact]
	public void SpanKindTests()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716,
				   configuration: new MockConfiguration(openTelemetryBridgeEnabled: "true"))))
			OTSamples.SpanKindSample();

		payloadSender.FirstSpan.Type.Should().Be(ApiConstants.TypeExternal);
		payloadSender.FirstSpan.Subtype.Should().Be(ApiConstants.SubtypeHttp);

		payloadSender.Spans.ElementAt(1).Type.Should().Be(ApiConstants.TypeDb);
		payloadSender.Spans.ElementAt(1).Subtype.Should().Be("mysql");

		payloadSender.Spans.ElementAt(2).Type.Should().Be(ApiConstants.TypeExternal);
		payloadSender.Spans.ElementAt(2).Subtype.Should().Be("grpc");

		payloadSender.Spans.ElementAt(3).Type.Should().Be(ApiConstants.TypeMessaging);
		payloadSender.Spans.ElementAt(3).Subtype.Should().Be("rabbitmq");
	}

	[Fact]
	public void DisableOTelBridgeTest()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716,
				   configuration: new MockConfiguration(openTelemetryBridgeEnabled: "false"))))
			OTSamples.Sample1();

		payloadSender.WaitForTransactions(TimeSpan.FromSeconds(5));
		payloadSender.Transactions.Should().BeNullOrEmpty();
	}

	[Fact]
	public void SpanLinkTest()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716,
				   configuration: new MockConfiguration(openTelemetryBridgeEnabled: "true"))))
			OTSamples.SpanLinkSample();

		payloadSender.WaitForTransactions(count: 3);

		(payloadSender.Transactions[2] as Transaction)!.Links.Should().NotBeNullOrEmpty();
		(payloadSender.Transactions[2] as Transaction)!.Links.ElementAt(0).SpanId.Should().Be(payloadSender.Transactions[0].Id);
	}

	[Fact]
	public void DistributedTracingTest()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716,
				   configuration: new MockConfiguration(openTelemetryBridgeEnabled: "true"))))
			OTSamples.DistributedTraceSample();


		payloadSender.WaitForTransactions(count: 2);

		payloadSender.FirstTransaction.TraceId.Should()
			.Be(payloadSender.Transactions[1].TraceId, because: "The transactions should be under the same trace.");
	}

	/// <summary>
	/// Make sure that the traceId on a root activity is the same as the traceId on the transaction which the bridge creates from the root activity.
	/// </summary>
	[Fact]
	public void ActivityAndTransactionTraceIdSynced()
	{
		var payloadSender = new MockPayloadSender();
		using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716,
			configuration: new MockConfiguration(openTelemetryBridgeEnabled: "true")));
		var src = new ActivitySource("Test");
		string? traceId;
		using (var activity = src.StartActivity("foo", ActivityKind.Server))
			traceId = activity?.TraceId.ToString();
		traceId.Should().NotBeNull();
		payloadSender.FirstTransaction.TraceId.Should().Be(traceId);
	}

	[Fact]
	public void ResourceIsRequiredWhenSpanDestinationServiceIsNotNull()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716,
				   configuration: new MockConfiguration(openTelemetryBridgeEnabled: "true"))))
			OTSamples.TwoSpansWithAttributes();

		payloadSender.Spans.Should()
			.NotContain(span =>
					span.Context.Destination != null
					&& span.Context.Destination.Service != null
					&& span.Context.Destination.Service.Resource == null,
				"Resource is required in Destination Service");
	}

	[Fact]
	public void AzureFunctionsWorkerActivitiesAreNotCaptured()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			var src = new ActivitySource("Microsoft.Azure.Functions.Worker");
			using (src.StartActivity("SomeFunction"))
			{ }
		}
		payloadSender.Transactions.Should().BeEmpty();
		payloadSender.Spans.Should().BeEmpty();
	}

	[Fact]
	public void AzureFunctionsInvokeFunctionAsyncWithEmptySourceIsNotCaptured()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			var src = new ActivitySource(string.Empty);
			using (src.StartActivity("InvokeFunctionAsync"))
			{ }
		}
		payloadSender.Transactions.Should().BeEmpty();
		payloadSender.Spans.Should().BeEmpty();
	}

	[Fact]
	public void OtelAttributesAreCappedAt128()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			var src = new ActivitySource("Test.AttributeCap");
			using (var activity = src.StartActivity("root", ActivityKind.Server))
			{
				for (var i = 0; i < 130; i++)
					activity?.SetTag($"key{i}", $"value{i}");
			}
		}
		payloadSender.WaitForTransactions();
		payloadSender.FirstTransaction.Otel.Attributes.Should().HaveCount(128);
	}

	[Fact]
	public void OtelAttributeStringValuesAreTruncatedAt10000Chars()
	{
		var payloadSender = new MockPayloadSender();
		var longValue = new string('x', 15_000);
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			var src = new ActivitySource("Test.Truncation");
			using (var activity = src.StartActivity("root", ActivityKind.Server))
				activity?.SetTag("long-key", longValue);
		}
		payloadSender.WaitForTransactions();
		payloadSender.FirstTransaction.Otel.Attributes.Should().ContainKey("long-key");
		((string)payloadSender.FirstTransaction.Otel.Attributes["long-key"]).Should().HaveLength(10_000);
	}

	/// <summary>
	/// Baseline: activities from the given source ARE captured when no deduplication flag is set,
	/// confirming the test setup is valid before testing the dedup path.
	/// </summary>
	[Theory]
	[InlineData("MongoDB.Driver")]
	[InlineData("Grpc.Net.Client")]
	public void ActivitiesAreCapturedWithoutDeduplicationFlag(string sourceName)
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			var src = new ActivitySource(sourceName);
			using (src.StartActivity("operation", ActivityKind.Client))
			{ }
		}
		payloadSender.WaitForTransactions();
		payloadSender.Transactions.Should().HaveCount(1);
	}

	[Theory]
	[InlineData("MongoDB.Driver", "Elastic.Apm.MongoDb")]
	[InlineData("Grpc.Net.Client", "Elastic.Apm.GrpcClient")]
	public void ActivitiesAreSkippedWhenSourceNameMatchesInstrumentationPackage(string sourceName, string packageAssemblyName)
	{
		var payloadSender = new MockPayloadSender();
		var components = new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716);
		using (new ApmAgent(components))
		{
			components.ElasticActivityListener.CheckAssembly(packageAssemblyName);
			var src = new ActivitySource(sourceName);
			using (src.StartActivity("operation", ActivityKind.Client))
			{ }
		}
		payloadSender.Transactions.Should().BeEmpty();
		payloadSender.Spans.Should().BeEmpty();
	}

	/// <summary>
	/// Baseline: Azure service activities tagged with az.namespace ARE captured when no dedup flag is set.
	/// </summary>
	[Theory]
	[InlineData("Microsoft.ServiceBus")]
	[InlineData("Microsoft.Storage")]
	[InlineData("Microsoft.DocumentDB")]
	public void AzureServiceActivitiesAreCapturedWithoutDeduplicationFlag(string azNamespace)
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			var src = new ActivitySource("Azure.Test.Source");
			var tags = new ActivityTagsCollection { ["az.namespace"] = azNamespace };
			using (src.StartActivity("operation", ActivityKind.Client, default(ActivityContext), tags))
			{ }
		}
		payloadSender.WaitForTransactions();
		payloadSender.Transactions.Should().HaveCount(1);
	}

	[Theory]
	[InlineData("Microsoft.ServiceBus", "Elastic.Apm.Azure.ServiceBus")]
	[InlineData("Microsoft.Storage", "Elastic.Apm.Azure.Storage")]
	[InlineData("Microsoft.DocumentDB", "Elastic.Apm.Azure.CosmosDb")]
	public void AzureServiceActivitiesAreSkippedWhenInstrumentationPackageIsDetected(string azNamespace, string packageAssemblyName)
	{
		var payloadSender = new MockPayloadSender();
		var components = new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716);
		using (new ApmAgent(components))
		{
			components.ElasticActivityListener.CheckAssembly(packageAssemblyName);
			var src = new ActivitySource("Azure.Test.Source");
			var tags = new ActivityTagsCollection { ["az.namespace"] = azNamespace };
			using (src.StartActivity("operation", ActivityKind.Client, default(ActivityContext), tags))
			{ }
		}
		payloadSender.Transactions.Should().BeEmpty();
		payloadSender.Spans.Should().BeEmpty();
	}

	/// <summary>
	/// Activities whose OperationName matches a known internal listener (ASP.NET Core, System.Net.Http, Elastic APM's own
	/// transaction activity) must be silently dropped to prevent double-capturing.
	/// </summary>
	[Theory]
	[InlineData("Microsoft.AspNetCore.Hosting.HttpRequestIn")]
	[InlineData("System.Net.Http.HttpRequestOut")]
	[InlineData("System.Net.Http.Desktop.HttpRequestOut")]
	[InlineData("ElasticApm.Transaction")]
	public void KnownInternalActivitiesAreSkipped(string operationName)
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			var src = new ActivitySource("Test.KnownListeners");
			using (src.StartActivity(operationName))
			{ }
		}
		payloadSender.Transactions.Should().BeEmpty();
		payloadSender.Spans.Should().BeEmpty();
	}

	/// <summary>
	/// Instrumentation libraries (e.g. gRPC) emit integer-valued tags for port numbers.
	/// Verifies those are converted to strings correctly so the span resource is populated.
	/// </summary>
	[Fact]
	public void IntValuedTagIsUsedInSpanResource()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			agent.Tracer.CaptureTransaction("parent", "test", () =>
			{
				var src = new ActivitySource("Test.IntTag");
				using (var activity = src.StartActivity("rpc.call", ActivityKind.Client))
				{
					activity?.SetTag("rpc.system", "grpc");
					activity?.SetTag("net.peer.name", "myhost");
					activity?.SetTag("net.peer.port", 50051); // int, not string
				}
			});
		}
		payloadSender.WaitForTransactions();
		payloadSender.WaitForSpans();
		var rpcSpan = payloadSender.Spans.Should().ContainSingle().Subject;
		rpcSpan.Context.Destination.Service.Resource.Should().Be("myhost:50051");
	}

	[Fact]
	public void ActivitiesAreNotCapturedAfterDispose()
	{
		var payloadSender = new MockPayloadSender();
		var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716));
		agent.Dispose();

		var src = new ActivitySource("Test.PostDispose");
		using (src.StartActivity("root", ActivityKind.Server))
		{ }

		payloadSender.Transactions.Should().BeEmpty();
		payloadSender.Spans.Should().BeEmpty();
	}

	[Fact]
	public void LongValuedPortTagIsUsedInSpanResource()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			agent.Tracer.CaptureTransaction("parent", "test", () =>
			{
				var src = new ActivitySource("Test.LongTag");
				using (var activity = src.StartActivity("rpc.call", ActivityKind.Client))
				{
					activity?.SetTag("rpc.system", "grpc");
					activity?.SetTag("net.peer.name", "myhost");
					activity?.SetTag("net.peer.port", 50051L); // long, not int or string
				}
			});
		}

		payloadSender.WaitForTransactions();
		payloadSender.WaitForSpans();

		var rpcSpan = payloadSender.Spans.Should().ContainSingle().Subject;
		rpcSpan.Context.Destination.Service.Resource.Should().Be("myhost:50051");
	}

	[Fact]
	public void HttpUrlWithNoExplicitPortAndNonStandardSchemeProducesHostOnlyResource()
	{
		// Verifies that ParseNetName does not emit "host:-1" when Uri.Port returns -1
		// (i.e. no explicit port and the scheme has no registered default).
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			agent.Tracer.CaptureTransaction("parent", "test", () =>
			{
				var src = new ActivitySource("Test.NoPort");
				using (var activity = src.StartActivity("http-call", ActivityKind.Client))
					activity?.SetTag(SemanticConventions.UrlFull, "custom://api.example.com/path");
			});
		}

		payloadSender.WaitForTransactions();
		payloadSender.WaitForSpans();

		var span = payloadSender.Spans.Should().ContainSingle().Subject;
		span.Type.Should().Be(ApiConstants.TypeExternal);
		span.Subtype.Should().Be(ApiConstants.SubtypeHttp);
		span.Context.Destination.Service.Resource.Should().Be("api.example.com");
		span.Context.Destination.Service.Resource.Should().NotContain(":-1");
	}

	[Fact]
	public void HttpSchemeOnlyDoesNotClassifySpanAsHttp()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			agent.Tracer.CaptureTransaction("parent", "test", () =>
			{
				var src = new ActivitySource("Test.SchemeOnly");
				using (var activity = src.StartActivity("scheme-only", ActivityKind.Client))
					activity?.SetTag(SemanticConventions.HttpScheme, "https");
			});
		}

		payloadSender.WaitForTransactions();
		payloadSender.WaitForSpans();

		var span = payloadSender.Spans.Should().ContainSingle().Subject;
		span.Type.Should().Be(ApiConstants.TypeUnknown);
		span.Subtype.Should().BeNull();
	}

	/// <summary>
	/// http.scheme alone is not sufficient to classify a root Server activity as an HTTP request transaction;
	/// url.full or http.url is required (aligned with span HTTP detection).
	/// </summary>
	[Fact]
	public void HttpSchemeOnlyDoesNotClassifyServerTransactionAsRequest()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			var src = new ActivitySource("Test.SchemeOnlyServer");
			using (var activity = src.StartActivity("scheme-only-server", ActivityKind.Server))
				activity?.SetTag(SemanticConventions.HttpScheme, "https");
		}

		payloadSender.WaitForTransactions();
		payloadSender.FirstTransaction.Type.Should().Be("unknown");
	}

	[Fact]
	public void HttpHostWithUnknownSchemeDoesNotProduceTrailingColonInResource()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			agent.Tracer.CaptureTransaction("parent", "test", () =>
			{
				var src = new ActivitySource("Test.UnknownSchemeHost");
				using (var activity = src.StartActivity("http-call", ActivityKind.Client))
				{
					activity?.SetTag(SemanticConventions.HttpHost, "api.example.com");
					activity?.SetTag(SemanticConventions.HttpScheme, "custom");
					activity?.SetTag(SemanticConventions.UrlFull, "custom://api.example.com/path");
				}
			});
		}

		payloadSender.WaitForTransactions();
		payloadSender.WaitForSpans();

		var span = payloadSender.Spans.Should().ContainSingle().Subject;
		span.Type.Should().Be(ApiConstants.TypeExternal);
		span.Subtype.Should().Be(ApiConstants.SubtypeHttp);
		span.Context.Destination.Service.Resource.Should().Be("api.example.com");
	}
}
