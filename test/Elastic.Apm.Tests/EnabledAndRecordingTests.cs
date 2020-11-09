// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using Elastic.Apm.Api;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class EnabledAndRecordingTests
	{
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
			span1.Context.Db = new Database { Instance = "foo", Statement = "Select * from foo" };
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
