// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
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
			       new ApmAgent(new TestAgentComponents(configuration: new MockConfiguration(transactionMaxSpans: "0"),
				       payloadSender: payloadSender)))
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
			       new ApmAgent(new TestAgentComponents(configuration: new MockConfiguration(transactionMaxSpans: "0"),
				       payloadSender: payloadSender)))
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
			       new ApmAgent(new TestAgentComponents(
				       configuration: new MockConfiguration(transactionSampleRate: "0"), payloadSender: payloadSender)))
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
			       new ApmAgent(new TestAgentComponents(
				       configuration: new MockConfiguration(transactionSampleRate: "0"), payloadSender: payloadSender)))
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
			       new ApmAgent(new TestAgentComponents(
				       configuration: new MockConfiguration(transactionSampleRate: "0"), payloadSender: payloadSender)))
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

		[Fact]
		public void IsCaptureStackTraceOnEndEnabled_test_with_new_and_legacy_settings()
		{
			// Enabled by default if duration is met
			Create_Span_ForCaptureStackTraceTest(new MockConfiguration()).Let(span =>
			{
				span.Duration = ConfigConsts.DefaultValues.SpanStackTraceMinDurationInMilliseconds;
				span.IsCaptureStackTraceOnEndEnabled().Should().BeTrue();
			});

			// Disabled if duration is too low.
			Create_Span_ForCaptureStackTraceTest(new MockConfiguration()).Let(span =>
			{
				span.Duration = ConfigConsts.DefaultValues.SpanStackTraceMinDurationInMilliseconds - 1;
				span.IsCaptureStackTraceOnEndEnabled().Should().BeFalse();
			});

			// Disabled via "StackTraceLimit".
			Create_Span_ForCaptureStackTraceTest(new MockConfiguration(stackTraceLimit: "0")).Let(span =>
			{
				span.Duration = ConfigConsts.DefaultValues.SpanStackTraceMinDurationInMilliseconds;
				span.IsCaptureStackTraceOnEndEnabled().Should().BeFalse();
			});

			// Disabled if a StackTrace is already present.
			Create_Span_ForCaptureStackTraceTest(new MockConfiguration()).Let(span =>
			{
				span.Duration = ConfigConsts.DefaultValues.SpanStackTraceMinDurationInMilliseconds;
				span.RawStackTrace = new StackTrace();
				span.IsCaptureStackTraceOnEndEnabled().Should().BeFalse();
			});

			// Enabled if duration is greater or equal
			Create_Span_ForCaptureStackTraceTest(new MockConfiguration()).Let(span =>
			{
				span.Duration = ConfigConsts.DefaultValues.SpanStackTraceMinDurationInMilliseconds;
				span.IsCaptureStackTraceOnEndEnabled().Should().BeTrue();
			});

			// Disabled via "SpanStackTraceMinDurationInMilliseconds"
			Create_Span_ForCaptureStackTraceTest(new MockConfiguration(spanStackTraceMinDurationInMilliseconds: "-1")).Let(span =>
			{
				span.Duration = ConfigConsts.DefaultValues.SpanStackTraceMinDurationInMilliseconds;
				span.IsCaptureStackTraceOnEndEnabled().Should().BeFalse();
			});

			// Disabled via legacy setting "SpanFramesMinDurationInMilliseconds"
			Create_Span_ForCaptureStackTraceTest(new MockConfiguration(spanFramesMinDurationInMilliseconds: "0")).Let(span =>
			{
				span.Duration = ConfigConsts.DefaultValues.SpanStackTraceMinDurationInMilliseconds;
				span.IsCaptureStackTraceOnEndEnabled().Should().BeFalse();
			});

			// Disabled: "SpanStackTraceMinDurationInMilliseconds"	 dominates legacy setting "SpanFramesMinDurationInMilliseconds"
			Create_Span_ForCaptureStackTraceTest(new MockConfiguration(spanStackTraceMinDurationInMilliseconds: "-1", spanFramesMinDurationInMilliseconds: "1000ms")).Let(span =>
			{
				span.Duration = ConfigConsts.DefaultValues.SpanStackTraceMinDurationInMilliseconds;
				span.IsCaptureStackTraceOnEndEnabled().Should().BeFalse();
			});

			// Disabled: legacy setting "SpanFramesMinDurationInMilliseconds" still dominates over default value for "SpanStackTraceMinDurationInMilliseconds"
			Create_Span_ForCaptureStackTraceTest(new MockConfiguration(spanStackTraceMinDurationInMilliseconds: ConfigConsts.DefaultValues.SpanStackTraceMinDuration, spanFramesMinDurationInMilliseconds: "1s")).Let(span =>
			{
				span.Duration = ConfigConsts.DefaultValues.SpanStackTraceMinDurationInMilliseconds;
				span.IsCaptureStackTraceOnEndEnabled().Should().BeFalse();
			});

			// Enabled if duration exceeds set value
			Create_Span_ForCaptureStackTraceTest(new MockConfiguration(spanStackTraceMinDurationInMilliseconds: "200ms")).Let(span =>
			{
				span.Duration = 300;
				span.IsCaptureStackTraceOnEndEnabled().Should().BeTrue();
			});

			// Enabled if duration exceeds set legacy value
			Create_Span_ForCaptureStackTraceTest(new MockConfiguration(spanFramesMinDurationInMilliseconds: "200ms")).Let(span =>
			{
				span.Duration = 300;
				span.IsCaptureStackTraceOnEndEnabled().Should().BeTrue();
			});
		}

		[Fact]
		public void IsCaptureStackTraceOnStartEnabled_test_with_new_and_legacy_settings()
		{
			// Enabled by default
			Create_Span_ForCaptureStackTraceTest(new MockConfiguration()).IsCaptureStackTraceOnStartEnabled().Should()
				.BeTrue();

			// Disabled via "StackTraceLimit"
			Create_Span_ForCaptureStackTraceTest(new MockConfiguration(stackTraceLimit: "0"))
				.IsCaptureStackTraceOnStartEnabled().Should().BeFalse();

			// Disabled via "SpanStackTraceMinDurationInMilliseconds"
			Create_Span_ForCaptureStackTraceTest(new MockConfiguration(spanStackTraceMinDurationInMilliseconds: "-1"))
				.IsCaptureStackTraceOnStartEnabled().Should().BeFalse();

			// Disabled via legacy setting "SpanFramesMinDurationInMilliseconds"
			Create_Span_ForCaptureStackTraceTest(new MockConfiguration(spanFramesMinDurationInMilliseconds: "0"))
				.IsCaptureStackTraceOnStartEnabled().Should().BeFalse();

			// Disabled: "SpanStackTraceMinDurationInMilliseconds" dominates legacy setting "SpanFramesMinDurationInMilliseconds"
			Create_Span_ForCaptureStackTraceTest(new MockConfiguration(spanStackTraceMinDurationInMilliseconds: "-1",
				spanFramesMinDurationInMilliseconds: "23ms")).IsCaptureStackTraceOnStartEnabled().Should().BeFalse();

			// Disabled: legacy setting "SpanFramesMinDurationInMilliseconds" still dominates over default value for "SpanStackTraceMinDurationInMilliseconds"
			Create_Span_ForCaptureStackTraceTest(new MockConfiguration(
				spanStackTraceMinDurationInMilliseconds: ConfigConsts.DefaultValues.SpanStackTraceMinDuration,
				spanFramesMinDurationInMilliseconds: "23ms")).IsCaptureStackTraceOnStartEnabled().Should().BeTrue();
		}

		private static Model.Span Create_Span_ForCaptureStackTraceTest(IConfiguration configuration)
		{
			var agent = new ApmAgent(new TestAgentComponents(configuration: configuration,
				payloadSender: new MockPayloadSender()));
			var transaction = agent.Tracer.StartTransaction("transaction", "type");
			return transaction.StartSpan("span", "type") as Model.Span;
		}
	}
}
