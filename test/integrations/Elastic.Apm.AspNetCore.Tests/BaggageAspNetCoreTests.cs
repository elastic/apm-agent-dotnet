// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Utilities.XUnit;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetCore.Tests;

[Collection("DiagnosticListenerTest")]
public class BaggageAspNetCoreTests : MultiApplicationTestBase
{

	public BaggageAspNetCoreTests(ITestOutputHelper output) : base(output) { }

	private void ValidateOtelAttribute(Transaction transaction, string key, string value) =>
		transaction.Otel.Attributes.Should().Contain(new KeyValuePair<string, object>($"baggage.{key}", value));

	[Fact]
	public async Task AccessBaggageFromUpstream()
	{
		var client = new HttpClient();
		client.DefaultRequestHeaders.Add("baggage", "key1=value1, key2 = value2, key3=value3");

		// Send traceparent to have a sampled trace
		client.DefaultRequestHeaders.Add("traceparent", "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");

		var res = await client.GetAsync("http://localhost:5901/Home/ReturnBaggageAsString");
		res.IsSuccessStatusCode.Should().BeTrue();

		(await res.Content.ReadAsStringAsync()).Should().Be("[key1, value1][key2, value2][key3, value3]");

		_payloadSender1.WaitForTransactions();
		_payloadSender1.FirstTransaction.IsSampled.Should().BeTrue();

		_payloadSender1.FirstTransaction.Context.Request.Headers.Should().ContainKey("baggage");
		_payloadSender1.FirstTransaction.Context.Request.Headers["baggage"]
			.Should()
			.Be("key1=value1, key2 = value2, key3=value3");

		ValidateOtelAttribute(_payloadSender1.FirstTransaction, "key1", "value1");
		ValidateOtelAttribute(_payloadSender1.FirstTransaction, "key2", "value2");
		ValidateOtelAttribute(_payloadSender1.FirstTransaction, "key3", "value3");
	}


	/// <summary>
	/// Calls the 1. service without any baggage, the /Home/WriteBaggage endpoint in the 1. service adds a baggage and then
	/// calls the 2. service as a downstream service.
	///
	/// The test makes sure that the agent in the 2. service captures the baggage added by the 1. service.
	/// </summary>
	public async Task MultipleServices()
	{
		var client = new HttpClient();
		client.DefaultRequestHeaders.Add("OutgoingServiceUrl", "http://localhost:5050");

		// Send traceparent to have a sampled trace
		client.DefaultRequestHeaders.Add("traceparent", "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");

		var res = await client.GetAsync("http://localhost:5901/Home/WriteBaggage");
		res.IsSuccessStatusCode.Should().BeTrue();

		_payloadSender1.WaitForTransactions();
		_payloadSender1.Transactions.Should().HaveCount(1);

		// Service 1 has no incoming baggage header and no captured baggage on the transaction
		_payloadSender1.FirstTransaction.Context.Request.Headers.Should().NotContainKey("baggage");
		_payloadSender1.FirstTransaction.Otel.Should().BeNull();


		_payloadSender2.Transactions.Should().HaveCount(1);

		// Service 2 has the baggage added by the Service 1
		_payloadSender2.FirstTransaction.Context.Request.Headers.Should().ContainKey("baggage");
		_payloadSender2.FirstTransaction.Context.Request.Headers["baggage"]
			.Should()
			.Be("foo=bar");

		ValidateOtelAttribute(_payloadSender2.FirstTransaction, "foo", "bar");
	}
}
