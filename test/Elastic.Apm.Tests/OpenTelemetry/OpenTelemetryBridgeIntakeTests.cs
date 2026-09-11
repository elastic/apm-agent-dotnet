// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

#if NET
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.OpenTelemetry;

/// <summary>
/// End to end coverage for the OpenTelemetry bridge: a real agent, the real payload sender, and the intake endpoint
/// of MockApmServer, which validates every event against the APM Server specification and rejects an entire ndjson
/// batch when it does not deserialize. The bridge tests elsewhere stop at the in memory model, so this is what proves
/// bridged data survives serialization and the intake contract.
/// <para>
/// The collection serializes these tests against each other, which is all it does here: no other class in this
/// assembly joins it, so it is not what keeps unrelated tests out. Narrowing the bridge through
/// <c>allowedSources</c> is, and every test which asserts on exactly what the server received uses it.
/// </para>
/// </summary>
[Collection("OpenTelemetry")]
public class OpenTelemetryBridgeIntakeTests : IDisposable
{
	private readonly MockApmServer.MockApmServer _server;
	private readonly int _port;
	private readonly ManualResetEvent _transactionReceived = new(false);

	public OpenTelemetryBridgeIntakeTests()
	{
		_server = new MockApmServer.MockApmServer(new NoopLogger(), nameof(OpenTelemetryBridgeIntakeTests));
		_port = _server.FindAvailablePortToListen();
		_server.OnReceive += o =>
		{
			if (o is MockApmServer.TransactionDto)
				_transactionReceived.Set();
		};
		_server.RunInBackground(_port);
	}

	public void Dispose()
	{
		_server.StopAsync().GetAwaiter().GetResult();
		_transactionReceived.Dispose();
	}

	/// <summary>
	/// The bridge listens process wide, so an agent under test also captures activities produced by tests running in
	/// parallel. Where a test asserts on exactly what was received, <paramref name="allowedSources" /> narrows the
	/// bridge to that test's own activity source and makes the result deterministic.
	/// </summary>
	private MockConfiguration Configuration(string experimentalSources = null, string allowedSources = null, string deniedSources = null) => new(
		serverUrl: $"http://localhost:{_port}",
		flushInterval: "1s",
		disableMetrics: "*",
		cloudProvider: "none",
		centralConfig: "false",
		openTelemetryBridgeAllowedActivitySources: allowedSources,
		openTelemetryBridgeDeniedActivitySources: deniedSources,
		openTelemetryBridgeExperimentalSourcesEnabled: experimentalSources);

	private void WaitForTransaction()
	{
		_transactionReceived.WaitOne(TimeSpan.FromMinutes(1)).Should().BeTrue(
			"timed out waiting for a transaction. Invalid payloads received: {0}",
			string.Join(Environment.NewLine, _server.ReceivedData.InvalidPayloadErrors));

		// This is what turns a rejected batch into a failure rather than a vacuous pass of the assertions that follow.
		// For the one test which keeps the catch-all subscription, an activity from an unrelated test running in
		// parallel can reach this server too, so an invalid event produced elsewhere would fail that test as well.
		// That is accepted: every known cause of an invalid bridged event is fixed, and a new one should surface.
		_server.ReceivedData.InvalidPayloadErrors.Should().BeEmpty();
	}

	/// <summary>
	/// A bridged transaction carries an otel object. Intake deserialization is strict about unknown members, so this
	/// is what catches the whole batch being rejected and every event in it silently lost.
	/// </summary>
	[Fact]
	public void BridgedTransactionSurvivesTheIntakeContract()
	{
		using (new ApmAgent(new AgentComponents(new NoopLogger(), Configuration(allowedSources: "Intake.Bridge"))))
		{
			var src = new ActivitySource("Intake.Bridge");
			using (var activity = src.StartActivity("bridged-operation", ActivityKind.Server))
				activity!.SetTag("custom.attribute", "value");
		}

		WaitForTransaction();

		_server.ReceivedData.Transactions.Should().HaveCount(1);
		var transaction = _server.ReceivedData.Transactions.Single();
		transaction.Name.Should().Be("bridged-operation");
		transaction.Otel.Should().NotBeNull();
		transaction.Otel.SpanKind.Should().Be("Server");
		transaction.Otel.Attributes.Should().ContainKey("custom.attribute");
		transaction.AssertValid();
	}

	[Fact]
	public void BridgedSpanSurvivesTheIntakeContract()
	{
		using (var agent = new ApmAgent(new AgentComponents(new NoopLogger(), Configuration(allowedSources: "Intake.Bridge.Span"))))
		{
			agent.Tracer.CaptureTransaction("UserTransaction", "request", () =>
			{
				var src = new ActivitySource("Intake.Bridge.Span");
				using (var activity = src.StartActivity("bridged-child", ActivityKind.Client))
					activity!.SetTag("custom.attribute", "value");
			});
		}

		WaitForTransaction();

		_server.ReceivedData.Spans.Should().HaveCount(1);
		var span = _server.ReceivedData.Spans.Single();
		span.Name.Should().Be("bridged-child");
		span.Otel.Should().NotBeNull();
		span.Otel.SpanKind.Should().Be("Client");
		span.ParentId.Should().Be(_server.ReceivedData.Transactions.Single().Id);
		span.AssertValid();
	}

	/// <summary>
	/// The link the bridge records for a declared but unhonoured remote parent has to reach the server as span_id and
	/// trace_id, not merely exist in the in memory model.
	/// </summary>
	[Fact]
	public void DeclaredRemoteParentLinkReachesTheServer()
	{
		const string producerTraceId = "0af7651916cd43dd8448eb211c80319c";
		const string producerSpanId = "b7ad6b7169203331";

		using (var agent = new ApmAgent(new AgentComponents(new NoopLogger(), Configuration(allowedSources: "Intake.Bridge.Messaging"))))
		{
			agent.Tracer.CaptureTransaction("Consumer-loop", "messaging", () =>
			{
				var src = new ActivitySource("Intake.Bridge.Messaging");
				var producer = ActivityContext.Parse($"00-{producerTraceId}-{producerSpanId}-01", null);
				using (src.StartActivity("receive", ActivityKind.Consumer, producer))
				{ }
			});
		}

		WaitForTransaction();

		var span = _server.ReceivedData.Spans.Single(s => s.Name == "receive");
		span.Links.Should().ContainSingle();
		span.Links.Single().SpanId.Should().Be(producerSpanId);
		span.Links.Single().TraceId.Should().Be(producerTraceId);
		span.AssertValid();
	}

	/// <summary>
	/// Communication with the APM Server performs DNS resolution and connection setup of its own. On .NET 9 and later
	/// that used to be reported as top level transactions, which is the pollution this default removes.
	/// <para>
	/// This is the one test which has to leave the bridge on its default allowed list, because what it asserts is that
	/// the default produces nothing for the agent's own transport. Narrowing the allowed list here would remove the very
	/// thing under test. The one denial is for the mock server: it is an ASP.NET Core application hosted in this very
	/// process, so the bridge would otherwise turn each intake request it receives into a transaction, which the next
	/// batch then delivers to it in turn. That is an artefact of the test setup, not of the agent's transport.
	/// </para>
	/// </summary>
	[Fact]
	public void AgentTransportProducesNoBridgedTelemetry()
	{
		using (var agent = new ApmAgent(new AgentComponents(new NoopLogger(), Configuration(deniedSources: "Microsoft.AspNetCore"))))
			agent.Tracer.CaptureTransaction("UserTransaction", "request", () => { });

		WaitForTransaction();

		// A negative assertion has nothing to wait for, so it is backed by a fixed grace period for a batch which may
		// still be in flight. Too short a wait can only let unwanted data through; it cannot fail an otherwise good run.
		Thread.Sleep(TimeSpan.FromSeconds(2));

		_server.ReceivedData.Transactions.Should().Contain(t => t.Name == "UserTransaction");
		_server.ReceivedData.Transactions.Should().NotContain(t => IsConnectionLevelName(t.Name));
		_server.ReceivedData.Spans.Should().NotContain(s => IsConnectionLevelName(s.Name));
	}

	/// <summary>
	/// With the experimental sources enabled, connection level activities are captured as spans, must satisfy the
	/// intake contract, and still must not become transactions.
	/// </summary>
	[Fact]
	public void EnabledExperimentalSourcesStillProduceNoTopLevelTransactions()
	{
		using (var agent = new ApmAgent(new AgentComponents(new NoopLogger(),
			Configuration(experimentalSources: "true", allowedSources: "Experimental.System.Net.NameResolution"))))
		{
			agent.Tracer.CaptureTransaction("UserTransaction", "request", () =>
			{
				var src = new ActivitySource("Experimental.System.Net.NameResolution");
				using (var activity = src.StartActivity("DnsLookup"))
					activity!.DisplayName = "DNS lookup example.com";
			});
		}

		WaitForTransaction();

		_server.ReceivedData.Transactions.Should().OnlyContain(t => t.Name == "UserTransaction");
		_server.ReceivedData.Spans.Should().ContainSingle()
			.Which.Name.Should().Be("DNS lookup example.com");
		_server.ReceivedData.Spans.Single().AssertValid();
	}

	/// <summary>
	/// Display names of the activities the runtime emits while establishing a connection, as they appear once stopped.
	/// </summary>
	private static bool IsConnectionLevelName(string name) =>
		name != null
		&& (name.StartsWith("DNS lookup", StringComparison.Ordinal)
			|| name.StartsWith("socket connect", StringComparison.Ordinal)
			|| name.StartsWith("TLS client handshake", StringComparison.Ordinal)
			|| name.StartsWith("HTTP connection_setup", StringComparison.Ordinal)
			|| name.StartsWith("HTTP wait_for_connection", StringComparison.Ordinal)
			|| name.StartsWith("Experimental.System.Net.", StringComparison.Ordinal));
}
#endif
