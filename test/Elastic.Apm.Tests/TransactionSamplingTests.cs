using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class TransactionSamplingTests
	{
		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void SpansSentOnlyForSampledTransaction(bool isSampled)
		{
			var mockPayloadSender = new MockPayloadSender();
			var mockConfig = new MockConfigSnapshot(transactionSampleRate: isSampled ? "1" : "0");
			using (var agent = new ApmAgent(new TestAgentComponents(config: mockConfig, payloadSender: mockPayloadSender)))
			{
				agent.Tracer.CaptureTransaction("test transaction name", "test transaction type",
					transaction =>
						transaction.CaptureSpan("test span name", "test span type", span => { })
				);
			}

			mockPayloadSender.Transactions.Count.Should().Be(1);
			mockPayloadSender.FirstTransaction.SpanCount.Dropped.Should().Be(0);
			if (isSampled)
			{
				mockPayloadSender.FirstTransaction.SpanCount.Started.Should().Be(1);
				mockPayloadSender.Spans.Count.Should().Be(1);
			}
			else
			{
				mockPayloadSender.FirstTransaction.SpanCount.Started.Should().Be(0);
				mockPayloadSender.Spans.Should().BeEmpty();
			}
		}
	}
}
