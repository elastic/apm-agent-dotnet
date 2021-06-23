// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
using System.Threading;
using Elastic.Apm.Metrics;
using Elastic.Apm.Metrics.MetricsProvider;
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

			payloadSender.WaitForMetrics(TimeSpan.FromSeconds(15));
			payloadSender.Metrics.Should().NotBeEmpty();

			// Assert that span2 is captured and the span.self_time.sum.us == span2.Duration
			payloadSender.Metrics.Should()
				.Contain(n =>
					n is MetricSet && ((MetricSet)n)!.Span.Type.Equals("Bar2") &&
					string.IsNullOrEmpty(((MetricSet)n)!.Span.SubType) &&
					((MetricSet)n)!.Samples.Any(sample =>
						sample.KeyValue.Key.Equals("span.self_time.sum.us") &&
						DoubleCompare(sample.KeyValue.Value, span2.Duration!.Value * 1000)) &&
					((MetricSet)n)!.Samples.Any(sample => sample.KeyValue.Key.Equals("span.self_time.count") && sample.KeyValue.Value == 1)
				);

			// Assert that span2 is captured and the span.self_time.sum.us == span1.Duration - span2.Duration
			payloadSender.Metrics.Should()
				.Contain(n =>
					n is MetricSet && ((MetricSet)n)!.Span.Type.Equals("Bar") &&
					string.IsNullOrEmpty(((MetricSet)n)!.Span.SubType) &&
					((MetricSet)n)!.Samples.Any(sample => sample.KeyValue.Key.Equals("span.self_time.sum.us") &&
						DoubleCompare(sample.KeyValue.Value, (span1.Duration!.Value - span2.Duration!.Value) * 1000)) &&
					((MetricSet)n)!.Samples.Any(sample => sample.KeyValue.Key.Equals("span.self_time.count") && sample.KeyValue.Value == 1)
				);

			// Assert that app (the transaction) is captured and the span.self_time.sum.us == transaction.SelfDuration
			payloadSender.Metrics.Should()
				.Contain(n =>
					n is MetricSet && ((MetricSet)n)!.Span.Type.Equals("app") &&
					string.IsNullOrEmpty(((MetricSet)n)!.Span.SubType) &&
					((MetricSet)n)!.Samples.Any(sample => sample.KeyValue.Key.Equals("span.self_time.sum.us") &&
						DoubleCompare(sample.KeyValue.Value, transaction.SelfDuration * 1000)) &&
					((MetricSet)n)!.Samples.Any(sample => sample.KeyValue.Key.Equals("span.self_time.count") && sample.KeyValue.Value == 1)
				);
		}

		//                                  total self type
		// ██████████████████████████████    30   30 transaction
		//          10        20        30
		// {"metricset":{"timestamp":1556893458478000,"transaction":{"name":"test","type":"request"},"samples":{"transaction.duration.count":{"value":1},"transaction.duration.sum.us":{"value":30},"transaction.breakdown.count":{"value":1}}}}
		// {"metricset":{"timestamp":1556893458478000,"transaction":{"name":"test","type":"request"},"span":{"type":"app"},"samples":{"span.self_time.count":{"value":1},"span.self_time.sum.us":{"value":30}}}}
		[Fact]
		public void AcceptanceTest01()
		{
			var (agent, breakdownMetricsProvider) = SetUpAgent();
			using (agent)
			{
				var t = agent.Tracer.StartTransaction("test", "request");
				t.Duration = 0.03;
				t.End();
			}
			var metricSets = breakdownMetricsProvider.GetSamples();

			var metrics = metricSets as MetricSet[] ?? metricSets.ToArray();
			metrics.Should().NotBeNullOrEmpty();
			metrics.Count().Should().Be(2);
			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.duration.count") && s.KeyValue.Value == 1)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.duration.sum.us") && s.KeyValue.Value == 30)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.breakdown.count") && s.KeyValue.Value == 1)
				);

			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Span.Type.Equals("app")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.count") && s.KeyValue.Value == 1)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.sum.us") && s.KeyValue.Value == 30)
				);
		}

		//                                  total self type
		// ██████████░░░░░░░░░░██████████    30   20 transaction
		// └─────────██████████              10   10 db.mysql
		//          10        20        30
		// {"metricset":{"timestamp":1556893458387000,"transaction":{"name":"test","type":"request"},"samples":{"transaction.duration.count":{"value":1},"transaction.duration.sum.us":{"value":30},"transaction.breakdown.count":{"value":1}}}}
		// {"metricset":{"timestamp":1556893458387000,"transaction":{"name":"test","type":"request"},"span":{"type":"db","subtype":"mysql"},"samples":{"span.self_time.count":{"value":1},"span.self_time.sum.us":{"value":10}}}}
		// {"metricset":{"timestamp":1556893458387000,"transaction":{"name":"test","type":"request"},"span":{"type":"app"},"samples":{"span.self_time.count":{"value":1},"span.self_time.sum.us":{"value":20}}}}

		[Fact]
		public void AcceptanceTest02()
		{
			var (agent, breakdownMetricsProvider) = SetUpAgent();
			using (agent)
			{
				var t = agent.TracerInternal.StartTransactionInternal("test", "request", 0);
				t.Duration = 0.03;

				var span = t.StartSpanInternal("db.mysql", "db", "mysql", timestamp: 10);
				span.Duration = 0.01;

				span.End();

				t.End();
			}

			var metricSets = breakdownMetricsProvider.GetSamples();

			var metrics = metricSets as MetricSet[] ?? metricSets.ToArray();
			metrics.Should().NotBeNullOrEmpty();
			metrics.Count().Should().Be(3);
			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.duration.count") && s.KeyValue.Value == 1)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.duration.sum.us") && s.KeyValue.Value == 30)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.breakdown.count") && s.KeyValue.Value == 1)
				);

			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Span.Type.Equals("app")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.count") && s.KeyValue.Value == 1)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.sum.us") && DoubleCompare(s.KeyValue.Value, 20))
				);

			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Span.Type.Equals("db")
						&& n.Span.SubType.Equals("mysql")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.count") && s.KeyValue.Value == 1)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.sum.us") && DoubleCompare(s.KeyValue.Value, 10))
				);
		}

		//                                total self type
		// ██████████░░░░░░░░░░██████████    30   20 transaction
		// └─────────██████████              10   10 app
		//          10        20        30
		//{"metricset":{"timestamp":1556893458471000,"transaction":{"name":"test","type":"request"},"samples":{"transaction.duration.count":{"value":1},"transaction.duration.sum.us":{"value":30},"transaction.breakdown.count":{"value":1}}}}
		//{"metricset":{"timestamp":1556893458471000,"transaction":{"name":"test","type":"request"},"span":{"type":"app"},"samples":{"span.self_time.count":{"value":2},"span.self_time.sum.us":{"value":30}}}}
		[Fact]
		public void AcceptanceTest03()
		{
			var (agent, breakdownMetricsProvider) = SetUpAgent();
			using (agent)
			{
				var t = agent.TracerInternal.StartTransactionInternal("test", "request", 0);
				t.Duration = 0.03;

				var span = t.StartSpanInternal("foo", "app", timestamp: 10);
				span.Duration = 0.01;

				span.End();

				t.End();
			}

			var metricSets = breakdownMetricsProvider.GetSamples();

			var metrics = metricSets as MetricSet[] ?? metricSets.ToArray();
			metrics.Should().NotBeNullOrEmpty();
			metrics.Count().Should().Be(2);
			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.duration.count") && s.KeyValue.Value == 1)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.duration.sum.us") && s.KeyValue.Value == 30)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.breakdown.count") && s.KeyValue.Value == 1)
				);

			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Span.Type.Equals("app")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.count") && s.KeyValue.Value == 2)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.sum.us") && s.KeyValue.Value == 30)
				);
		}

		//                                   total self type
		// ██████████░░░░░░░░░░██████████    30   20 transaction
		// ├─────────██████████              10   10 db.mysql
		// └─────────██████████              10   10 db.mysql
		//          10        20        30
		// {"metricset":{"timestamp":1556893458375000,"transaction":{"name":"test","type":"request"},"samples":{"transaction.duration.count":{"value":1},"transaction.duration.sum.us":{"value":30},"transaction.breakdown.count":{"value":1}}}}
		// {"metricset":{"timestamp":1556893458375000,"transaction":{"name":"test","type":"request"},"span":{"type":"db","subtype":"mysql"},"samples":{"span.self_time.count":{"value":2},"span.self_time.sum.us":{"value":20}}}}
		// {"metricset":{"timestamp":1556893458375000,"transaction":{"name":"test","type":"request"},"span":{"type":"app"},"samples":{" ":{"value":1},"span.self_time.sum.us":{"value":20}}}}

		[Fact]
		public void AcceptanceTest04()
		{
			var (agent, breakdownMetricsProvider) = SetUpAgent();
			using (agent)
			{
				var t = agent.TracerInternal.StartTransactionInternal("test", "request", 0);
				t.Duration = 0.03;

				var span = t.StartSpanInternal("db.mysql", "db", "mysql", timestamp: 10);
				var span2 = t.StartSpanInternal("db.mysql", "db", "mysql", timestamp: 10);
				span.Duration = 0.01;
				span2.Duration = 0.01;

				span.End();
				span2.End();

				t.End();
			}

			var metricSets = breakdownMetricsProvider.GetSamples();

			var metrics = metricSets as MetricSet[] ?? metricSets.ToArray();
			metrics.Should().NotBeNullOrEmpty();
			metrics.Count().Should().Be(3);
			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.duration.count") && s.KeyValue.Value == 1)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.duration.sum.us") && s.KeyValue.Value == 30)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.breakdown.count") && s.KeyValue.Value == 1)
				);

			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Span.Type.Equals("app")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.count") && s.KeyValue.Value == 1)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.sum.us") && DoubleCompare(s.KeyValue.Value, 20))
				);

			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Span.Type.Equals("db")
						&& n.Span.SubType.Equals("mysql")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.count") && s.KeyValue.Value == 2)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.sum.us") && DoubleCompare(s.KeyValue.Value, 20))
				);
		}

		//                                   total self type
		// ██████████░░░░░░░░░░░░░░░█████    30   15 transaction
		// ├─────────██████████              10   10 db.mysql
		// └──────────────██████████         10   10 db.mysql
		//          10        20        30
		// {"metricset":{"timestamp":1556893458417000,"transaction":{"name":"test","type":"request"},"samples":{"transaction.duration.count":{"value":1},"transaction.duration.sum.us":{"value":30},"transaction.breakdown.count":{"value":1}}}}
		// {"metricset":{"timestamp":1556893458417000,"transaction":{"name":"test","type":"request"},"span":{"type":"db","subtype":"mysql"},"samples":{"span.self_time.count":{"value":2},"span.self_time.sum.us":{"value":20}}}}
		// {"metricset":{"timestamp":1556893458417000,"transaction":{"name":"test","type":"request"},"span":{"type":"app"},"samples":{"span.self_time.count":{"value":1},"span.self_time.sum.us":{"value":15}}}}
		[Fact]
		public void AcceptanceTest05()
		{
			var (agent, breakdownMetricsProvider) = SetUpAgent();
			using (agent)
			{
				var t = agent.TracerInternal.StartTransactionInternal("test", "request", 0);
				t.Duration = 0.03;

				var span = t.StartSpanInternal("db.mysql", "db", "mysql", timestamp: 10);
				var span2 = t.StartSpanInternal("db.mysql", "db", "mysql", timestamp: 15);
				span.Duration = 0.01;
				span2.Duration = 0.01;

				span.End();
				span2.End();

				t.End();
			}

			var metricSets = breakdownMetricsProvider.GetSamples();

			var metrics = metricSets as MetricSet[] ?? metricSets.ToArray();
			metrics.Should().NotBeNullOrEmpty();
			metrics.Count().Should().Be(3);
			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.duration.count") && s.KeyValue.Value == 1)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.duration.sum.us") && s.KeyValue.Value == 30)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.breakdown.count") && s.KeyValue.Value == 1)
				);

			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Span.Type.Equals("app")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.count") && s.KeyValue.Value == 1)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.sum.us") && DoubleCompare(s.KeyValue.Value, 15))
				);

			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Span.Type.Equals("db")
						&& n.Span.SubType.Equals("mysql")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.count") && s.KeyValue.Value == 2)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.sum.us") && DoubleCompare(s.KeyValue.Value, 20))
				);
		}

		//                                   total self type
		// █████░░░░░░░░░░░░░░░░░░░░█████    30   10 transaction
		// ├────██████████                   10   10 db.mysql
		// └──────────────██████████         10   10 db.mysql
		//          10        20        30
		// {"metricset":{"timestamp":1556893458462000,"transaction":{"name":"test","type":"request"},"samples":{"transaction.duration.count":{"value":1},"transaction.duration.sum.us":{"value":30},"transaction.breakdown.count":{"value":1}}}}
		// {"metricset":{"timestamp":1556893458462000,"transaction":{"name":"test","type":"request"},"span":{"type":"db","subtype":"mysql"},"samples":{"span.self_time.count":{"value":2},"span.self_time.sum.us":{"value":20}}}}
		// {"metricset":{"timestamp":1556893458462000,"transaction":{"name":"test","type":"request"},"span":{"type":"app"},"samples":{"span.self_time.count":{"value":1},"span.self_time.sum.us":{"value":10}}}}
		[Fact]
		public void AcceptanceTest06()
		{
			var (agent, breakdownMetricsProvider) = SetUpAgent();
			using (agent)
			{
				var t = agent.TracerInternal.StartTransactionInternal("test", "request", 0);
				t.Duration = 0.03;

				var span = t.StartSpanInternal("db.mysql", "db", "mysql", timestamp: 5);
				var span2 = t.StartSpanInternal("db.mysql", "db", "mysql", timestamp: 15);
				span.Duration = 0.01;
				span2.Duration = 0.01;

				span.End();
				span2.End();

				t.End();
			}

			var metricSets = breakdownMetricsProvider.GetSamples();

			var metrics = metricSets as MetricSet[] ?? metricSets.ToArray();
			metrics.Should().NotBeNullOrEmpty();
			metrics.Count().Should().Be(3);
			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.duration.count") && s.KeyValue.Value == 1)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.duration.sum.us") && s.KeyValue.Value == 30)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.breakdown.count") && s.KeyValue.Value == 1)
				);

			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Span.Type.Equals("app")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.count") && s.KeyValue.Value == 1)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.sum.us") && DoubleCompare(s.KeyValue.Value, 10))
				);

			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Span.Type.Equals("db")
						&& n.Span.SubType.Equals("mysql")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.count") && s.KeyValue.Value == 2)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.sum.us") && DoubleCompare(s.KeyValue.Value, 20))
				);
		}

		//                                  total self type
		// ██████████░░░░░█████░░░░░█████    30   20 transaction
		// ├─────────█████                    5    5 db.mysql
		// └───────────────────█████          5    5 db.mysql
		//          10        20        30
		// {"metricset":{"timestamp":1556893458453000,"transaction":{"name":"test","type":"request"},"samples":{"transaction.duration.count":{"value":1},"transaction.duration.sum.us":{"value":30},"transaction.breakdown.count":{"value":1}}}}
		// {"metricset":{"timestamp":1556893458453000,"transaction":{"name":"test","type":"request"},"span":{"type":"db","subtype":"mysql"},"samples":{"span.self_time.count":{"value":2},"span.self_time.sum.us":{"value":10}}}}
		// {"metricset":{"timestamp":1556893458453000,"transaction":{"name":"test","type":"request"},"span":{"type":"app"},"samples":{"span.self_time.count":{"value":1},"span.self_time.sum.us":{"value":20}}}}
		[Fact]
		public void AcceptanceTest07()
		{
			var (agent, breakdownMetricsProvider) = SetUpAgent();
			using (agent)
			{
				var t = agent.TracerInternal.StartTransactionInternal("test", "request", 0);
				t.Duration = 0.03;

				var span = t.StartSpanInternal("db.mysql", "db", "mysql", timestamp: 10);
				span.Duration = 0.005;
				span.End();

				var span2 = t.StartSpanInternal("db.mysql", "db", "mysql", timestamp: 20);
				span2.Duration = 0.005;
				span2.End();

				t.End();
			}

			var metricSets = breakdownMetricsProvider.GetSamples();

			var metrics = metricSets as MetricSet[] ?? metricSets.ToArray();
			metrics.Should().NotBeNullOrEmpty();
			metrics.Count().Should().Be(3);
			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.duration.count") && s.KeyValue.Value == 1)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.duration.sum.us") && s.KeyValue.Value == 30)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.breakdown.count") && s.KeyValue.Value == 1)
				);

			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Span.Type.Equals("app")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.count") && s.KeyValue.Value == 1)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.sum.us") && DoubleCompare(s.KeyValue.Value, 20))
				);

			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Span.Type.Equals("db")
						&& n.Span.SubType.Equals("mysql")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.count") && s.KeyValue.Value == 2)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.sum.us") && DoubleCompare(s.KeyValue.Value, 10))
				);
		}

		//                                   total self type
		// ██████████░░░░░░░░░░██████████    30   20 transaction
		// └─────────█████░░░░░              10    5 app
		//           └────██████████         10   10 db.mysql
		//         10        20        30
		//
		// {"metricset":{"timestamp":1556893458398000,"transaction":{"name":"test","type":"request"},"samples":{"transaction.duration.count":{"value":1},"transaction.duration.sum.us":{"value":30},"transaction.breakdown.count":{"value":1}}}}
		// {"metricset":{"timestamp":1556893458398000,"transaction":{"name":"test","type":"request"},"span":{"type":"db","subtype":"mysql"},"samples":{"span.self_time.count":{"value":1},"span.self_time.sum.us":{"value":10}}}}
		// {"metricset":{"timestamp":1556893458398000,"transaction":{"name":"test","type":"request"},"span":{"type":"app"},"samples":{"span.self_time.count":{"value":2},"span.self_time.sum.us":{"value":25}}}}
		[Fact]
		public void AcceptanceTest08()
		{
			var (agent, breakdownMetricsProvider) = SetUpAgent();
			using (agent)
			{
				var t = agent.TracerInternal.StartTransactionInternal("test", "request", 0);
				t.Duration = 0.03;

				var span = t.StartSpanInternal("foo", "app", timestamp: 10);
				span.Duration = 0.01;

				var span2 = span.StartSpanInternal("db.mysql", "db", "mysql", timestamp: 15);
				span2.Duration = 0.01;

				span.End();
				span2.End();

				t.End();
			}

			var metricSets = breakdownMetricsProvider.GetSamples();

			var metrics = metricSets as MetricSet[] ?? metricSets.ToArray();
			metrics.Should().NotBeNullOrEmpty();
			metrics.Count().Should().Be(3);
			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.duration.count") && s.KeyValue.Value == 1)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.duration.sum.us") && DoubleCompare(s.KeyValue.Value, 30))
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.breakdown.count") && s.KeyValue.Value == 1)
				);

			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Span.Type.Equals("app")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.count") && s.KeyValue.Value == 2)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.sum.us") && DoubleCompare(s.KeyValue.Value, 25))
				);

			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Span.Type.Equals("db")
						&& n.Span.SubType.Equals("mysql")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.count") && s.KeyValue.Value == 1)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.sum.us") && DoubleCompare(s.KeyValue.Value, 10))
				);
		}

		//                                  total self type
		// ██████████░░░░░░░░░░              20   10 transaction
		// └─────────██████████░░░░░░░░░░    20   10 app
		//           └─────────██████████    10   10 db.mysql
		//          10        20        30
		//
		// {"metricset":{"timestamp":1556893458444000,"transaction":{"name":"test","type":"request"},"samples":{"transaction.duration.count":{"value":1},"transaction.duration.sum.us":{"value":20},"transaction.breakdown.count":{"value":1}}}}
		// {"metricset":{"timestamp":1556893458444000,"transaction":{"name":"test","type":"request"},"span":{"type":"app"},"samples":{"span.self_time.count":{"value":1},"span.self_time.sum.us":{"value":10}}}}
		[Fact]
		public void AcceptanceTest09()
		{
			var (agent, breakdownMetricsProvider) = SetUpAgent();
			using (agent)
			{
				var t = agent.TracerInternal.StartTransactionInternal("test", "request", 0);
				t.Duration = 0.02;

				var span = t.StartSpanInternal("foo", "app", timestamp: 10);
				span.Duration = 0.02;

				t.End();
				var span2 = span.StartSpanInternal("db.mysql", "db", "mysql", timestamp: 20);
				span2.Duration = 0.01;

				span.End();
				span2.End();
			}

			var metricSets = breakdownMetricsProvider.GetSamples();

			var metrics = metricSets as MetricSet[] ?? metricSets.ToArray();
			metrics.Should().NotBeNullOrEmpty();
			metrics.Count().Should().Be(2);
			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.duration.count") && s.KeyValue.Value == 1)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.duration.sum.us") && DoubleCompare(s.KeyValue.Value, 20))
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.breakdown.count") && s.KeyValue.Value == 1)
				);

			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Span.Type.Equals("app")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.count") && s.KeyValue.Value == 1)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.sum.us") && DoubleCompare(s.KeyValue.Value, 10))
				);
		}

		//                                 total self type
		// ██████████░░░░░░░░░░              20   10 transaction
		// └─────────████████████████████    20   20 db.mysql
		//          10        20        30
		// {"metricset":{"timestamp":1556893458409000,"transaction":{"name":"test","type":"request"},"samples":{"transaction.duration.count":{"value":1},"transaction.duration.sum.us":{"value":20},"transaction.breakdown.count":{"value":1}}}}
		// {"metricset":{"timestamp":1556893458409000,"transaction":{"name":"test","type":"request"},"span":{"type":"app"},"samples":{"span.self_time.count":{"value":1},"span.self_time.sum.us":{"value":10}}}}
		[Fact]
		public void AcceptanceTest10()
		{
			var (agent, breakdownMetricsProvider) = SetUpAgent();
			using (agent)
			{
				var t = agent.TracerInternal.StartTransactionInternal("test", "request", 0);
				t.Duration = 0.02;

				var span = t.StartSpanInternal("db.mysql", "db", "mysql", timestamp: 10);
				span.Duration = 0.03;
				t.End();
				span.End();
			}

			var metricSets = breakdownMetricsProvider.GetSamples();

			var metrics = metricSets as MetricSet[] ?? metricSets.ToArray();
			metrics.Should().NotBeNullOrEmpty();
			metrics.Count().Should().Be(2);
			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.duration.count") && s.KeyValue.Value == 1)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.duration.sum.us") && DoubleCompare(s.KeyValue.Value, 20))
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.breakdown.count") && s.KeyValue.Value == 1)
				);

			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Span.Type.Equals("app")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.count") && s.KeyValue.Value == 1)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.sum.us") && DoubleCompare(s.KeyValue.Value, 10))
				);
		}

		//                                 total self type
		// ██████████                        10   10 transaction
		// └───────────────────██████████    10   10 db.mysql
		//         10        20        30
		//
		// {"metricset":{"timestamp":1556893458434000,"transaction":{"name":"test","type":"request"},"samples":{"transaction.duration.count":{"value":1},"transaction.duration.sum.us":{"value":10},"transaction.breakdown.count":{"value":1}}}}
		// {"metricset":{"timestamp":1556893458434000,"transaction":{"name":"test","type":"request"},"span":{"type":"app"},"samples":{"span.self_time.count":{"value":1},"span.self_time.sum.us":{"value":10}}}}
		[Fact]
		public void AcceptanceTest11()
		{
			var (agent, breakdownMetricsProvider) = SetUpAgent();
			using (agent)
			{
				var t = agent.TracerInternal.StartTransactionInternal("test", "request", 0);
				t.Duration = 0.01;
				t.End();

				var span = t.StartSpanInternal("db.mysql", "db", "mysql", timestamp: 20);
				span.Duration = 0.01;

				span.End();
			}

			var metricSets = breakdownMetricsProvider.GetSamples();

			var metrics = metricSets as MetricSet[] ?? metricSets.ToArray();
			metrics.Should().NotBeNullOrEmpty();
			metrics.Count().Should().Be(2);
			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.duration.count") && s.KeyValue.Value == 1)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.duration.sum.us") && DoubleCompare(s.KeyValue.Value, 10))
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("transaction.breakdown.count") && s.KeyValue.Value == 1)
				);

			metrics.Should()
				.Contain(
					n => n.Transaction.Name.Equals("test")
						&& n.Transaction.Type.Equals("request")
						&& n.Span.Type.Equals("app")
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.count") && s.KeyValue.Value == 1)
						&& n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.sum.us") && DoubleCompare(s.KeyValue.Value, 10))
				);
		}

		[Fact]
		public void DisableSpanSelfTimeTest()
		{
			var payloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(
				new AgentComponents(
					new NoopLogger(),
					new MockConfigSnapshot(metricsInterval: "1s", disableMetrics: "span.self_time"),
					payloadSender,
					null, //metricsCollector will be set in AgentComponents.ctor
					new CurrentExecutionSegmentsContainer(),
					new NoopCentralConfigFetcher(),
					new MockApmServerInfo(new ElasticVersion(7, 12, 0, string.Empty))));

			agent.Tracer.CaptureTransaction("Foo", "Bar", _ =>
			{
				Thread.Sleep(100);
			});

			payloadSender.WaitForTransactions();
			payloadSender.WaitForMetrics();
			payloadSender.Metrics
				.Where(n => n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.count")))
				.Should()
				.BeNullOrEmpty();
			payloadSender.Metrics
				.Where(n => n.Samples.Any(s => s.KeyValue.Key.Equals("span.self_time.sum.us")))
				.Should()
				.BeNullOrEmpty();
		}

		/// <summary>
		/// Makes sure that the number of metricsets for breakdown is limited to 1k.
		/// </summary>
		[Fact]
		public void MoreThan1KBreakDownMetrics()
		{
			var (agent, breakdownMetricsProvider) = SetUpAgent();
			using (agent)
			{
				var t = agent.TracerInternal.StartTransactionInternal("test", "request");

				var rnd = new Random();
				for (var i = 0; i < 5000; i++) t.CaptureSpan("foo", $"bar-{rnd.Next().ToString()}", () => { });
				t.End();
			}

			breakdownMetricsProvider.GetSamples().Count().Should().Be(1000);
		}

		private bool DoubleCompare(double value, double expectedValue) => Math.Abs(value - expectedValue) < 10;

		private (ApmAgent, BreakdownMetricsProvider) SetUpAgent()
		{
			var breakdownMetricsProvider = new BreakdownMetricsProvider(new NoopLogger());

			var agentComponents = new AgentComponents(
				new NoopLogger(),
				new MockConfigSnapshot(metricsInterval: "1s"),
				new NoopPayloadSender(),
				new FakeMetricsCollector(), //metricsCollector will be set in AgentComponents.ctor
				new CurrentExecutionSegmentsContainer(),
				new NoopCentralConfigFetcher(),
				new MockApmServerInfo(new ElasticVersion(7, 12, 0, string.Empty)),
				breakdownMetricsProvider);

			var agent = new ApmAgent(agentComponents);

			return (agent, breakdownMetricsProvider);
		}
	}
}
