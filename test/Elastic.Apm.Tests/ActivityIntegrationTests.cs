using System.Diagnostics;
using System.Threading;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class ActivityIntegrationTests
	{
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

			activity.Stop();
		}

		/// <summary>
		/// Makes sure that even if an activity is active, the agent will ignore it and not try to reuse the traceId if the IdFormat of the
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

			activity.Stop();
		}
	}
}
