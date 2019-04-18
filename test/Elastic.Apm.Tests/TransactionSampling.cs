using Elastic.Apm.Api;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class TransactionSampling
	{
		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void SpansSentOnlyForSampledTransaction(bool isSampled)
		{
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				agent.TracerInternal.Sampler = new Sampler(isSampled ? 1.0 : 0.0);
				agent.Tracer.CaptureTransaction("test transaction name", "test transaction type",
					transaction =>
						transaction.CaptureSpan("test span name", "test span type", span => { })
				);
			}

			payloadSender.Transactions.Count.Should().Be(1);
			payloadSender.FirstTransaction.SpanCount.Dropped.Should().Be(0);
			if (isSampled)
			{
				payloadSender.FirstTransaction.SpanCount.Started.Should().Be(1);
				payloadSender.Spans.Count.Should().Be(1);
			}
			else
			{
				payloadSender.FirstTransaction.SpanCount.Started.Should().Be(0);
				payloadSender.Spans.Should().BeEmpty();
			}
		}
	}
}
