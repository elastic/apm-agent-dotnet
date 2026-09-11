// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Apm.Api;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.OpenTelemetry.Tests;

/// <summary>
/// An activity can declare a parent that was never created in this process, which is what a messaging consumer does
/// when it continues the producer's context. When an Elastic transaction is already in progress the bridge nests the
/// activity under the current span rather than starting a new trace, so the declared parent is recorded as a span
/// link instead of being discarded.
/// </summary>
[Collection("OpenTelemetry")]
public class RemoteParentSpanLinkTests
{
	private const string ProducerTraceId = "0af7651916cd43dd8448eb211c80319c";
	private const string ProducerSpanId = "b7ad6b7169203331";

	private static ActivityContext ProducerContext =>
		ActivityContext.Parse($"00-{ProducerTraceId}-{ProducerSpanId}-01", null);

	[Fact]
	public void DeclaredRemoteParentIsRecordedAsASpanLink()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			agent.Tracer.CaptureTransaction("Consumer-loop", "messaging", () =>
			{
				var src = new ActivitySource("Link.Messaging");
				using (src.StartActivity("receive", ActivityKind.Consumer, ProducerContext))
				{ }
			});
		}

		payloadSender.WaitForSpans();
		payloadSender.Spans.Should().HaveCount(1);
		payloadSender.FirstSpan.Name.Should().Be("receive");
		payloadSender.FirstSpan.ParentId.Should().Be(payloadSender.FirstTransaction.Id);

		payloadSender.FirstSpan.Links.Should().ContainSingle()
			.Which.Should().Match<SpanLink>(l => l.SpanId == ProducerSpanId && l.TraceId == ProducerTraceId);
	}

	[Fact]
	public void ActivityLinksAreKeptAlongsideTheDeclaredParentLink()
	{
		const string linkedTraceId = "1af7651916cd43dd8448eb211c80319c";
		const string linkedSpanId = "c7ad6b7169203331";

		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			agent.Tracer.CaptureTransaction("Consumer-loop", "messaging", () =>
			{
				var src = new ActivitySource("Link.Messaging.Batch");
				var links = new[] { new ActivityLink(ActivityContext.Parse($"00-{linkedTraceId}-{linkedSpanId}-01", null)) };
				using (src.StartActivity("receive", ActivityKind.Consumer, ProducerContext, links: links))
				{ }
			});
		}

		payloadSender.WaitForSpans();
		payloadSender.FirstSpan.Links.Should().HaveCount(2);
		payloadSender.FirstSpan.Links.Should().Contain(l => l.SpanId == linkedSpanId && l.TraceId == linkedTraceId);
		payloadSender.FirstSpan.Links.Should().Contain(l => l.SpanId == ProducerSpanId && l.TraceId == ProducerTraceId);
	}

	[Fact]
	public void RootActivityInsideATransactionGetsNoSyntheticLink()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			agent.Tracer.CaptureTransaction("T", "request", () =>
			{
				var src = new ActivitySource("Link.NoParent");
				using (src.StartActivity("work"))
				{ }
			});
		}

		payloadSender.WaitForSpans();
		payloadSender.FirstSpan.Name.Should().Be("work");
		payloadSender.FirstSpan.Links.Should().BeNullOrEmpty();
	}

	/// <summary>
	/// Without an ambient transaction the declared parent is honoured as a distributed trace continuation, so there is
	/// nothing to record as a link.
	/// </summary>
	[Fact]
	public void RemoteParentHonouredAsATransactionGetsNoSyntheticLink()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			var src = new ActivitySource("Link.NoAmbient");
			using (src.StartActivity("receive", ActivityKind.Consumer, ProducerContext))
			{ }
		}

		payloadSender.WaitForTransactions();
		payloadSender.Transactions.Should().HaveCount(1);
		payloadSender.FirstTransaction.TraceId.Should().Be(ProducerTraceId);
		payloadSender.FirstTransaction.ParentId.Should().Be(ProducerSpanId);
		payloadSender.Spans.Should().BeEmpty();
	}

	/// <summary>
	/// A null Activity.Parent does not prove the declared parent is remote: an activity started from an ActivityContext
	/// the caller already had in hand arrives the same way. When that context is the segment the span is nested under,
	/// a link would point at the span's own parent.
	/// </summary>
	[Fact]
	public void DeclaredParentWhichIsTheElasticParentGetsNoLink()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			agent.Tracer.CaptureTransaction("T", "request", () =>
			{
				var src = new ActivitySource("Link.AmbientContext");
				using (src.StartActivity("work", ActivityKind.Internal, Activity.Current!.Context))
				{ }
			});
		}

		payloadSender.WaitForSpans();
		payloadSender.FirstSpan.Name.Should().Be("work");
		payloadSender.FirstSpan.ParentId.Should().Be(payloadSender.FirstTransaction.Id);
		payloadSender.FirstSpan.Links.Should().BeNullOrEmpty();
	}

	/// <summary>
	/// The declared context need not be the immediate parent. Started from the transaction's context while an Elastic
	/// span is current, the activity nests under that span and the declared parent is its grandparent: still part of
	/// this trace, so a link would only point back into the same waterfall.
	/// </summary>
	[Fact]
	public void DeclaredParentWhichIsAnAncestorSegmentGetsNoLink()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			agent.Tracer.CaptureTransaction("T", "request", () =>
			{
				var transactionContext = Activity.Current!.Context;
				agent.Tracer.CurrentTransaction!.CaptureSpan("elastic-span", "app", () =>
				{
					var src = new ActivitySource("Link.AncestorContext");
					using (src.StartActivity("work", ActivityKind.Internal, transactionContext))
					{ }
				});
			});
		}

		payloadSender.WaitForSpans(count: 2);
		var work = (Span)payloadSender.Spans.Single(s => s.Name == "work");
		var elasticSpan = payloadSender.Spans.Single(s => s.Name == "elastic-span");

		work.ParentId.Should().Be(elasticSpan.Id);
		work.Links.Should().BeNullOrEmpty();
	}

	/// <summary>
	/// A caller which links the same context it declares as its parent gets one link, not two.
	/// </summary>
	[Fact]
	public void DeclaredParentAlreadyPresentInTheActivityLinksIsNotDuplicated()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			agent.Tracer.CaptureTransaction("Consumer-loop", "messaging", () =>
			{
				var src = new ActivitySource("Link.Messaging.Duplicate");
				var links = new[] { new ActivityLink(ProducerContext) };
				using (src.StartActivity("receive", ActivityKind.Consumer, ProducerContext, links: links))
				{ }
			});
		}

		payloadSender.WaitForSpans();
		payloadSender.FirstSpan.Links.Should().ContainSingle()
			.Which.Should().Match<SpanLink>(l => l.SpanId == ProducerSpanId && l.TraceId == ProducerTraceId);
	}
}
