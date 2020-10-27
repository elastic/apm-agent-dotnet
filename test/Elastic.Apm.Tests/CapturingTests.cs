// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Model;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Microsoft.Diagnostics.Tracing.Parsers.AspNet;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class CapturingTests
	{
		/// <summary>
		/// Starts the agent with enabled=false and uses the API to capture 1 transaction.
		/// Makes sure that the Tracer returns a NoopTransaction and no Transaction is captured by the PayloadSender
		/// </summary>
		[Fact]
		public void AgentDisabledBasicTransaction()
		{
			var payloadSender = new MockPayloadSender();
			var configReader = new MockConfigSnapshot(enabled: "false");
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, config: configReader));
			agent.Tracer.CurrentTransaction.Should().BeNull();
			var transaction = agent.Tracer.StartTransaction("TestTransaction", "Test");
			transaction.Should().NotBeOfType<Transaction>();
			transaction.Should().BeOfType<NoopTransaction>();

			agent.Tracer.CurrentTransaction.Should().Be(transaction);

			transaction.End();

			agent.Tracer.CurrentTransaction.Should().BeNull();
			payloadSender.Transactions.Should().BeNullOrEmpty();
		}

		/// <summary>
		/// Starts the agent with enabled=false and uses the API to capture 1 transaction with the convenient lambda based API.
		/// Makes sure that the Tracer returns a NoopTransaction and no Transaction is captured by the PayloadSender
		/// </summary>
		[Fact]
		public void AgentDisabledTransactionWithLambda()
		{
			var payloadSender = new MockPayloadSender();
			var configReader = new MockConfigSnapshot(enabled: "false");
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, config: configReader));
			var codeExecuted = false;
			agent.Tracer.CaptureTransaction("TestTransaction", "Test", transaction =>
			{
				codeExecuted = true;
				transaction.Should().NotBeOfType<Transaction>();
				transaction.Should().BeOfType<NoopTransaction>();

				// ReSharper disable AccessToDisposedClosure
				agent.Tracer.CurrentTransaction.Should().NotBeNull();
				agent.Tracer.CurrentTransaction.Should().Be(transaction);
			});

			codeExecuted.Should().BeTrue();
			payloadSender.Transactions.Should().BeNullOrEmpty();
		}

		/// <summary>
		/// Starts the agent with enabled=false and uses the API to capture 1 transaction with 2 spans
		/// Makes sure that the Tracer returns a NoopTransaction and NoopSpans and nothing is captured by the PayloadSender
		/// </summary>
		[Fact]
		public void AgentDisabledBasicTransactionWithSpans()
		{
			var payloadSender = new MockPayloadSender();
			var configReader = new MockConfigSnapshot(enabled: "false");
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, config: configReader));
			var transaction = agent.Tracer.StartTransaction("TestTransaction", "Test");
			transaction.Should().NotBeOfType<Transaction>();
			transaction.Should().BeOfType<NoopTransaction>();

			var span1 = transaction.StartSpan("test", "test");
			span1.Should().NotBeOfType<Span>();
			span1.Should().BeOfType<NoopSpan>();
			agent.Tracer.CurrentSpan.Should().Be(span1);
			span1.End();

			agent.Tracer.CurrentSpan.Should().BeNull();

			var span2 = span1.StartSpan("test2", "test");
			span2.Should().NotBeOfType<Span>();
			span2.Should().BeOfType<NoopSpan>();
			span2.End();

			transaction.End();
			payloadSender.Transactions.Should().BeNullOrEmpty();
			payloadSender.Spans.Should().BeNullOrEmpty();
		}

		/// <summary>
		/// Starts the agent with enabled=false and uses the API to capture 1 transaction with the convenient lambda based API.
		/// Makes sure that the Tracer returns a NoopTransaction and NoopSpans and nothing is captured by the PayloadSender
		/// </summary>
		[Fact]
		public void AgentDisabledTransactionWithLambdaAndSpans()
		{
			var payloadSender = new MockPayloadSender();
			var configReader = new MockConfigSnapshot(enabled: "false");
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, config: configReader));

			var block1Ran = false;
			var block2Ran = false;
			var block3Ran = false;

			agent.Tracer.CaptureTransaction("TestTransaction", "Test", transaction =>
			{
				block1Ran = true;
				transaction.Should().NotBeOfType<Transaction>();
				transaction.Should().BeOfType<NoopTransaction>();

				transaction.CaptureSpan("test", "test", span1 =>
				{
					block2Ran = true;
					span1.Should().NotBeOfType<Span>();
					span1.Should().BeOfType<NoopSpan>();
					span1.CaptureSpan("test2", "test", span2 =>
					{
						block3Ran = true;
						span2.Should().NotBeOfType<Span>();
						span2.Should().BeOfType<NoopSpan>();
					});
				});
			});

			block1Ran.Should().BeTrue();
			block2Ran.Should().BeTrue();
			block3Ran.Should().BeTrue();
			payloadSender.Transactions.Should().BeNullOrEmpty();
			payloadSender.Spans.Should().BeNullOrEmpty();
		}

		[Fact]
		public void AgentDisabledCaptureErrors()
		{
			var payloadSender = new MockPayloadSender();
			var configReader = new MockConfigSnapshot(enabled: "false");
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, config: configReader));

			//TODO: implement
		}
	}
}
