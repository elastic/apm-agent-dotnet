// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Apm.Api;
using Xunit;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.XUnit;
using OpenTelemetrySample;
using FluentAssertions;
using Elastic.Apm.Model;

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
		foreach (var span in payloadSender.Spans) span.TraceId.Should().Be(payloadSender.FirstTransaction.TraceId);
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
}
