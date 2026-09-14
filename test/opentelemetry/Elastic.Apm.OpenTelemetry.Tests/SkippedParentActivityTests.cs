// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.OpenTelemetry.Tests;

/// <summary>
/// An activity whose in-process parent the bridge declined on purpose, because ShouldSkipActivity rejects it or its
/// source is excluded by the activity source filter, used to become a transaction pointing at that parent, which is
/// never sent to the server. The skipped parent is collapsed instead: the trace continues from whatever the top-most
/// skipped ancestor declared as its own parent, so a remote parent one hop up is honoured and a chain with no remote
/// ancestor becomes a root of its trace.
/// </summary>
[Collection("OpenTelemetry")]
public class SkippedParentActivityTests
{
	private const string RemoteTraceId = "0af7651916cd43dd8448eb211c80319c";
	private const string RemoteSpanId = "b7ad6b7169203331";

	private static ActivityContext RemoteContext => ActivityContext.Parse($"00-{RemoteTraceId}-{RemoteSpanId}-01", null);

	/// <summary>
	/// System.Net.Http.HttpRequestOut is skipped by the bridge unconditionally. Started with no parent of its own, a
	/// child of it has nothing to continue from and becomes the root of the trace it already belongs to.
	/// </summary>
	[Fact]
	public void ChildOfSkippedActivityDoesNotCreateAnOrphanTransaction()
	{
		var payloadSender = new MockPayloadSender();
		string traceId;
		string skippedParentSpanId;

		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			var src = new ActivitySource("Test.OrphanParent");
			using var parent = src.StartActivity(KnownListeners.SystemNetHttpHttpRequestOut, ActivityKind.Client);
			using var child = src.StartActivity("child");

			traceId = child!.TraceId.ToString();
			skippedParentSpanId = parent!.SpanId.ToString();
		}

		payloadSender.WaitForTransactions();
		payloadSender.Transactions.Should().HaveCount(1);
		payloadSender.FirstTransaction.Name.Should().Be("child");
		payloadSender.FirstTransaction.TraceId.Should().Be(traceId);
		payloadSender.FirstTransaction.ParentId.Should().NotBe(skippedParentSpanId);
		payloadSender.FirstTransaction.ParentId.Should().BeNull();
	}

	/// <summary>
	/// The same rule applies whichever reason the bridge had for skipping the parent. Here the parent is skipped
	/// because an Elastic instrumentation package for the same technology is present.
	/// </summary>
	[Fact]
	public void ChildOfADeduplicatedActivityDoesNotCreateAnOrphanTransaction()
	{
		var payloadSender = new MockPayloadSender();
		var components = new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716);
		string traceId;
		string skippedParentSpanId;

		using (new ApmAgent(components))
		{
			components.ElasticActivityListener.CheckAssembly("Elastic.Apm.MongoDb");

			var mongo = new ActivitySource("MongoDB.Driver");
			var app = new ActivitySource("Test.DedupParent");
			using var parent = mongo.StartActivity("mongodb.query", ActivityKind.Client);
			using var child = app.StartActivity("child");

			traceId = child!.TraceId.ToString();
			skippedParentSpanId = parent!.SpanId.ToString();
		}

		payloadSender.WaitForTransactions();
		payloadSender.Transactions.Should().HaveCount(1);
		payloadSender.FirstTransaction.Name.Should().Be("child");
		payloadSender.FirstTransaction.TraceId.Should().Be(traceId);
		payloadSender.FirstTransaction.ParentId.Should().NotBe(skippedParentSpanId);
		payloadSender.FirstTransaction.ParentId.Should().BeNull();
	}

	/// <summary>
	/// A parent whose source the activity source filter excludes is one the bridge declined on purpose, so the same
	/// rule applies as for a skipped one. The parent activity exists at all only because a second listener, such as an
	/// OpenTelemetry SDK the application configures itself, observes that source.
	/// </summary>
	[Fact]
	public void ChildOfASourceFilteredActivityDoesNotCreateAnOrphanTransaction()
	{
		const string deniedSource = "Test.DeniedParent";

		using var otherPipeline = new ActivityListener
		{
			ShouldListenTo = s => s.Name == deniedSource,
			Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
		};
		ActivitySource.AddActivityListener(otherPipeline);

		var payloadSender = new MockPayloadSender();
		string traceId;
		string deniedParentSpanId;

		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716,
			configuration: new MockConfiguration(openTelemetryBridgeDeniedActivitySources: deniedSource))))
		{
			using var parent = new ActivitySource(deniedSource).StartActivity("denied-parent", ActivityKind.Client);
			using var child = new ActivitySource("Test.DeniedParent.Child").StartActivity("child");

			traceId = child!.TraceId.ToString();
			deniedParentSpanId = parent!.SpanId.ToString();
		}

		payloadSender.WaitForTransactions();
		payloadSender.Transactions.Should().HaveCount(1);
		payloadSender.FirstTransaction.Name.Should().Be("child");
		payloadSender.FirstTransaction.TraceId.Should().Be(traceId);
		payloadSender.FirstTransaction.ParentId.Should().NotBe(deniedParentSpanId);
		payloadSender.FirstTransaction.ParentId.Should().BeNull();
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ChildOfFilteredSourcelessHostingRequestContinuesFromItsRemoteParent(bool sampled)
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716,
			configuration: new MockConfiguration(openTelemetryBridgeDeniedActivitySources: "Microsoft.AspNetCore"))))
		{
			using var request = new Activity(KnownListeners.MicrosoftAspNetCoreHostingHttpRequestIn)
				.SetParentId($"00-{RemoteTraceId}-{RemoteSpanId}-{(sampled ? "01" : "00")}");
			request.Start();
			using var source = new ActivitySource("Test.FilteredHostingRequest.Child");
			using (source.StartActivity("child"))
			{ }
		}

		var transaction = payloadSender.Transactions.Should().ContainSingle().Which;
		transaction.Name.Should().Be("child");
		transaction.TraceId.Should().Be(RemoteTraceId);
		transaction.ParentId.Should().Be(RemoteSpanId);
		transaction.IsSampled.Should().Be(sampled);
		payloadSender.Spans.Should().BeEmpty();
	}

	/// <summary>
	/// A genuinely remote parent, where only the ParentId is known, must still continue the distributed trace.
	/// </summary>
	[Fact]
	public void ActivityWithRemoteParentStillStartsATransaction()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			var src = new ActivitySource("Test.RemoteParent");
			using (src.StartActivity("incoming", ActivityKind.Server, RemoteContext))
			{ }
		}

		payloadSender.WaitForTransactions();
		payloadSender.Transactions.Should().HaveCount(1);
		payloadSender.FirstTransaction.TraceId.Should().Be(RemoteTraceId);
		payloadSender.FirstTransaction.ParentId.Should().Be(RemoteSpanId);
	}

	/// <summary>
	/// The skipped parent may itself continue a remote trace, which is what the ASP.NET Core request activity does when
	/// the ASP.NET Core integration is loaded and declines to create a transaction, for example for a URL matched by
	/// TransactionIgnoreUrls. Severing at the skipped parent would make the child a second root in the upstream trace
	/// and lose the edge to the calling service; collapsing it keeps that edge.
	/// </summary>
	[Fact]
	public void ChildOfASkippedActivityContinuesFromTheSkippedActivitysRemoteParent()
	{
		var payloadSender = new MockPayloadSender();
		var components = new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716);
		string skippedParentSpanId;

		using (new ApmAgent(components))
		{
			components.ElasticActivityListener.CheckAssembly("Elastic.Apm.AspNetCore");

			var hosting = new ActivitySource("Microsoft.AspNetCore");
			var app = new ActivitySource("Test.RemoteGrandparent");
			using var request = hosting.StartActivity(KnownListeners.MicrosoftAspNetCoreHostingHttpRequestIn, ActivityKind.Server, RemoteContext);
			using var child = app.StartActivity("child");

			skippedParentSpanId = request!.SpanId.ToString();
		}

		payloadSender.WaitForTransactions();
		payloadSender.Transactions.Should().HaveCount(1);
		payloadSender.FirstTransaction.Name.Should().Be("child");
		payloadSender.FirstTransaction.TraceId.Should().Be(RemoteTraceId);
		payloadSender.FirstTransaction.ParentId.Should().Be(RemoteSpanId);
		payloadSender.FirstTransaction.ParentId.Should().NotBe(skippedParentSpanId);
	}

	/// <summary>Every skipped ancestor is collapsed, not only the immediate parent.</summary>
	[Fact]
	public void AChainOfSkippedAncestorsIsCollapsedToTheNearestRemoteParent()
	{
		var payloadSender = new MockPayloadSender();
		var components = new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716);

		using (new ApmAgent(components))
		{
			components.ElasticActivityListener.CheckAssembly("Elastic.Apm.AspNetCore");

			var hosting = new ActivitySource("Microsoft.AspNetCore");
			var http = new ActivitySource("System.Net.Http");
			var app = new ActivitySource("Test.SkippedChain");
			using var request = hosting.StartActivity(KnownListeners.MicrosoftAspNetCoreHostingHttpRequestIn, ActivityKind.Server, RemoteContext);
			using var outgoing = http.StartActivity(KnownListeners.SystemNetHttpHttpRequestOut, ActivityKind.Client);
			using var child = app.StartActivity("child");
		}

		payloadSender.WaitForTransactions();
		payloadSender.Transactions.Should().HaveCount(1);
		payloadSender.FirstTransaction.Name.Should().Be("child");
		payloadSender.FirstTransaction.TraceId.Should().Be(RemoteTraceId);
		payloadSender.FirstTransaction.ParentId.Should().Be(RemoteSpanId);
	}

	/// <summary>
	/// The agent's own transaction activity is skipped from capture too, but its span id is the id of a transaction
	/// which is sent, so it is not collapsed. Reaching this needs Activity.Current to point at a transaction's activity
	/// while no transaction is current, which only a stale execution context produces; a thread started without
	/// flowing the context reproduces that.
	/// </summary>
	[Fact]
	public void ChildOfTheAgentsOwnTransactionActivityContinuesFromThatTransaction()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			var transaction = agent.Tracer.StartTransaction("T", "request");
			var transactionActivity = Activity.Current;
			transactionActivity!.OperationName.Should().Be(KnownListeners.ApmTransactionActivityName);

			var thread = new Thread(() =>
			{
				agent.Tracer.CurrentTransaction.Should().BeNull();
				Activity.Current = transactionActivity;
				using (new ActivitySource("Test.OwnTransactionParent").StartActivity("child"))
				{ }
			});

			using (ExecutionContext.SuppressFlow())
				thread.Start();
			thread.Join();

			transaction.End();
		}

		payloadSender.WaitForTransactions(count: 2);
		var parent = payloadSender.Transactions.Single(t => t.Name == "T");
		var child = payloadSender.Transactions.Single(t => t.Name == "child");

		child.TraceId.Should().Be(parent.TraceId);
		child.ParentId.Should().Be(parent.Id);
	}
}
