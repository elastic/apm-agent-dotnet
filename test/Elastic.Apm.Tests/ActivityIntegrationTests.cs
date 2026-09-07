// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.XUnit;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	[Collection("ActivityIdFormat")]
	[CaptureRestoreActivityIdFormat]
	public class ActivityIntegrationTests
	{
		/// <summary>
		/// Makes sure that in case there is an active activity, the agent reuses its TraceId when it starts a new transaction.
		/// The prerequisite is that the IdFormat is W3C
		/// </summary>
		[Fact]
		public void ElasticTransactionReusesTraceIdFromCurrentActivity()
		{
#if NET
			var listener = new ActivityListener
			{
				ShouldListenTo = a => a.Name == "Elastic.Apm",
				Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
				ActivityStarted = activity => { },
				ActivityStopped = activity => { }
			};

			ActivitySource.AddActivityListener(listener);
#endif

			try
			{
				Activity.DefaultIdFormat = ActivityIdFormat.W3C;

				var activity = new Activity("UnitTestActivity");
				activity.Start();

				Activity.Current.TraceId.Should().Be(activity.TraceId);

				var payloadSender = new MockPayloadSender();
				using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
					agent.Tracer.CaptureTransaction("TestTransaction", "Test", () => Thread.Sleep(10));

				payloadSender.FirstTransaction.TraceId.Should().Be(activity.TraceId.ToString());
				payloadSender.FirstTransaction.ParentId.Should().BeNullOrEmpty();

				activity.Stop();
			}
			catch
			{
#if NET
				listener.Dispose();
#endif
			}
		}

		/// <summary>
		/// Makes sure that even if an activity is active, the agent will ignore it and not try to reuse the traceId if the
		/// IdFormat of the
		/// activity is NOT w3c.
		/// </summary>
		[Fact]
		public void HierarchicalIdFormatNotUsedByApmTransaction()
		{
			Activity.DefaultIdFormat = ActivityIdFormat.Hierarchical;

			var activity = new Activity("UnitTestActivity");
			activity.Start();

			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
				agent.Tracer.CaptureTransaction("TestTransaction", "Test", () => Thread.Sleep(10));

			payloadSender.FirstTransaction.TraceId.Should().NotBe(activity.TraceId.ToString());
			payloadSender.FirstTransaction.ParentId.Should().BeNullOrEmpty();

			activity.Stop();
		}

		/// <summary>
		/// First starts an Elastic APM transaction, then starts an activity.
		/// Makes sure the activity has the same TraceId as the transaction.
		/// </summary>
		[Fact]
		public void StartActivityAfterTransaction()
		{
			Activity.Current = null;

			string activityTraceId = null;
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				agent.Tracer.CaptureTransaction("TestTransaction", "Test", () =>
				{
					var activity = new Activity("UnitTestActivity");
					activity.Start();
					Thread.Sleep(10);
					activityTraceId = activity.TraceId.ToString();
					activity.Stop();
				});
			}

			activityTraceId.Should().Be(payloadSender.FirstTransaction.TraceId);
		}

		/// <summary>
		/// Same as <see cref="StartActivityAfterTransaction" />, but sets ActivityIdFormat.Hierarchical first.
		/// </summary>
		[Fact]
		public void StartActivityWithHierarchicalIdAfterTransaction()
		{
			Activity.Current = null;
			Activity.DefaultIdFormat = ActivityIdFormat.Hierarchical;

			string activityTraceId = null;
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				agent.Tracer.CaptureTransaction("TestTransaction", "Test", () =>
				{
					var activity = new Activity("UnitTestActivity");
					activity.Start();
					Thread.Sleep(10);
					activityTraceId = activity.TraceId.ToString();
					activity.Stop();
				});
			}

			activityTraceId.Should().Be(payloadSender.FirstTransaction.TraceId);
		}

		/// <summary>
		/// Makes sure that transactions on the same Activity are part of the same trace.
		/// </summary>
		[Fact]
		public void MultipleTransactionInOneActivity()
		{
			Activity.Current = null;
			Activity.DefaultIdFormat = ActivityIdFormat.W3C;
			var activity = new Activity("UnitTestActivity");
			activity.Start();

			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				var transaction1 = agent.Tracer.StartTransaction("transaction1", "test");
				transaction1.End();

				var transaction2 = agent.Tracer.StartTransaction("transaction2", "test");
				transaction2.End();
			}

			payloadSender.WaitForTransactions();
			payloadSender.Transactions.Should().HaveCount(2);
			payloadSender.Transactions[0].ParentId.Should().BeNullOrEmpty();
			payloadSender.Transactions[0].TraceId.Should().Be(activity.TraceId.ToString());
			payloadSender.Transactions[1].TraceId.Should().Be(activity.TraceId.ToString());
			payloadSender.Transactions[0].Id.Should().NotBe(payloadSender.Transactions[1].Id);
			activity.Stop();
		}

#if NET
		/// <summary>
		/// Makes sure the sample rate is applied to transactions and spans created through the OpenTelemetry bridge.
		/// </summary>
		[Fact]
		public async Task ActivityRespectsSampling()
		{
			const int count = 100;
			const double rate = 0.5;
			const string transactionNamePrefix = nameof(ActivityRespectsSampling) + " transaction ";
			const string spanName = nameof(ActivityRespectsSampling) + " span";

			Activity.Current = null;
			Activity.DefaultIdFormat = ActivityIdFormat.W3C;
			var source = new ActivitySource(GetType().FullName, "1.0.0");

			var payloadSender = new MockPayloadSender();
			var config = new MockConfiguration(
				// The rate has to be formatted with an invariant decimal separator, the agent only parses it that way.
				transactionSampleRate: rate.ToString("N2", CultureInfo.InvariantCulture)
			);
			using var components = new TestAgentComponents(
				apmServerInfo: MockApmServerInfo.Version716,
				configuration: config,
				payloadSender: payloadSender
			);
			using var agent = new ApmAgent(components);
			agent.Configuration.TransactionSampleRate.Should().Be(rate);

			for (var i = 0; i < count; i++)
			{
				using var transaction = source.StartActivity(transactionNamePrefix + i);
				using var span = source.StartActivity(spanName, ActivityKind.Internal);
				await Task.Delay(1);
			}

			// The bridge listens to every ActivitySource in the process, so activities created by tests running in
			// parallel also end up in this payload sender. Both activities above are ended synchronously by the
			// `using` scope, so by now every transaction and span this test created has already been queued and only
			// the ones created here must be counted.
			var transactions = payloadSender.Transactions
				.Where(t => t.Name.StartsWith(transactionNamePrefix, StringComparison.Ordinal))
				.ToArray();
			transactions.Length.Should().Be(count);

			var sampled = transactions.Where(t => t.IsSampled).ToArray();
			sampled.Length.Should().BeLessThan(count);
			sampled.Length.Should().BeGreaterThan(count / 10);

			// Spans are only reported for sampled transactions, so every span this test produced has to belong to one
			// of its own sampled transactions. The exact span count is deliberately not asserted: whether an inner
			// activity is captured as a span depends on the transaction the bridge has current at that moment, and the
			// bridge is a process-wide listener that tests running in parallel also drive.
			var sampledIds = sampled.Select(t => t.Id).ToArray();
			var spanTransactionIds = payloadSender.Spans
				.Where(s => s.Name == spanName)
				.Select(s => s.TransactionId)
				.ToArray();

			spanTransactionIds.Should().NotBeEmpty();
			spanTransactionIds.Should().OnlyContain(id => sampledIds.Contains(id));
			spanTransactionIds.Distinct().Should().HaveCountLessThanOrEqualTo(sampled.Length);
		}
#endif
	}
}
