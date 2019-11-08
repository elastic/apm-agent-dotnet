using System.Linq;
using Elastic.Apm.Tests.Mocks;
using Elastic.Apm.Tests.TestHelpers;
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

		[Fact]
		public void SpansShouldNotBeSent_WhenTransactionMaxSpansIsEqualToZero()
		{
			// Arrange
			var mockPayloadSender = new MockPayloadSender();
			var mockConfig = new MockConfigSnapshot(transactionMaxSpans: "0");

			// Act
			using (var agent = new ApmAgent(new TestAgentComponents(config: mockConfig, payloadSender: mockPayloadSender)))
			{
				agent.Tracer.CaptureTransaction("test transaction name", "test transaction type",
					transaction =>
						transaction.CaptureSpan("test span name", "test span type", span => { })
				);
			}

			// Assert
			mockPayloadSender.Transactions.Count.Should().Be(1);
			mockPayloadSender.Spans.Count.Should().Be(0);
			mockPayloadSender.FirstTransaction.SpanCount.Dropped.Should().Be(1);
		}

		[Fact]
		public void LimitedAmountOfSpansShouldBeSent_WhenTransactionMaxSpansIsPositiveNumber()
		{
			// Arrange
			const int spansCount = 10;
			const int maxSpansCount = 5;
			var mockPayloadSender = new MockPayloadSender();
			var mockConfig = new MockConfigSnapshot(transactionMaxSpans: maxSpansCount.ToString());

			// Act
			using (var agent = new ApmAgent(new TestAgentComponents(config: mockConfig, payloadSender: mockPayloadSender)))
			{
				agent.Tracer.CaptureTransaction("test transaction name", "test transaction type",
					transaction =>
					{
						foreach (var iteration in Enumerable.Range(1, spansCount))
							transaction.CaptureSpan($"test span name #{iteration}", "test span type", span => { });
					});
			}

			// Assert
			mockPayloadSender.Transactions.Count.Should().Be(1);
			mockPayloadSender.Spans.Count.Should().Be(maxSpansCount);
			mockPayloadSender.FirstTransaction.SpanCount.Dropped.Should().Be(spansCount - maxSpansCount);
		}

		[Fact]
		public void LimitedAmountOfSpansShouldBeSent_WhenSpansAreCapturedConcurrently()
		{
			// Arrange
			const int spansCount = 10;
			const int maxSpansCount = 2;
			var mockPayloadSender = new MockPayloadSender();
			var mockConfig = new MockConfigSnapshot(transactionMaxSpans: maxSpansCount.ToString());

			// Act
			using (var agent = new ApmAgent(new TestAgentComponents(config: mockConfig, payloadSender: mockPayloadSender)))
			{
				agent.Tracer.CaptureTransaction("test transaction name", "test transaction type",
					transaction =>
					{
						MultiThreadsTestUtils.TestOnThreads(spansCount, threadIndex =>
						{
							transaction.CaptureSpan($"test span name #{threadIndex}", "test span type", span => { });
							return 1;
						});
					});
			}

			// Assert
			mockPayloadSender.Transactions.Count.Should().Be(1);
			mockPayloadSender.Spans.Count.Should().Be(maxSpansCount);
			mockPayloadSender.FirstTransaction.SpanCount.Dropped.Should().Be(spansCount - maxSpansCount);
		}

		[Fact]
		public void AllSpansShouldBeSent_WhenTransactionMaxSpansIsEqualToMinusOne()
		{
			// Arrange
			var mockPayloadSender = new MockPayloadSender();
			var mockConfig = new MockConfigSnapshot(transactionMaxSpans: "-1");
			var spansCount = 1000;

			// Act
			using (var agent = new ApmAgent(new TestAgentComponents(config: mockConfig, payloadSender: mockPayloadSender)))
			{
				agent.Tracer.CaptureTransaction("test transaction name", "test transaction type",
					transaction =>
					{
						foreach (var iteration in Enumerable.Range(1, spansCount))
							transaction.CaptureSpan($"test span name #{iteration}", "test span type", span => { });
					});
			}

			// Assert
			mockPayloadSender.Transactions.Count.Should().Be(1);
			mockPayloadSender.Spans.Count.Should().Be(spansCount);
			mockPayloadSender.FirstTransaction.SpanCount.Dropped.Should().Be(0);
		}
	}
}
