// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Linq;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests;

public class TransactionGroupNamesTests
{
	[Fact]
	public void TransactionGroupNames_AreApplied()
	{
		const string groupName = "GET /customer/*";

		var payloadSender = new MockPayloadSender();

		using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
			configuration: new MockConfiguration(transactionNameGroups: groupName)));

		agent.Tracer.CaptureTransaction("GET /order/1", "Test",
			transaction => { });

		payloadSender.WaitForTransactions();

		payloadSender.Transactions.Count.Should().Be(1);
		var transaction = payloadSender.Transactions.Single();
		transaction.Name.Should().Be("GET /order/1");

		payloadSender.Clear();

		agent.Tracer.CaptureTransaction("GET /customer/1", "Test",
			transaction => { });

		payloadSender.WaitForTransactions();
		payloadSender.Transactions.Count.Should().Be(1);
		transaction = payloadSender.Transactions.Single();
		transaction.Name.Should().Be(groupName);
	}
}
