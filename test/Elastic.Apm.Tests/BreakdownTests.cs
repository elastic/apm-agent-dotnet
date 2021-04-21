// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
using System.Threading;
using Elastic.Apm.Metrics;
using Elastic.Apm.Model;
using Elastic.Apm.ServerInfo;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Elastic.Apm.Tests
{
	public class BreakdownTests
	{
		/// <summary>
		/// Captures transaction ---> span ---> span
		/// Makes sure that all metrics related to breakdown are captured
		/// </summary>
		[Fact]
		public void TransactionWithSpans()
		{
			var payloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(
				new AgentComponents(
					new NoopLogger(),
					new MockConfigSnapshot(metricsInterval: "1s"),
					payloadSender,
					null, //metricsCollector will be set in AgentComponents.ctor
					new CurrentExecutionSegmentsContainer(),
					new NoopCentralConfigFetcher(),
					new MockApmServerInfo(new ElasticVersion(7, 12, 0, string.Empty))));

			Transaction transaction = null;
			Span span1 = null;
			Span span2 = null;
			agent.Tracer.CaptureTransaction("Foo", "Bar", t =>
			{
				transaction = t as Transaction;
				Thread.Sleep(100);
				t.CaptureSpan("Foo1", "Bar", s1 =>
				{
					span1 = s1 as Span;
					Thread.Sleep(100);
					s1.CaptureSpan("Foo2", "Bar2", s2 =>
					{
						span2 = s2 as Span;
						Thread.Sleep(100);
					});
				});
				Thread.Sleep(100);
			});

			payloadSender.WaitForMetrics(TimeSpan.FromSeconds(5));
			payloadSender.Metrics.Should().NotBeEmpty();

			// Assert that span2 is captured and the span.self_time.sum.us == span2.Duration
			payloadSender.Metrics.Should()
				.Contain(n =>
					n is MetricSet && ((MetricSet)n)!.Span.Type.Equals("Bar2") &&
					string.IsNullOrEmpty(((MetricSet)n)!.Span.SubType) &&
					((MetricSet)n)!.Samples.Any(sample =>
						sample.KeyValue.Key.Equals("span.self_time.sum.us") && sample.KeyValue.Value == span2.Duration!.Value) &&
					((MetricSet)n)!.Samples.Any(sample => sample.KeyValue.Key.Equals("span.self_time.count") && sample.KeyValue.Value == 1)
				);

			// Assert that span2 is captured and the span.self_time.sum.us == span1.Duration - span2.Duration
			payloadSender.Metrics.Should()
				.Contain(n =>
					n is MetricSet && ((MetricSet)n)!.Span.Type.Equals("Bar") &&
					string.IsNullOrEmpty(((MetricSet)n)!.Span.SubType) &&
					((MetricSet)n)!.Samples.Any(sample => sample.KeyValue.Key.Equals("span.self_time.sum.us") &&
						sample.KeyValue.Value == span1.Duration!.Value - span2.Duration!.Value) &&
					((MetricSet)n)!.Samples.Any(sample => sample.KeyValue.Key.Equals("span.self_time.count") && sample.KeyValue.Value == 1)
				);

			// Assert that app (the transaction) is captured and the span.self_time.sum.us == transaction.SelfDuration
			payloadSender.Metrics.Should()
				.Contain(n =>
					n is MetricSet && ((MetricSet)n)!.Span.Type.Equals("app") &&
					string.IsNullOrEmpty(((MetricSet)n)!.Span.SubType) &&
					((MetricSet)n)!.Samples.Any(sample => sample.KeyValue.Key.Equals("span.self_time.sum.us") &&
						sample.KeyValue.Value == transaction.SelfDuration) &&
					((MetricSet)n)!.Samples.Any(sample => sample.KeyValue.Key.Equals("span.self_time.count") && sample.KeyValue.Value == 1)
				);
		}
	}
}
