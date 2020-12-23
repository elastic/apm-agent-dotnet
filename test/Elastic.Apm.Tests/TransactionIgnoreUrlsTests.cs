// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Api;
using Elastic.Apm.Report;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class TransactionIgnoreUrlsTests
	{
		[Fact]
		public void TransactionWithDefaultIgnoreUrlsSetting()
		{
			var payloadSender = new MockPayloadSenderWithFilters();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			agent.Tracer.CaptureTransaction("Test", "Test",
				transaction =>
				{
					transaction.Context.Request =
						new Request("GET", new Url { Full = "http://localhost/bootstrap.css", PathName = "/bootstrap.css" });
				});

			payloadSender.SignalEndTransactions();
			payloadSender.WaitForTransactions();
			payloadSender.Transactions.Should().BeEmpty();

			agent.Tracer.CaptureTransaction("Test", "Test",
				transaction =>
				{
					transaction.Context.Request =
						new Request("GET", new Url { Full = "http://localhost/home/index", PathName = "/home/index" });
				});

			payloadSender.WaitForTransactions();
			payloadSender.Transactions.Count.Should().Be(1);

			agent.Tracer.CaptureTransaction("Test", "Test",
				transaction => { });

			payloadSender.WaitForTransactions();
			payloadSender.Transactions.Count.Should().Be(2);
		}
	}
}
