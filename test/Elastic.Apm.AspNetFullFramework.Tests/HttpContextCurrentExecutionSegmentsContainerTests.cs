// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
	}
}
