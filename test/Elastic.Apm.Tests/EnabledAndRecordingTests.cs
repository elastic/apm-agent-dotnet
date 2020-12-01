// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Moq;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class EnabledAndRecordingTests
	{
		[Fact]
		public void Subscribers_Not_Subscribed_When_Agent_Disabled()
		{
			var payloadSender = new NoopPayloadSender();
			var configReader = new MockConfigSnapshot(enabled: "false");
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, config: configReader));

			var subscriber = new Mock<IDiagnosticsSubscriber>();
			agent.Subscribe(subscriber.Object);
			subscriber.Verify(s => s.Subscribe(It.IsAny<IApmAgent>()), Times.Never);
		}

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

		/// <summary>
		/// Makes sure that Tracer.CurrentSpan and Tracer.CurrentTransaction are set correctly when agent is disabled
		/// </summary>
		[Fact]
		public void CurrentTransactionAndSpanWithDisabledAgent()
		{
			var payloadSender = new MockPayloadSender();
			var configReader = new MockConfigSnapshot(enabled: "false");
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, config: configReader));

			var transaction = agent.Tracer.StartTransaction("foo", "bar");
			agent.Tracer.CurrentSpan.Should().BeNull();
			agent.Tracer.CurrentTransaction.Should().Be(transaction);

			var span1 = transaction.StartSpan("foo", "bar");
			agent.Tracer.CurrentSpan.Should().Be(span1);
			agent.Tracer.CurrentTransaction.Should().Be(transaction);

			var span2 = span1.StartSpan("foo", "bar");
			agent.Tracer.CurrentSpan.Should().Be(span2);
			agent.Tracer.CurrentTransaction.Should().Be(transaction);

			span2.End();

			agent.Tracer.CurrentSpan.Should().Be(span1);
			agent.Tracer.CurrentTransaction.Should().Be(transaction);

			span1.End();

			agent.Tracer.CurrentSpan.Should().BeNull();
			agent.Tracer.CurrentTransaction.Should().Be(transaction);

			transaction.End();

			agent.Tracer.CurrentSpan.Should().BeNull();
			agent.Tracer.CurrentTransaction.Should().BeNull();
		}

		[Fact]
		public void AgentDisabledCaptureErrors()
		{
			var payloadSender = new MockPayloadSender();
			var configReader = new MockConfigSnapshot(enabled: "false");
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, config: configReader));

			agent.Tracer.CaptureTransaction("foo", "bar", transaction =>
			{
				transaction.CaptureError("foo", "bar", new StackTrace().GetFrames());
				transaction.CaptureException(new Exception());

				transaction.CaptureSpan("foo", "bar", span =>
				{
					span.CaptureError("foo", "bar", new StackTrace().GetFrames());
					span.CaptureException(new Exception());
				});
			});

			payloadSender.Transactions.Should().BeNullOrEmpty();
			payloadSender.Spans.Should().BeNullOrEmpty();
		}

		/// <summary>
		/// Creates a transaction and multiple spans and exercises the public API with enabled=false.
		/// Makes sure nothing is captured.
		/// </summary>
		[Fact]
		public void CaptureTransactionAndSpansWithEnabledOnFalse()
		{
			var mockPayloadSender = new MockPayloadSender();
			var mockConfigSnapshot = new MockConfigSnapshot(enabled: "false");
			using var agent = new ApmAgent(new AgentComponents(payloadSender: mockPayloadSender, configurationReader: mockConfigSnapshot));

			CreateTransactionsAndSpans(agent);

			mockPayloadSender.Transactions.Should().BeEmpty();
			mockPayloadSender.Spans.Should().BeEmpty();
			mockPayloadSender.Errors.Should().BeEmpty();
			mockPayloadSender.Metrics.Should().BeEmpty();
		}

		/// <summary>
		/// Creates a transaction and multiple spans and exercises the public API with recording=false.
		/// Makes sure nothing is captured.
		/// </summary>
		[Fact]
		public void CaptureTransactionAndSpansWithRecordingOnFalse()
		{
			var mockPayloadSender = new MockPayloadSender();
			var mockConfigSnapshot = new MockConfigSnapshot(recording: "false");
			using var agent = new ApmAgent(new AgentComponents(payloadSender: mockPayloadSender, configurationReader: mockConfigSnapshot));

			CreateTransactionsAndSpans(agent);

			mockPayloadSender.Transactions.Should().BeEmpty();
			mockPayloadSender.Spans.Should().BeEmpty();
			mockPayloadSender.Errors.Should().BeEmpty();
		}

		private void CreateTransactionsAndSpans(ApmAgent agent)
		{
			var transaction1 = agent.Tracer.StartTransaction("Foo", "Bar");

			transaction1.SetLabel("foo", "bar");
			transaction1.Context.User = new User { Email = "a@b.d" };
			transaction1.Context.Response = new Response { Finished = true, StatusCode = 200 };
			transaction1.Context.Request = new Request("GET", new Url { Full = "http://localhost" });
			transaction1.CaptureError("foo", "nar", new StackTrace().GetFrames());


			var span1 = transaction1.StartSpan("foo", "bar");
			span1.Context.Db = new Database { Instance = "foo" };
			span1.SetLabel("foo", 42);
			span1.SetLabel("bar", true);

			span1.CaptureSpan("foo", "bar", span =>
			{
				span.Context.Http = new Http { Method = "GET", Url = "http://localhost" };
				span.CaptureError("foo", "nar", new StackTrace().GetFrames());
			});

			span1.CaptureException(new Exception());
			span1.End();
			transaction1.End();
		}
	}
}
