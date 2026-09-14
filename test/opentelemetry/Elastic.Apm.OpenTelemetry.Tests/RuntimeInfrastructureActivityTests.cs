// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Linq;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.OpenTelemetry.Tests;

/// <summary>
/// .NET 9 added experimental activity sources covering connection level plumbing (DNS resolution, socket connect,
/// TLS handshake and HTTP connection setup). They are dormant until a listener subscribes, so a listener is what
/// switches them on, which the bridge only does when OpenTelemetryBridgeExperimentalSourcesEnabled is set.
/// ConnectionSetup in particular is always the root of its own trace, so promoting these to transactions produced
/// meaningless top level entries in the UI for work such as application startup, or the agent's own transport.
/// <para>
/// The sources are matched by name, so these tests are meaningful on every target framework even though the runtime
/// only emits them on .NET 9 and above.
/// </para>
/// </summary>
[Collection("OpenTelemetry")]
public class RuntimeInfrastructureActivityTests
{
	[Theory]
	[InlineData("Experimental.System.Net.NameResolution")]
	[InlineData("Experimental.System.Net.Sockets")]
	[InlineData("Experimental.System.Net.Security")]
	[InlineData("Experimental.System.Net.Http.Connections")]
	public void RuntimeInfrastructureActivitiesAreNotPromotedToTransactions(string sourceName)
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716,
			configuration: new MockConfiguration(openTelemetryBridgeExperimentalSourcesEnabled: "true"))))
		{
			var src = new ActivitySource(sourceName);
			using (src.StartActivity("operation"))
			{ }
		}

		payloadSender.Transactions.Should().BeEmpty();
		payloadSender.Spans.Should().BeEmpty();
	}

	[Theory]
	[InlineData("Experimental.System.Net.NameResolution")]
	[InlineData("Experimental.System.Net.Sockets")]
	[InlineData("Experimental.System.Net.Security")]
	[InlineData("Experimental.System.Net.Http.Connections")]
	public void RuntimeInfrastructureActivitiesAreCapturedAsSpansWithinATransaction(string sourceName)
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716,
			configuration: new MockConfiguration(openTelemetryBridgeExperimentalSourcesEnabled: "true"))))
		{
			agent.Tracer.CaptureTransaction("UserTransaction", "request", () =>
			{
				var src = new ActivitySource(sourceName);
				using (src.StartActivity("operation"))
				{ }
			});
		}

		payloadSender.WaitForTransactions();
		payloadSender.Transactions.Should().HaveCount(1);
		payloadSender.Spans.Should().HaveCount(1);
		payloadSender.FirstSpan.ParentId.Should().Be(payloadSender.FirstTransaction.Id);
	}

	/// <summary>
	/// The runtime activities only set a readable DisplayName when they stop, for example DNS lookup example.com.
	/// </summary>
	[Fact]
	public void DisplayNameSetWhenAnActivityStopsIsAdoptedByTheTransaction()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			var src = new ActivitySource("Test.LateDisplayName");
			using (var activity = src.StartActivity("raw.operation.name", ActivityKind.Server))
				activity!.DisplayName = "GET /orders";
		}

		payloadSender.WaitForTransactions();
		payloadSender.FirstTransaction.Name.Should().Be("GET /orders");
	}

	[Fact]
	public void DisplayNameSetWhenAnActivityStopsIsAdoptedByTheSpan()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716,
			configuration: new MockConfiguration(openTelemetryBridgeExperimentalSourcesEnabled: "true"))))
		{
			agent.Tracer.CaptureTransaction("UserTransaction", "request", () =>
			{
				var src = new ActivitySource("Experimental.System.Net.NameResolution");
				using (var activity = src.StartActivity("Experimental.System.Net.NameResolution.DnsLookup"))
					activity!.DisplayName = "DNS lookup example.com";
			});
		}

		payloadSender.WaitForSpans();
		payloadSender.FirstSpan.Name.Should().Be("DNS lookup example.com");
	}

	[Fact]
	public void NameSetThroughTheElasticApiIsNotOverwrittenByALateDisplayName()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			var src = new ActivitySource("Test.LateDisplayName");
			using (var activity = src.StartActivity("raw.operation.name", ActivityKind.Server))
			{
				agent.Tracer.CurrentTransaction!.Name = "NamedThroughTheElasticApi";
				activity!.DisplayName = "GET /orders";
			}
		}

		payloadSender.WaitForTransactions();
		payloadSender.FirstTransaction.Name.Should().Be("NamedThroughTheElasticApi");
	}

	[Fact]
	public void NameSetThroughTheElasticApiIsNotOverwrittenOnASpan()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			agent.Tracer.CaptureTransaction("UserTransaction", "request", () =>
			{
				var src = new ActivitySource("Test.LateDisplayName.Span");
				using (var activity = src.StartActivity("raw.operation.name"))
				{
					agent.Tracer.CurrentSpan!.Name = "NamedThroughTheElasticApi";
					activity!.DisplayName = "renamed by the activity";
				}
			});
		}

		payloadSender.WaitForSpans();
		payloadSender.FirstSpan.Name.Should().Be("NamedThroughTheElasticApi");
	}

	/// <summary>
	/// The final display name is what any OpenTelemetry exporter would report, so it is adopted even when the activity
	/// already carried a display name of its own when it started. Only a name set through the Elastic API is kept.
	/// </summary>
	[Fact]
	public void DisplayNameChangedAfterStartIsAdoptedEvenWhenOneWasSetUpFront()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			// ActivitySource.StartActivity notifies listeners before the caller can touch DisplayName, so the only way to
			// have one already set at that point is to construct the activity directly.
			var activity = new Activity("raw.operation.name") { DisplayName = "chosen up front" };
			activity.Start();
			activity.DisplayName = "changed at the end";
			activity.Stop();
		}

		payloadSender.WaitForTransactions();
		payloadSender.FirstTransaction.Name.Should().Be("changed at the end");
	}

	/// <summary>
	/// Activities constructed directly, rather than through an <see cref="ActivitySource" />, report an empty source
	/// name. The default allowed pattern matches it, so this legacy style of instrumentation keeps working.
	/// </summary>
	[Fact]
	public void ActivitiesWithoutASourceAreStillCapturedByDefault()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			var activity = new Activity("legacy.operation");
			activity.Start();
			activity.Stop();
		}

		payloadSender.WaitForTransactions();
		payloadSender.Transactions.Should().HaveCount(1);
		payloadSender.FirstTransaction.Name.Should().Be("legacy.operation");
	}

	[Fact]
	public void UnchangedDisplayNameLeavesTheTransactionNameAlone()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			var src = new ActivitySource("Test.UnchangedDisplayName");
			using (src.StartActivity("stable.operation.name", ActivityKind.Server))
			{ }
		}

		payloadSender.WaitForTransactions();
		payloadSender.FirstTransaction.Name.Should().Be("stable.operation.name");
	}

	/// <summary>
	/// Connection level activities nest inside each other, mirroring how the runtime parents DNS resolution, socket
	/// connect and the TLS handshake beneath connection setup.
	/// </summary>
	[Fact]
	public void NestedRuntimeInfrastructureActivitiesFormASpanChain()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716,
			configuration: new MockConfiguration(openTelemetryBridgeExperimentalSourcesEnabled: "true"))))
		{
			agent.Tracer.CaptureTransaction("UserTransaction", "request", () =>
			{
				var connections = new ActivitySource("Experimental.System.Net.Http.Connections");
				var dns = new ActivitySource("Experimental.System.Net.NameResolution");
				using (connections.StartActivity("ConnectionSetup"))
				using (dns.StartActivity("DnsLookup"))
				{ }
			});
		}

		payloadSender.WaitForSpans(count: 2);
		payloadSender.Transactions.Should().HaveCount(1);
		payloadSender.Spans.Should().HaveCount(2);

		var connectionSetup = payloadSender.Spans.Single(s => s.Name == "ConnectionSetup");
		var dnsLookup = payloadSender.Spans.Single(s => s.Name == "DnsLookup");

		connectionSetup.ParentId.Should().Be(payloadSender.FirstTransaction.Id);
		dnsLookup.ParentId.Should().Be(connectionSetup.Id);
	}
}
