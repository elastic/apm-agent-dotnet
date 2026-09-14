// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Model;
using Elastic.Apm.Report.Serialization;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	/// <summary>
	/// Regression tests for the span compression buffer handling in <see cref="Span.End" /> and <see cref="Transaction.End" />.
	/// Spec: https://github.com/elastic/apm/blob/main/specs/agents/handling-huge-traces/tracing-spans-compress.md
	/// <para>
	/// Ended, sampled spans must be reported exactly once unless explicitly dropped or abandoned. Compression must preserve
	/// their total count and the parent references of reported children.
	/// </para>
	/// The OpenTelemetry bridge is disabled for the same reason as in <see cref="SpanCompressionTests" />.
	/// </summary>
	public class SpanCompressionBufferTests
	{
		private static ApmAgent CreateAgent(MockPayloadSender payloadSender, string exitSpanMinDuration = "0",
			string sameKindMaxDuration = null, string exactMatchMaxDuration = null, bool spanCompressionEnabled = true,
			string transactionMaxSpans = null
		) =>
			new(new TestAgentComponents(apmServerInfo: MockApmServerInfo.Version80, payloadSender: payloadSender,
				configuration: new MockConfiguration(spanCompressionEnabled: spanCompressionEnabled.ToString(), exitSpanMinDuration: exitSpanMinDuration,
					spanCompressionSameKindMaxDuration: sameKindMaxDuration, spanCompressionExactMatchMaxDuration: exactMatchMaxDuration,
					transactionMaxSpans: transactionMaxSpans, openTelemetryBridgeEnabled: "false")));

		/// <summary>
		/// Starts a db exit span (the shape produced by the SQL instrumentations) under a transaction or under another span.
		/// </summary>
		private static Span StartDbSpan(IExecutionSegment parent, string name, long? timestamp = null, string instance = "01",
			bool makeCurrent = true)
		{
			var span = parent switch
			{
				Transaction transaction => transaction.StartSpanInternal(name, ApiConstants.TypeDb, ApiConstants.SubtypeMssql,
					timestamp: timestamp, isExitSpan: true, makeCurrent: makeCurrent),
				Span parentSpan => parentSpan.StartSpanInternal(name, ApiConstants.TypeDb, ApiConstants.SubtypeMssql,
					timestamp: timestamp, isExitSpan: true, makeCurrent: makeCurrent),
				_ => throw new ArgumentException("Unexpected execution segment type", nameof(parent))
			};

			span.Context.Db = new Database { Type = Database.TypeSql, Instance = instance };
			return span;
		}

		/// <summary>
		/// The number of ended spans a reported span stands for: itself, or every span merged into it when it is a composite.
		/// </summary>
		private static int Represents(ISpan span) => (span as Span)?.Composite?.Count ?? 1;

		private static IEnumerable<Span> Composites(MockPayloadSender payloadSender) =>
			payloadSender.Spans.OfType<Span>().Where(s => s.Composite != null);

		private static void AssertNoOrphanedSpans(MockPayloadSender payloadSender)
		{
			var ids = new HashSet<string>(payloadSender.Spans.Select(s => s.Id)
				.Concat(payloadSender.Transactions.Select(t => t.Id)));
			payloadSender.Spans.Should().OnlyContain(s => ids.Contains(s.ParentId),
				"compression and fast-exit dropping must preserve the parents of reported spans");
		}

		/// <summary>
		/// The shape seen in the field when two instrumentations trace the same SQL command: an outer db exit span with a
		/// db exit child of the same name, repeated for every command in the transaction. The child ends first with a precise
		/// duration, the outer span ends with a duration of 0 (coarse clock). Despite matching the same-kind duration threshold,
		/// the outer spans must be retained individually because their children reference them.
		/// Before the fix the child fell through to the transaction's compression buffer, reported that buffered span without
		/// replacing it, and the very same span object was then enqueued once per subsequent command and once more when the
		/// transaction ended. The children were never reported at all.
		/// </summary>
		[Fact]
		public void NestedExitSpans_AreReportedExactlyOnce()
		{
			var payloadSender = new MockPayloadSender();
			var innerIds = new List<string>();
			const int commands = 5;

			using (var agent = CreateAgent(payloadSender))
			{
				var transaction = agent.Tracer.StartTransaction("GET Home/Index", ApiConstants.TypeRequest);

				for (var i = 0; i < commands; i++)
				{
					var outer = StartDbSpan(transaction, $"[dbo].[usp{i}]");
					var inner = StartDbSpan(outer, $"[dbo].[usp{i}]");
					innerIds.Add(inner.Id);

					inner.Duration = 1.2;
					inner.End();

					outer.Duration = 0;
					outer.End();
				}

				transaction.End();
			}

			payloadSender.Spans.Select(s => s.Id).Should().OnlyHaveUniqueItems("a span must never be reported twice");
			payloadSender.Spans.Sum(Represents).Should().Be(commands * 2, "every ended span must be accounted for exactly once");

			// the children are reported individually when their parent ends
			foreach (var innerId in innerIds)
				payloadSender.Spans.Should().ContainSingle(s => s.Id == innerId);

			Composites(payloadSender).Should().BeEmpty();
			payloadSender.Spans.Should().HaveCount(commands * 2);
			AssertNoOrphanedSpans(payloadSender);
		}

		/// <summary>
		/// Parents with identical names and short durations must not be compressed away when their buffered children
		/// are reported, otherwise the children reference parent IDs that will never be sent.
		/// </summary>
		[Fact]
		public void BufferedChildSpan_IsReported_AndItsParentIsNotCompressedAway()
		{
			var payloadSender = new MockPayloadSender();
			var childIds = new List<string>();
			const int commands = 3;

			using (var agent = CreateAgent(payloadSender))
			{
				var transaction = agent.Tracer.StartTransaction("GET Home/Index", ApiConstants.TypeRequest);

				for (var i = 0; i < commands; i++)
				{
					var outer = StartDbSpan(transaction, "SELECT * FROM Table");
					var child = StartDbSpan(outer, "SELECT * FROM Table");
					childIds.Add(child.Id);

					child.Duration = 1;
					child.End();

					outer.Duration = 1;
					outer.End();
				}

				transaction.End();
			}

			payloadSender.Spans.Select(s => s.Id).Should().OnlyHaveUniqueItems();
			payloadSender.Spans.Sum(Represents).Should().Be(commands * 2);

			foreach (var childId in childIds)
				payloadSender.Spans.Should().ContainSingle(s => s.Id == childId);

			Composites(payloadSender).Should().BeEmpty();
			payloadSender.Spans.Should().HaveCount(commands * 2);
			AssertNoOrphanedSpans(payloadSender);
		}

		[Theory]
		[InlineData(true, true, true)]
		[InlineData(true, true, false)]
		[InlineData(true, false, true)]
		[InlineData(true, false, false)]
		[InlineData(false, true, true)]
		[InlineData(false, true, false)]
		[InlineData(false, false, true)]
		[InlineData(false, false, false)]
		public void FastParent_WithReportedChild_IsNotDropped(bool compressionEnabled, bool childEndsFirst, bool makeChildCurrent)
		{
			var payloadSender = new MockPayloadSender();
			using var agent = CreateAgent(payloadSender, exitSpanMinDuration: "10ms", spanCompressionEnabled: compressionEnabled);
			var transaction = agent.Tracer.StartTransaction("transaction", "type");
			var parent = StartDbSpan(transaction, "parent");
			var child = StartDbSpan(parent, "child", makeCurrent: makeChildCurrent);
			parent.Duration = 1;
			child.Duration = 20;

			if (childEndsFirst)
				child.End();

			parent.End();
			child.End();
			transaction.End();

			payloadSender.Spans.Should().HaveCount(2);
			payloadSender.Spans.Select(s => s.Id).Should().OnlyHaveUniqueItems();
			payloadSender.Spans.Should().ContainSingle(s => s.Id == parent.Id);
			payloadSender.Spans.Should().ContainSingle(s => s.Id == child.Id && s.ParentId == parent.Id);
			AssertNoOrphanedSpans(payloadSender);
		}

		[Fact]
		public void ParentWithCompressibleChildren_IsRetained_WhileChildrenStillCompress()
		{
			var payloadSender = new MockPayloadSender();
			using var agent = CreateAgent(payloadSender);
			var transaction = agent.Tracer.StartTransaction("transaction", "type");

			for (var i = 0; i < 2; i++)
			{
				var parent = StartDbSpan(transaction, "parent");
				for (var j = 0; j < 2; j++)
				{
					var child = StartDbSpan(parent, "child");
					child.Duration = 1;
					child.End();
				}
				parent.Duration = 1;
				parent.End();
			}
			transaction.End();

			payloadSender.Spans.Should().HaveCount(4);
			payloadSender.Spans.Sum(Represents).Should().Be(6);
			Composites(payloadSender).Should().HaveCount(2).And.OnlyContain(s => s.Name == "child" && s.Composite.Count == 2);
			AssertNoOrphanedSpans(payloadSender);
		}

		/// <summary>
		/// A buffered span that acquires a child keeps its own id when it absorbs a sibling, so the child is not orphaned
		/// and compression may continue. Only the sibling merged into the composite loses its identity, which is why
		/// <see cref="Span.IsCompressionEligible" /> is checked on the ending span rather than on the buffered one.
		/// </summary>
		[Fact]
		public void BufferedSpan_AcquiringChild_StillCompressesWithSibling_AndKeepsItsChild()
		{
			var payloadSender = new MockPayloadSender();
			using var agent = CreateAgent(payloadSender);
			var transaction = agent.Tracer.StartTransaction("transaction", "type");
			var parent = StartDbSpan(transaction, "SELECT 1");
			parent.Duration = 1;
			parent.End();

			var child = StartDbSpan(parent, "child");
			var sibling = StartDbSpan(transaction, "SELECT 1");
			sibling.Duration = 1;
			sibling.End();
			child.End();
			transaction.End();

			payloadSender.Spans.Should().HaveCount(2);
			payloadSender.Spans.Sum(Represents).Should().Be(3);
			Composites(payloadSender).Should().ContainSingle().Which.Id.Should().Be(parent.Id);
			payloadSender.Spans.Should().ContainSingle(s => s.Id == child.Id && s.ParentId == parent.Id);
			AssertNoOrphanedSpans(payloadSender);
		}

		/// <summary>
		/// A child that can never reach APM Server cannot be orphaned, so it must not stop its parent from being compressed.
		/// Otherwise compression switches off once <c>transaction_max_spans</c> is reached, which is exactly the huge trace
		/// it exists to shrink.
		/// </summary>
		[Fact]
		public void ChildDroppedByMaxSpans_DoesNotPreventItsParentFromCompressing()
		{
			var payloadSender = new MockPayloadSender();
			using var agent = CreateAgent(payloadSender, transactionMaxSpans: "2");
			var transaction = agent.Tracer.StartTransaction("transaction", "type");

			var first = StartDbSpan(transaction, "SELECT 1");
			var second = StartDbSpan(transaction, "SELECT 1");

			// the third span of the transaction exceeds transaction_max_spans and is never reported
			var droppedChild = StartDbSpan(first, "child");
			droppedChild.Duration = 1;
			droppedChild.End();

			first.Duration = 1;
			first.End();
			second.Duration = 1;
			second.End();
			transaction.End();

			var composite = payloadSender.Spans.Should().ContainSingle().Which.As<Span>();
			composite.Id.Should().Be(first.Id);
			composite.Composite.Count.Should().Be(2);
			payloadSender.FirstTransaction.SpanCount.Dropped.Should().Be(1);
		}

		/// <summary>
		/// Spans discarded for being faster than <c>exit_span_min_duration</c> are not reported, so they move from the
		/// started count to the dropped count, a composite counting for every span it represents.
		/// </summary>
		[Fact]
		public void DroppedFastExitSpans_AreCountedInSpanCount()
		{
			var payloadSender = new MockPayloadSender();
			using var agent = CreateAgent(payloadSender, exitSpanMinDuration: "10ms");
			var transaction = agent.Tracer.StartTransaction("transaction", "type");
			var start = TimeUtils.TimestampNow();

			// two fast spans that compress into a composite which is then dropped as a whole
			var first = StartDbSpan(transaction, "SELECT 1", timestamp: start);
			first.Duration = 1;
			first.End();
			var second = StartDbSpan(transaction, "SELECT 1", timestamp: start + 3000);
			second.Duration = 1;
			second.End();

			// ends the compression sequence and is itself slow enough to be kept
			var slow = StartDbSpan(transaction, "SELECT 2", timestamp: start + 10_000);
			slow.Duration = 20;
			slow.End();

			transaction.End();

			payloadSender.Spans.Should().ContainSingle().Which.Id.Should().Be(slow.Id);
			payloadSender.FirstTransaction.SpanCount.Started.Should().Be(1);
			payloadSender.FirstTransaction.SpanCount.Dropped.Should().Be(2);
		}

		[Fact]
		public void Grandchild_DoesNotConsumeTransactionsCompressionBuffer()
		{
			var payloadSender = new MockPayloadSender();
			using var agent = CreateAgent(payloadSender);
			var transaction = agent.Tracer.StartTransaction("transaction", "type");
			var sibling = StartDbSpan(transaction, "SELECT 1");
			sibling.Duration = 1;
			sibling.End();

			var parent = StartDbSpan(transaction, "parent");
			var child = StartDbSpan(parent, "SELECT 1");
			child.Duration = 1;
			child.End();
			payloadSender.Spans.Should().BeEmpty("the child belongs in its direct parent's buffer");

			parent.End();
			transaction.End();

			payloadSender.Spans.Should().HaveCount(3);
			payloadSender.Spans.Select(s => s.Id).Should().OnlyHaveUniqueItems();
			Composites(payloadSender).Should().BeEmpty();
			AssertNoOrphanedSpans(payloadSender);
		}

		/// <summary>
		/// The spec treats the transaction like any other parent: a compression eligible span that ends after its parent has
		/// ended must be reported immediately. Before the fix such a span was placed into the transaction's compression buffer
		/// after that buffer had already been flushed, so it was never reported.
		/// </summary>
		[Fact]
		public void EligibleSpan_EndingAfterTransactionEnded_IsReported()
		{
			var payloadSender = new MockPayloadSender();

			using (var agent = CreateAgent(payloadSender))
			{
				var transaction = agent.Tracer.StartTransaction("GET Home/Index", ApiConstants.TypeRequest);
				var span = StartDbSpan(transaction, "SELECT 1");

				transaction.End();
				span.End();

				payloadSender.Spans.Should().ContainSingle().Which.Id.Should().Be(span.Id);
			}
		}

		/// <summary>
		/// A span abandoned by an instrumentation (see <see cref="Span.Abandon" />) is not reported itself, but a child it
		/// buffered has ended normally and must still be reported.
		/// </summary>
		[Fact]
		public void AbandonedParent_StillReportsItsBufferedChild()
		{
			var payloadSender = new MockPayloadSender();

			using (var agent = CreateAgent(payloadSender))
			{
				var transaction = agent.Tracer.StartTransaction("GET Home/Index", ApiConstants.TypeRequest);
				var parent = StartDbSpan(transaction, "outer");
				var child = StartDbSpan(parent, "inner");

				child.Duration = 1;
				child.End();

				parent.Abandon();
				transaction.End();

				payloadSender.Spans.Should().ContainSingle().Which.Id.Should().Be(child.Id);
			}
		}

		/// <summary>
		/// The exit_span_min_duration threshold applies to buffered spans too. Before the fix, the span left in the
		/// transaction's compression buffer was queued directly by <see cref="Transaction.End" /> and bypassed the check that
		/// every other reported span goes through.
		/// </summary>
		[Fact]
		public void FastExitSpan_FlushedAtTransactionEnd_IsDroppedByExitSpanMinDuration()
		{
			var payloadSender = new MockPayloadSender();

			using (var agent = CreateAgent(payloadSender, exitSpanMinDuration: "10ms"))
			{
				var transaction = agent.Tracer.StartTransaction("GET Home/Index", ApiConstants.TypeRequest);
				var span = StartDbSpan(transaction, "SELECT 1");

				span.Duration = 1;
				span.End();
				transaction.End();
			}

			payloadSender.Spans.Should().BeEmpty();
			var stats = payloadSender.FirstTransaction.DroppedSpanStats.Should().ContainSingle().Subject;
			stats.ServiceTargetType.Should().Be(ApiConstants.SubtypeMssql);
			stats.Duration.Count.Should().Be(1);
			stats.Duration.Sum.Us.Should().Be(1000);
		}

		[Theory]
		[InlineData("transaction", false)]
		[InlineData("transaction", true)]
		[InlineData("sibling", false)]
		[InlineData("sibling", true)]
		[InlineData("parent", false)]
		[InlineData("parent", true)]
		public void DroppedComposite_PreservesCallCountAndNetDuration(string flushTrigger, bool sameKind)
		{
			var payloadSender = new MockPayloadSender();
			using var agent = CreateAgent(payloadSender, exitSpanMinDuration: "10ms", sameKindMaxDuration: "1ms");
			var transaction = agent.Tracer.StartTransaction("transaction", "type");
			var parent = flushTrigger == "parent" ? transaction.StartSpan("parent", "app") : null;
			var owner = parent ?? (IExecutionSegment)transaction;
			var start = TimeUtils.TimestampNow();

			var first = StartDbSpan(owner, "SELECT 1", timestamp: start);
			first.Duration = 1;
			first.End();
			var second = StartDbSpan(owner, sameKind ? "SELECT 2" : "SELECT 1", timestamp: start + 3000);
			second.Duration = 1;
			second.End();

			first.Composite.Should().NotBeNull();
			first.Composite.CompressionStrategy.Should().Be(sameKind ? "same_kind" : "exact_match");
			first.Duration.Should().Be(4);

			if (flushTrigger == "sibling")
				owner.StartSpan("flush", "app").End();
			parent?.End();
			transaction.End();

			payloadSender.Spans.Should().NotContain(s => s.Type == ApiConstants.TypeDb);
			var stats = payloadSender.FirstTransaction.DroppedSpanStats.Should().ContainSingle().Subject;
			stats.ServiceTargetType.Should().Be(ApiConstants.SubtypeMssql);
			stats.ServiceTargetName.Should().Be("01");
			stats.Outcome.Should().Be(Outcome.Success);
			stats.Duration.Count.Should().Be(2);
			stats.Duration.Sum.UsRaw.Should().Be(2000);
			stats.Duration.Sum.Us.Should().Be(2000);
			new PayloadItemSerializer().Serialize(payloadSender.FirstTransaction)
				.Should().Contain("\"duration\":{\"count\":2,\"sum\":{\"us\":2000}}");
		}

		[Fact]
		public void DroppedComposites_AccumulateWithIndividualSpans()
		{
			var payloadSender = new MockPayloadSender();
			using var agent = CreateAgent(payloadSender, exitSpanMinDuration: "10ms");
			var transaction = agent.Tracer.StartTransaction("transaction", "type");
			var start = TimeUtils.TimestampNow();

			for (var group = 0; group < 3; group++)
			{
				var count = group == 1 ? 1 : 2;
				for (var i = 0; i < count; i++)
				{
					var span = StartDbSpan(transaction, "SELECT 1", timestamp: start + group * 10000 + i * 3000);
					span.Duration = 1;
					span.End();
				}
				transaction.StartSpan("flush", "app").End();
			}
			transaction.End();

			payloadSender.Spans.Should().HaveCount(3).And.OnlyContain(s => s.Type == "app");
			var stats = payloadSender.FirstTransaction.DroppedSpanStats.Should().ContainSingle().Subject;
			stats.Duration.Count.Should().Be(5);
			stats.Duration.Sum.Us.Should().Be(5000);
		}

		/// <summary>
		/// When a buffered span is dropped because a non-compressible sibling ends, the dropped span statistics must describe
		/// the dropped span, not the sibling that caused it to be flushed. Before the fix the sibling's context was used, which
		/// here has no destination at all, so nothing was recorded.
		/// </summary>
		[Fact]
		public void DroppedSpanStats_DescribeTheDroppedSpan_NotTheSpanThatFlushedIt()
		{
			var payloadSender = new MockPayloadSender();

			using (var agent = CreateAgent(payloadSender, exitSpanMinDuration: "10ms"))
			{
				var transaction = agent.Tracer.StartTransaction("GET Home/Index", ApiConstants.TypeRequest);

				var fastDbSpan = StartDbSpan(transaction, "SELECT 1", instance: "dbA");
				fastDbSpan.Duration = 1;
				fastDbSpan.End();

				// a non-exit span is never compression eligible, so ending it flushes (and here drops) the buffered db span
				var appSpan = transaction.StartSpan("work", "app");
				appSpan.End();

				transaction.End();

				payloadSender.Spans.Should().ContainSingle().Which.Id.Should().Be(appSpan.Id);
			}

			var stats = payloadSender.FirstTransaction.DroppedSpanStats.Should().ContainSingle().Subject;
			stats.ServiceTargetType.Should().Be(ApiConstants.SubtypeMssql);
			stats.ServiceTargetName.Should().Be("dbA");
			stats.Duration.Count.Should().Be(1);
		}

		/// <summary>
		/// The duration of a composite span is the gross duration of the spans it represents: the end of the last compressed
		/// span minus the start of the first one. Before the fix the duration was recomputed as "now" when the composite was
		/// reported, which added the idle time between the last compressed span and the end of the transaction.
		/// </summary>
		[Fact]
		public void CompositeDuration_IsGrossDurationOfCompressedSpans_NotTheReportTime()
		{
			var payloadSender = new MockPayloadSender();

			using (var agent = CreateAgent(payloadSender))
			{
				var transaction = agent.Tracer.StartTransaction("GET Home/Index", ApiConstants.TypeRequest);
				var start = TimeUtils.TimestampNow();

				var first = StartDbSpan(transaction, "SELECT 1", timestamp: start);
				first.Duration = 5;
				first.End();

				// starts 10ms after the first span and lasts 5ms, so the pair spans 15ms gross with a net sum of 10ms
				var second = StartDbSpan(transaction, "SELECT 1", timestamp: start + 10_000);
				second.Duration = 5;
				second.End();

				Thread.Sleep(60);
				transaction.End();
			}

			var composite = payloadSender.Spans.Should().ContainSingle().Which.As<Span>();
			composite.Composite.Should().NotBeNull();
			composite.Composite.Count.Should().Be(2);
			composite.Composite.Sum.Should().Be(10);
			composite.Duration.Should().Be(15);
		}

		[Fact]
		public void ConcurrentlyEndingParentsAndChildren_PreserveParentReferences()
		{
			var payloadSender = new MockPayloadSender();
			using var agent = CreateAgent(payloadSender, exitSpanMinDuration: "10ms");
			var transaction = agent.Tracer.StartTransaction("transaction", "type");
			var spans = new List<Span>();
			for (var i = 0; i < 100; i++)
			{
				var parent = StartDbSpan(transaction, "parent", makeCurrent: false);
				var child = StartDbSpan(parent, "child", makeCurrent: false);
				parent.Duration = 1;
				child.Duration = 20;
				spans.Add(parent);
				spans.Add(child);
			}

			Parallel.ForEach(spans, new ParallelOptions { MaxDegreeOfParallelism = 8 }, span => span.End());
			transaction.End();

			payloadSender.Spans.Should().HaveCount(spans.Count);
			payloadSender.Spans.Select(s => s.Id).Should().OnlyHaveUniqueItems();
			AssertNoOrphanedSpans(payloadSender);
		}

		/// <summary>
		/// The spec requires setting and retrieving the buffered span to be atomic. Siblings ending concurrently must neither
		/// report the same buffered span twice nor overwrite (and thereby lose) each other's buffered span.
		/// </summary>
		[Fact]
		public void ConcurrentlyEndingSiblings_AreNeitherLostNorDuplicated()
		{
			const int spansPerTransaction = 400;

			for (var iteration = 0; iteration < 5; iteration++)
			{
				var payloadSender = new MockPayloadSender();

				using (var agent = CreateAgent(payloadSender))
				{
					var transaction = agent.Tracer.StartTransaction("GET Home/Index", ApiConstants.TypeRequest);

					// a mix of names and durations so that some siblings compress and others do not
					var spans = Enumerable.Range(0, spansPerTransaction)
						.Select(i => StartDbSpan(transaction, $"SELECT {i % 4}"))
						.ToList();

					for (var i = 0; i < spans.Count; i++)
						spans[i].Duration = i % 3;

					Parallel.ForEach(spans, new ParallelOptions { MaxDegreeOfParallelism = 8 }, span => span.End());

					transaction.End();
				}

				payloadSender.Spans.Select(s => s.Id).Should().OnlyHaveUniqueItems($"iteration {iteration}");
				payloadSender.Spans.Sum(Represents).Should().Be(spansPerTransaction, $"iteration {iteration}");
			}
		}
	}
}
