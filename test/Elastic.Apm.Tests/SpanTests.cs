// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class SpanTests
	{
		[Fact]
		public void CaptureError_ShouldUseTransactionIdAsParent_WhenSpanDropped()
		{
			// Arrange
			var payloadSender = new MockPayloadSender();
			using (var agent =
				new ApmAgent(new TestAgentComponents(configuration: new MockConfiguration(transactionMaxSpans: "0"), payloadSender: payloadSender)))
			{
				agent.Tracer.CaptureTransaction("transaction", "type", transaction =>
				{
					transaction.CaptureSpan("parent", "type", span =>
					{
						// Act
						span.CaptureError("Error message", "culprit", Array.Empty<StackFrame>());
					});
				});
			}

			// Assert
			payloadSender.FirstError.ParentId.Should().Be(payloadSender.FirstTransaction.Id);
		}

		[Fact]
		public void CaptureException_ShouldUseTransactionIdAsParent_WhenSpanDropped()
		{
			// Arrange
			var payloadSender = new MockPayloadSender();
			using (var agent =
				new ApmAgent(new TestAgentComponents(configuration: new MockConfiguration(transactionMaxSpans: "0"), payloadSender: payloadSender)))
			{
				agent.Tracer.CaptureTransaction("transaction", "type", transaction =>
				{
					transaction.CaptureSpan("parent", "type", span =>
					{
						// Act
						span.CaptureException(new Exception(), "culprit");
					});
				});
			}

			// Assert
			payloadSender.FirstError.ParentId.Should().Be(payloadSender.FirstTransaction.Id);
		}

		[Fact]
		public void CaptureError_ShouldUseTransactionIdAsParent_WhenSpanNonSampled()
		{
			// Arrange
			var payloadSender = new MockPayloadSender();
			using (var agent =
				new ApmAgent(new TestAgentComponents(configuration: new MockConfiguration(transactionSampleRate: "0"), payloadSender: payloadSender)))
			{
				agent.Tracer.CaptureTransaction("transaction", "type", transaction =>
				{
					transaction.CaptureSpan("parent", "type", span =>
					{
						// Act
						span.CaptureError("Error message", "culprit", Array.Empty<StackFrame>());
					});
				});
			}

			// Assert
			payloadSender.FirstError.ParentId.Should().Be(payloadSender.FirstTransaction.Id);
		}

		[Fact]
		public void CaptureException_ShouldUseTransactionIdAsParent_WhenSpanNonSampled()
		{
			// Arrange
			var payloadSender = new MockPayloadSender();
			using (var agent =
				new ApmAgent(new TestAgentComponents(configuration: new MockConfiguration(transactionSampleRate: "0"), payloadSender: payloadSender)))
			{
				agent.Tracer.CaptureTransaction("transaction", "type", transaction =>
				{
					transaction.CaptureSpan("parent", "type", span =>
					{
						// Act
						span.CaptureException(new Exception(), "culprit");
					});
				});
			}

			// Assert
			payloadSender.FirstError.ParentId.Should().Be(payloadSender.FirstTransaction.Id);
		}

		[Fact]
		public void End_ShouldRestoreParentSpan_WhenTransactionIsNotSampled()
		{
			// Arrange
			var payloadSender = new MockPayloadSender();
			using (var agent =
				new ApmAgent(new TestAgentComponents(configuration: new MockConfiguration(transactionSampleRate: "0"), payloadSender: payloadSender)))
			{
				var transaction = agent.Tracer.StartTransaction("transaction", "type");

				var parentSpan = transaction.StartSpan("parent", "type");

				// Act
				parentSpan.CaptureSpan("span", "type", span => { });

				// Assert
				payloadSender.Spans.Count.Should().Be(0);
				agent.Tracer.CurrentSpan.Should().Be(parentSpan);
			}
		}
	}
}
