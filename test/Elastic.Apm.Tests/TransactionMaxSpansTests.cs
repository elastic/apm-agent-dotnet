using System.Linq;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class TransactionMaxSpansTests
	{
		[Fact]
		public void SpansShouldNotBeCollected_WhenTransactionMaxSpansIsEqualToZero()
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
		}

		[Fact]
		public void SpansCollectionShouldBeLimited_WhenTransactionMaxSpansIsPositiveNumber()
		{
			// Arrange
			var mockPayloadSender = new MockPayloadSender();
			var mockConfig = new MockConfigSnapshot(transactionMaxSpans: "5");

			// Act
			using (var agent = new ApmAgent(new TestAgentComponents(config: mockConfig, payloadSender: mockPayloadSender)))
			{
				agent.Tracer.CaptureTransaction("test transaction name", "test transaction type",
					transaction =>
					{
						foreach (var iteration in Enumerable.Range(1, 10))
						{
							transaction.CaptureSpan($"test span name #{iteration}", "test span type", span => { });
						}
					});
			}

			// Assert
			mockPayloadSender.Transactions.Count.Should().Be(1);
			mockPayloadSender.Spans.Count.Should().Be(5);
		}

		[Fact]
		public void AllSpansShouldBeCollected_WhenTransactionMaxSpansIsEqualToMinusOne()
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
						{
							transaction.CaptureSpan($"test span name #{iteration}", "test span type", span => { });
						}
					});
			}

			// Assert
			mockPayloadSender.Transactions.Count.Should().Be(1);
			mockPayloadSender.Spans.Count.Should().Be(spansCount);
		}
	}
}
