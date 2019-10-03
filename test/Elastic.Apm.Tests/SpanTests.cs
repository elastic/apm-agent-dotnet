using System;
using System.Diagnostics;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Mocks;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class SpanTests
	{
		[Fact]
		public void CaptureError_ShouldUseTransactionIdAsParent_WhenSpanDropped()
		{
			// Arrange
			var currentExecutionSegmentsContainer = new NoopCurrentExecutionSegmentsContainer();
			var logger = new NoopLogger();
			var payloadSender = new MockPayloadSender();

			var transaction = new Transaction(logger, "transaction", "type", new Sampler(1.0), null, payloadSender, null,
				currentExecutionSegmentsContainer);

			var span = new Span("parent", "type", transaction.Id, "traceId", transaction, payloadSender, logger,
				new MockConfigSnapshot(transactionMaxSpans: "0"), currentExecutionSegmentsContainer);

			// Act
			span.CaptureError("Error message", "culprit", new StackFrame[0]);

			// Assert
			Assert.Equal(transaction.Id, payloadSender.FirstError.ParentId);
		}

		[Fact]
		public void CaptureException_ShouldUseTransactionIdAsParent_WhenSpanDropped()
		{
			// Arrange
			var currentExecutionSegmentsContainer = new NoopCurrentExecutionSegmentsContainer();
			var logger = new NoopLogger();
			var payloadSender = new MockPayloadSender();

			var transaction = new Transaction(logger, "transaction", "type", new Sampler(1.0), null, payloadSender, null,
				currentExecutionSegmentsContainer);

			var span = new Span("parent", "type", transaction.Id, "traceId", transaction, payloadSender, logger,
				new MockConfigSnapshot(transactionMaxSpans: "0"), currentExecutionSegmentsContainer);

			// Act
			span.CaptureException(new Exception(), "culprit");

			// Assert
			Assert.Equal(transaction.Id, payloadSender.FirstError.ParentId);
		}

		[Fact]
		public void CaptureError_ShouldUseTransactionIdAsParent_WhenSpanNonSampled()
		{
			// Arrange
			var currentExecutionSegmentsContainer = new NoopCurrentExecutionSegmentsContainer();
			var logger = new NoopLogger();
			var payloadSender = new MockPayloadSender();

			var transaction = new Transaction(logger, "transaction", "type", new Sampler(0), null, payloadSender, null,
				currentExecutionSegmentsContainer);

			var span = new Span("parent", "type", transaction.Id, "traceId", transaction, payloadSender, logger,
				new MockConfigSnapshot(transactionMaxSpans: "10"), currentExecutionSegmentsContainer);

			// Act
			span.CaptureError("Error message", "culprit", new StackFrame[0]);

			// Assert
			Assert.Equal(transaction.Id, payloadSender.FirstError.ParentId);
		}

		[Fact]
		public void CaptureException_ShouldUseTransactionIdAsParent_WhenSpanNonSampled()
		{
			// Arrange
			var currentExecutionSegmentsContainer = new NoopCurrentExecutionSegmentsContainer();
			var logger = new NoopLogger();
			var payloadSender = new MockPayloadSender();

			var transaction = new Transaction(logger, "transaction", "type", new Sampler(0), null, payloadSender, null,
				currentExecutionSegmentsContainer);

			var span = new Span("parent", "type", transaction.Id, "traceId", transaction, payloadSender, logger,
				new MockConfigSnapshot(transactionMaxSpans: "10"), currentExecutionSegmentsContainer);

			// Act
			span.CaptureException(new Exception(), "culprit");

			// Assert
			Assert.Equal(transaction.Id, payloadSender.FirstError.ParentId);
		}
	}
}
