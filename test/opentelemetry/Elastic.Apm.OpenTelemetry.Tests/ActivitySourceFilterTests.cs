// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.OpenTelemetry.Tests;

/// <summary>
/// Covers which activity sources the bridge subscribes to. Filtering happens in
/// <see cref="ActivityListener.ShouldListenTo" />, so a denied source is never observed and, for sources which only
/// emit while a listener is attached, never even created.
/// </summary>
[Collection("OpenTelemetry")]
public class ActivitySourceFilterTests
{
	/// <summary>Starts one server activity from <paramref name="sourceName" /> and returns how many transactions the bridge produced.</summary>
	private static int CountCaptured(string sourceName, MockConfiguration configuration)
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
			apmServerInfo: MockApmServerInfo.Version716, configuration: configuration)))
		{
			var src = new ActivitySource(sourceName);
			using (src.StartActivity("operation", ActivityKind.Server))
			{ }
		}
		return payloadSender.Transactions.Count;
	}

	[Fact]
	public void EverythingIsObservedByDefault() =>
		CountCaptured("Filter.Default.Anything", new MockConfiguration()).Should().Be(1);

	[Fact]
	public void SourceMatchingTheDeniedListIsNotObserved() =>
		CountCaptured("Filter.Denied.Source",
			new MockConfiguration(openTelemetryBridgeDeniedActivitySources: "Filter.Denied.Source")).Should().Be(0);

	[Fact]
	public void SourceOutsideTheAllowedListIsNotObserved() =>
		CountCaptured("Filter.NotAllowed.Source",
			new MockConfiguration(openTelemetryBridgeAllowedActivitySources: "Something.Else")).Should().Be(0);

	[Theory]
	[InlineData(null, null, true)]
	[InlineData("*", "Microsoft.AspNetCore", false)]
	[InlineData("*", "microsoft.aspnet*", false)]
	[InlineData("(?i)", null, false)]
	[InlineData("*", "Other.Source", true)]
	public void SourcelessHostingRequestHonoursSourceFilter(string? allowed, string? denied, bool captured)
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
			apmServerInfo: MockApmServerInfo.Version716,
			configuration: new MockConfiguration(
				openTelemetryBridgeAllowedActivitySources: allowed,
				openTelemetryBridgeDeniedActivitySources: denied))))
		{
			// A case-insensitive empty pattern admits the sourceless fallback but not the named hosting source.
			using var request = new Activity(KnownListeners.MicrosoftAspNetCoreHostingHttpRequestIn)
				.SetIdFormat(ActivityIdFormat.W3C).Start();
		}

		payloadSender.Transactions.Should().HaveCount(captured ? 1 : 0);
		payloadSender.Spans.Should().BeEmpty();
	}

	[Fact]
	public void DenyingHostingSourceDoesNotExcludeOtherSourcelessActivitiesOrNamedSources()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
			apmServerInfo: MockApmServerInfo.Version716,
			configuration: new MockConfiguration(openTelemetryBridgeDeniedActivitySources: "Microsoft.AspNetCore"))))
		{
			using (new Activity("Other.Operation").SetIdFormat(ActivityIdFormat.W3C).Start())
			{ }

			using var source = new ActivitySource("Other.Source");
			using (source.StartActivity(KnownListeners.MicrosoftAspNetCoreHostingHttpRequestIn, ActivityKind.Server))
			{ }
		}

		payloadSender.Transactions.Should().HaveCount(2);
		payloadSender.Spans.Should().BeEmpty();
	}

	[Fact]
	public void SourceInsideTheAllowedListIsObserved() =>
		CountCaptured("Filter.Allowed.Source",
			new MockConfiguration(openTelemetryBridgeAllowedActivitySources: "Filter.Allowed.Source, Other.Source")).Should().Be(1);

	/// <summary>The allow list is a gate and the deny list is a veto; the veto always wins.</summary>
	[Fact]
	public void DeniedWinsOverAllowed() =>
		CountCaptured("Microsoft.System.Something",
			new MockConfiguration(
				openTelemetryBridgeAllowedActivitySources: "Microsoft.*",
				openTelemetryBridgeDeniedActivitySources: "Microsoft.System.Something")).Should().Be(0);

	[Fact]
	public void WildcardAllowedWithASpecificDenialStillObservesSiblings() =>
		CountCaptured("Microsoft.System.SomethingElse",
			new MockConfiguration(
				openTelemetryBridgeAllowedActivitySources: "Microsoft.*",
				openTelemetryBridgeDeniedActivitySources: "Microsoft.System.Something")).Should().Be(1);

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData(",  ,")]
	public void BlankAllowedListFallsBackToTheDefaultRatherThanMutingTheBridge(string allowed) =>
		CountCaptured("Filter.Blank.Allowed",
			new MockConfiguration(openTelemetryBridgeAllowedActivitySources: allowed)).Should().Be(1);

	[Theory]
	[InlineData("Experimental.System.Net.NameResolution")]
	[InlineData("Experimental.System.Net.Http.Connections")]
	[InlineData("Experimental.Something.Else")]
	public void ExperimentalSourcesAreNotObservedByDefault(string sourceName) =>
		CountCaptured(sourceName, new MockConfiguration()).Should().Be(0);

	[Fact]
	public void ExperimentalSourcesAreObservedWhenExplicitlyEnabled() =>
		CountCaptured("Experimental.Something.Else",
			new MockConfiguration(openTelemetryBridgeExperimentalSourcesEnabled: "true")).Should().Be(1);

	/// <summary>The flag contributes to the deny list, so an explicit denial still applies when it is enabled.</summary>
	[Fact]
	public void ExplicitDenialStillAppliesWhenExperimentalSourcesAreEnabled() =>
		CountCaptured("Experimental.Something.Else",
			new MockConfiguration(
				openTelemetryBridgeExperimentalSourcesEnabled: "true",
				openTelemetryBridgeDeniedActivitySources: "Experimental.Something.*")).Should().Be(0);

	/// <summary>
	/// Environment variables are the only configuration channel under the profiler, so the resolved filter has to be
	/// discoverable from the agent log.
	/// </summary>
	[Fact]
	public void EffectiveFilterIsLoggedWhenTheBridgeStarts()
	{
		var logger = new TestLogger(LogLevel.Debug);
		using (new ApmAgent(new TestAgentComponents(logger: logger, payloadSender: new MockPayloadSender(),
			apmServerInfo: MockApmServerInfo.Version716,
			configuration: new MockConfiguration(
				openTelemetryBridgeAllowedActivitySources: "Microsoft.*",
				openTelemetryBridgeDeniedActivitySources: "Microsoft.Something.Noisy"))))
		{ }

		var log = logger.Log;
		log.Should().Contain("activity source filter");
		log.Should().Contain("allowed=[Microsoft.*]");
		log.Should().Contain("Microsoft.Something.Noisy");
		log.Should().Contain("Experimental.*");
		log.Should().Contain("Experimental activity sources are disabled");
	}

	/// <summary>
	/// Denying a source only stops the Elastic bridge observing it. Elastic's own HTTP instrumentation is driven by a
	/// DiagnosticListener subscription and must keep working.
	/// </summary>
	[Fact]
	public async Task DenyingSystemNetHttpDoesNotDisableElasticHttpInstrumentation()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
			apmServerInfo: MockApmServerInfo.Version716,
			configuration: new MockConfiguration(openTelemetryBridgeDeniedActivitySources: "System.Net.Http"))))
		using (var localServer = LocalServer.Create())
		{
			agent.Subscribe(new Elastic.Apm.DiagnosticSource.HttpDiagnosticsSubscriber());
			await agent.Tracer.CaptureTransaction("T", "request", async () =>
			{
				using var http = new HttpClient();
				await http.GetAsync(localServer.Uri);
			});
		}

		payloadSender.WaitForSpans();
		payloadSender.Spans.Should().Contain(s => s.Type == "external" && s.Subtype == "http");
	}

	/// <summary>
	/// The agent creates the activity that represents a transaction through its own 'Elastic.Apm' activity source, and
	/// ActivitySource.CreateActivity returns null when nothing listens to a source. While the bridge is enabled its
	/// listener is the only subscriber to that source, so a filter which excluded it would leave Activity.Current unset
	/// for the duration of every transaction: an activity started inside one would become the root of a separate trace
	/// instead of joining it, and an OpenTelemetry SDK the application configures itself would report a different trace
	/// id than the agent.
	/// </summary>
	[Theory]
	[InlineData("Filter.Own.Source", null)]
	[InlineData(null, "Elastic.Apm")]
	[InlineData(null, "Elastic.*")]
	public void TheAgentsOwnActivitySourceIsObservedWhateverTheFilterSays(string? allowed, string? denied)
	{
		string? transactionActivityName = null;
		string? nestedTraceId = null;

		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
			apmServerInfo: MockApmServerInfo.Version716,
			configuration: new MockConfiguration(
				openTelemetryBridgeAllowedActivitySources: allowed,
				openTelemetryBridgeDeniedActivitySources: denied))))
		{
			agent.Tracer.CaptureTransaction("T", "request", () =>
			{
				transactionActivityName = Activity.Current?.OperationName;

				using var nested = new ActivitySource("Filter.Own.Source").StartActivity("child");
				nestedTraceId = nested?.TraceId.ToString();
			});
		}

		payloadSender.WaitForTransactions();
		transactionActivityName.Should().Be(KnownListeners.ApmTransactionActivityName);
		nestedTraceId.Should().Be(payloadSender.FirstTransaction.TraceId);
	}
}
