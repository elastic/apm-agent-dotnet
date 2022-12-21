// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	/// <summary>
	/// Tests <see cref="Transaction.DroppedSpanStats" />
	/// </summary>
	public class DroppedSpansStatsTests
	{
		[Fact]
		public void DroppedSpanStats_MustReflect_ExitSpanMinDuration_Configuration()
		{
			Helper_CreateSpanWithDuration(ConfigConsts.DefaultValues.ExitSpanMinDuration, 0).Should().BeNull();
			Helper_CreateSpanWithDuration("Oms", 0).Should().BeNull();
			Helper_CreateSpanWithDuration("10ms", 0).Count().Should().Be(1);
		}

		private static IEnumerable<DroppedSpanStats> Helper_CreateSpanWithDuration(string exitSpanMinDuration, double actualSpanDuration)
		{
			var payloadSender = new MockPayloadSender();
			using (var agent =
			       new ApmAgent(new TestAgentComponents(configuration: new MockConfiguration(exitSpanMinDuration: exitSpanMinDuration),
				       payloadSender: payloadSender)))
			{
				agent.Tracer.CaptureTransaction("transaction", "type", transaction =>
				{
					var span = transaction.StartSpan("exit_span", "type", isExitSpan: true);
					span.Duration = actualSpanDuration;
					span.End();
				});
			}

			return payloadSender.FirstTransaction.DroppedSpanStats;
		}

		[Fact]
		public void SingleDroppedSpanTest()
		{
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
				configuration: new MockConfiguration(transactionMaxSpans: "1"))))
			{
				var transaction = agent.Tracer.StartTransaction("foo", "test");
				//This is the span which won't be dropped
				transaction.CaptureSpan("fooSpan", "test", () => { });

				//This span will be dropped
				var span1 = transaction.StartSpan("foo", "bar", isExitSpan: true);
				span1.Context.Http = new Http { Method = "GET", StatusCode = 200, Url = "https://foo.bar" };
				span1.Duration = 100;
				span1.End();

				transaction.End();
			}

			payloadSender.Spans.Should().HaveCount(1);

			payloadSender.FirstTransaction.DroppedSpanStats.Should().NotBeNullOrEmpty();
			payloadSender.FirstTransaction.DroppedSpanStats.Should().HaveCount(1);
			payloadSender.FirstTransaction.DroppedSpanStats.First().DestinationServiceResource.Should().Be("foo.bar:443");
			payloadSender.FirstTransaction.DroppedSpanStats.First().ServiceTargetName.Should().Be("foo.bar:443");
			payloadSender.FirstTransaction.DroppedSpanStats.First().ServiceTargetType.Should().Be("bar");
			payloadSender.FirstTransaction.DroppedSpanStats.First().Outcome.Should().Be(Outcome.Success);
			payloadSender.FirstTransaction.DroppedSpanStats.First().DurationCount.Should().Be(1);
			payloadSender.FirstTransaction.DroppedSpanStats.First().DurationSumUs.Should().Be(100);
		}

		[Fact]
		public void MultipleDroppedSpanTest()
		{
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
				configuration: new MockConfiguration(transactionMaxSpans: "1"))))
			{
				var transaction = agent.Tracer.StartTransaction("foo", "test");
				//This is the span which won't be dropped
				transaction.CaptureSpan("fooSpan", "test", () => { });

				//Next spans will be dropped
				var span1 = transaction.StartSpan("foo", "bar", isExitSpan: true);
				span1.Context.Http = new Http { Method = "GET", StatusCode = 200, Url = "https://foo.bar" };
				span1.Duration = 100;
				span1.End();

				var span2 = transaction.StartSpan("foo", "bar", isExitSpan: true);
				span2.Context.Http = new Http { Method = "GET", StatusCode = 200, Url = "https://foo.bar" };
				span2.Duration = 150;
				span2.End();

				var span3 = transaction.StartSpan("foo", "bar", isExitSpan: true);
				span3.Context.Http = new Http { Method = "GET", StatusCode = 400, Url = "https://foo.bar" };
				span3.Outcome = Outcome.Failure;
				span3.Duration = 50;
				span3.End();

				var span4 = transaction.StartSpan("foo", "bar", isExitSpan: true);
				span4.Context.Http = new Http { Method = "GET", StatusCode = 400, Url = "https://foo2.bar" };
				span4.Duration = 15;
				span4.End();

				for (var i = 0; i < 50; i++)
				{
					var span5 = transaction.StartSpan("foo", "bar", isExitSpan: true);
					span5.Context.Destination = new Destination { Service = new Destination.DestinationService { Resource = "mysql" } };
					span5.Context.Db = new Database { Instance = "instance1", Type = "mysql", Statement = "Select Foo From Bar" };
					span5.Duration = 50;
					span5.End();
				}

				transaction.End();
			}

			payloadSender.Spans.Should().HaveCount(1);

			payloadSender.FirstTransaction.DroppedSpanStats.Should().NotBeNullOrEmpty();
			payloadSender.FirstTransaction.DroppedSpanStats.Should().HaveCount(4);

			payloadSender.FirstTransaction.DroppedSpanStats.Should()
				.Contain(n => n.Outcome == Outcome.Success
					&& n.DurationCount == 2 && Math.Abs(n.DurationSumUs - 250) < 1 && n.DestinationServiceResource == "foo.bar:443"
					&& n.ServiceTargetName == "foo.bar:443" && n.ServiceTargetType == "bar");

			payloadSender.FirstTransaction.DroppedSpanStats.Should()
				.Contain(n => n.Outcome == Outcome.Failure
					&& n.DurationCount == 1 && Math.Abs(n.DurationSumUs - 50) < 1 && n.DestinationServiceResource == "foo.bar:443"
					&& n.ServiceTargetName == "foo.bar:443" && n.ServiceTargetType == "bar");

			payloadSender.FirstTransaction.DroppedSpanStats.Should()
				.Contain(n => n.Outcome == Outcome.Success
					&& n.DurationCount == 1 && Math.Abs(n.DurationSumUs - 15) < 1 && n.DestinationServiceResource == "foo2.bar:443"
					&& n.ServiceTargetName == "foo2.bar:443" && n.ServiceTargetType == "bar");

			payloadSender.FirstTransaction.DroppedSpanStats.Should()
				.Contain(n => n.Outcome == Outcome.Success
					&& n.DurationCount == 50 && Math.Abs(n.DurationSumUs - 50 * 50) < 1 && n.DestinationServiceResource == "mysql");
		}

		/// <summary>
		/// Tests the fix 128 upper limit
		/// </summary>
		[Fact]
		public void MaxDroppedSpanStatsTest()
		{
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
				configuration: new MockConfiguration(transactionMaxSpans: "1"))))
			{
				var transaction = agent.Tracer.StartTransaction("foo", "test");
				//This is the span which won't be dropped
				transaction.CaptureSpan("fooSpan", "test", () => { });

				//Next spans will be dropped
				for (var i = 0; i < 500; i++)
				{
					var span1 = transaction.StartSpan("foo", "bar", isExitSpan: true);
					span1.Context.Http = new Http { Method = "GET", StatusCode = 200, Url = $"https://foo{i}.bar" };
					span1.Duration = 100;
					span1.End();
				}

				transaction.End();
			}

			payloadSender.FirstTransaction.DroppedSpanStats.Should().HaveCount(128);
		}

		/// <summary>
		/// Testing with custom spans without touching Span.Context
		/// </summary>
		[Fact]
		public void SimpleDroppedSpans()
		{
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
				configuration: new MockConfiguration(transactionMaxSpans: "1"))))
			{
				var transaction = agent.Tracer.StartTransaction("foo", "test");
				//This is the span which won't be dropped
				transaction.CaptureSpan("fooSpan", "test", () => { });

				//Next spans will be dropped
				for (var i = 0; i < 500; i++)
				{
					var span = transaction.StartSpan("foo", "bar", isExitSpan: true);
					span.End();
				}

				transaction.End();
			}
			payloadSender.FirstTransaction.DroppedSpanStats.Should().HaveCount(1);

			payloadSender.FirstTransaction.DroppedSpanStats.First().DestinationServiceResource.Should().Be("bar");
			payloadSender.FirstTransaction.DroppedSpanStats.First().ServiceTargetType.Should().Be("bar");
			payloadSender.FirstTransaction.DroppedSpanStats.First().ServiceTargetName.Should().BeNullOrEmpty();
			payloadSender.FirstTransaction.DroppedSpanStats.First().DurationCount.Should().Be(500);
		}
	}
}
