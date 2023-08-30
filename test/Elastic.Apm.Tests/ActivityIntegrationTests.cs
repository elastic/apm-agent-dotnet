// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.XUnit;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Tests
{
	[Collection("ActivityIdFormat")]
	[CaptureRestoreActivityIdFormat]
	public class ActivityIntegrationTests
	{
		private readonly ITestOutputHelper _testOutputHelper;

		public ActivityIntegrationTests(ITestOutputHelper testOutputHelper)
		{
			_testOutputHelper = testOutputHelper;
		}

		/// <summary>
		/// Makes sure that in case there is an active activity, the agent reuses its TraceId when it starts a new transaction.
		/// The prerequisite is that the IdFormat is W3C
		/// </summary>
		[Fact]
		public void ElasticTransactionReusesTraceIdFromCurrentActivity()
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

		/// <summary>
		/// Makes sure that transactions on the same Activity are part of the same trace.
		/// </summary>
		[Fact]
		public async Task ActivityRespectsSampling()
		{
			const int count = 100;
			const double rate = 0.5;

			Activity.Current = null;
			Activity.DefaultIdFormat = ActivityIdFormat.W3C;
			var source = new ActivitySource(GetType().FullName, "1.0.0");

			var payloadSender = new MockPayloadSender();
			var config = new MockConfiguration(
				transactionSampleRate: rate.ToString("N2")

			);
			using var components = new TestAgentComponents(
				apmServerInfo: MockApmServerInfo.Version716,
				configuration: config,
				payloadSender: payloadSender

			);
			using var agent = new ApmAgent(components);
			for (var i = 0; i < count; i++)
			{
				using var transaction = source.StartActivity($"Trace {i}");
				using var span = new Activity("UnitTestActivity").Start();
				await Task.Delay(1);
			}
			//Activity.Current.Should().BeNull();
			payloadSender.WaitForTransactions(count: count);

			var sampled = payloadSender.Transactions.Where(t => t.IsSampled).ToArray();
			sampled.Length.Should().BeLessThan(count);
			sampled.Length.Should().BeGreaterThan(count / 10);

			var sampledSpans = payloadSender.Spans.Where(t => t.IsSampled).ToArray();
			sampledSpans.Length.Should().Be(sampled.Length);


		}
	}
}
