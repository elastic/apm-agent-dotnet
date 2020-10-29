// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AspNetFullFrameworkSampleApp.Models;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection(Consts.AspNetFullFrameworkTestsCollection)]
	public class HttpContextCurrentExecutionSegmentsContainerTests : TestsBase
	{
		public HttpContextCurrentExecutionSegmentsContainerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
		{
		}

		[AspNetFullFrameworkFact]
		public async Task Transaction_And_Spans_Captured_When_Large_Request()
		{
			var samples = Enumerable.Range(1, 1_000)
				.Select(i => new CreateSampleDataViewModel { Name = $"Sample {i}" });

			var json = JsonConvert.SerializeObject(samples);
			var bytes = Encoding.UTF8.GetByteCount(json);

			// larger than 20Kb
			bytes.Should().BeGreaterThan(20_000);
			var content = new StringContent(json, Encoding.UTF8, "application/json");

			var client = new HttpClient();
			var bulkSamplesUri = Consts.SampleApp.CreateUrl("/Database/Bulk");
			var response = await client.PostAsync(bulkSamplesUri, content).ConfigureAwait(false);

			var responseContent = await response.Content.ReadAsStringAsync();
			response.IsSuccessStatusCode.Should().BeTrue(responseContent);

			await WaitAndCustomVerifyReceivedData(received =>
			{
				received.Transactions.Count.Should().Be(1);
				var transaction = received.Transactions.Single();

				transaction.SpanCount.Started.Should().Be(500);
				transaction.SpanCount.Dropped.Should().Be(501);
				received.Spans.Count.Should().Be(500);
			});
		}

		[AspNetFullFrameworkFact]
		public async Task Transaction_And_Spans_Captured_When_Controller_Action_Makes_Async_Http_Call()
		{
			var count = 100;
			var content = new StringContent($"{{\"count\":{count}}}", Encoding.UTF8, "application/json");

			var client = new HttpClient();
			var bulkSamplesUri = Consts.SampleApp.CreateUrl("/Database/Generate");
			var response = await client.PostAsync(bulkSamplesUri, content).ConfigureAwait(false);

			var responseContent = await response.Content.ReadAsStringAsync();
			response.IsSuccessStatusCode.Should().BeTrue(responseContent);

			await WaitAndCustomVerifyReceivedData(received =>
			{
				received.Transactions.Count.Should().Be(2);
				var transactions = received.Transactions
					.OrderByDescending(t => t.Timestamp)
					.ToList();

				var firstTransaction = transactions.First();
				firstTransaction.Name.Should().EndWith("Bulk");
				firstTransaction.SpanCount.Started.Should().Be(100);

				var secondTransaction = transactions.Last();
				secondTransaction.Name.Should().EndWith("Generate");
				secondTransaction.SpanCount.Started.Should().Be(3);

				received.Spans.Count.Should().Be(103);
			});
		}

		[AspNetFullFrameworkFact]
		public async Task Transaction_And_Spans_Captured_When_Multiple_Concurrent_Requests()
		{
			static HttpRequestMessage CreateMessage(int i)
			{
				var message = new HttpRequestMessage(HttpMethod.Get, Consts.SampleApp.CreateUrl("/Home/Contact"));
				message.Headers.Add("X-HttpRequest", i.ToString(CultureInfo.InvariantCulture));
				return message;
			}

			var count = 9;
			var messages = Enumerable.Range(1, count)
				.Select(i => CreateMessage(i))
				.ToList();

			// infinite timespan
			var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(-1) };

			var tasks = new List<Task<HttpResponseMessage>>(messages.Count);
			foreach (var message in messages)
				tasks.Add(client.SendAsync(message));

			await Task.WhenAll(tasks).ConfigureAwait(false);

			for (var index = 0; index < tasks.Count; index++)
			{
				var task = tasks[index];
				task.IsCompletedSuccessfully.Should().BeTrue();
				var response = task.Result;
				var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
				response.IsSuccessStatusCode.Should().BeTrue($"response {index}: {responseContent}");
			}

			await WaitAndCustomVerifyReceivedData(received =>
			{
				received.Transactions.Count.Should().Be(count * 2);

				var contactTransactions = received.Transactions
					.Where(t => t.Name == "GET Home/Contact")
					.ToList();

				contactTransactions.Should().HaveCount(count);

				var aboutTransactions = received.Transactions
					.Where(t => t.Name == "GET Home/About")
					.ToList();

				aboutTransactions.Should().HaveCount(count);

				// assert that each aboutTransaction is a child of a span associated with a contactTransaction
				foreach (var contactTransaction in contactTransactions)
				{
					contactTransaction.ParentId.Should().BeNull();
					var spans = received.Spans.Where(s => s.TransactionId == contactTransaction.Id)
						.ToList();

					spans.Should().HaveCount(2);

					var localHostSpan = spans.SingleOrDefault(s => s.Name == "GET localhost");
					localHostSpan.Should().NotBeNull();

					var aboutTransaction = aboutTransactions.SingleOrDefault(t => t.ParentId == localHostSpan.Id);
					aboutTransaction.Should().NotBeNull();
				}
			}).ConfigureAwait(false);
		}
	}
}
