// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using DotNet.Testcontainers;
using DotNet.Testcontainers.Configurations;
using Elastic.Apm;
using Elastic.Apm.Api;
using Elastic.Apm.AspNetCore.Tests;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Elasticsearch;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.XUnit;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Clients.Elasticsearch.Tests;

public class ElasticsearchTests : IClassFixture<ElasticsearchTestFixture>
{
	private readonly ITestOutputHelper _testOutputHelper;
	private readonly ElasticsearchTestFixture _esClientListenerFixture;
	private readonly ElasticsearchClient _client;


	public ElasticsearchTests(ITestOutputHelper testOutputHelper, ElasticsearchTestFixture esClientListenerFixture)
	{
		_testOutputHelper = testOutputHelper;
		_esClientListenerFixture = esClientListenerFixture;
		_client = _esClientListenerFixture.Client ?? throw new Exception("ElasticsearchClient is `null`");
	}

	[DisabledOnWindowsGitHubActionsDockerFact]
	public async Task IndexDataTest()
	{
		var (payloadSender, apmAgent) = SetUpAgent();

		await apmAgent.Tracer.CaptureTransaction("Test", "Foo", IndexDataAsync);

		payloadSender.Spans.Should().HaveCount(2);
		var databaseSpan = payloadSender.Spans.SingleOrDefault(s => s.Type == ApiConstants.TypeDb);
		var elasticsearchSpan = databaseSpan.Should().NotBeNull().And.BeOfType<Apm.Model.Span>().Subject;

		elasticsearchSpan.Name.Should().Be("Elasticsearch: PUT /{index}/_doc/{id}");
		elasticsearchSpan.Outcome.Should().Be(Outcome.Success);
		elasticsearchSpan.Type = ApiConstants.TypeDb;
		elasticsearchSpan.Subtype = ApiConstants.SubtypeElasticsearch;

		elasticsearchSpan.Otel.SpanKind.ToLower().Should().Be("client");
		elasticsearchSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, object>("db.system", "elasticsearch"));
		elasticsearchSpan.Otel.Attributes.Should()
			.Contain(new KeyValuePair<string, object>("http.url",
				$"{_esClientListenerFixture.Container.GetConnectionString()}my-tweet-index/_doc/1"));
		elasticsearchSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, object>("net.peer.name", _esClientListenerFixture.Container.Hostname));
	}

	[DisabledOnWindowsGitHubActionsDockerFact]
	public async Task GetDocumentTest()
	{
		// make sure data is present
		await IndexDataAsync();

		var (payloadSender, apmAgent) = SetUpAgent();

		await apmAgent.Tracer.CaptureTransaction("Test", "Foo", GetDocumentAsync);

		payloadSender.Spans.Should().HaveCount(2);
		var databaseSpan = payloadSender.Spans.SingleOrDefault(s => s.Type == ApiConstants.TypeDb);
		var elasticsearchSpan = databaseSpan.Should().NotBeNull().And.BeOfType<Apm.Model.Span>().Subject;

		elasticsearchSpan.Name.Should().Be("Elasticsearch: GET /{index}/_doc/{id}");
		elasticsearchSpan.Outcome.Should().Be(Outcome.Success);
		elasticsearchSpan.Type = ApiConstants.TypeDb;
		elasticsearchSpan.Subtype = ApiConstants.SubtypeElasticsearch;

		elasticsearchSpan.Otel.SpanKind.ToLower().Should().Be("client");
		elasticsearchSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, object>("db.system", "elasticsearch"));
		elasticsearchSpan.Otel.Attributes.Should()
			.Contain(new KeyValuePair<string, object>("http.url",
				$"{_esClientListenerFixture.Container.GetConnectionString()}my-tweet-index/_doc/1"));
		elasticsearchSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, object>("net.peer.name", _esClientListenerFixture.Container.Hostname));
	}

	[DisabledOnWindowsGitHubActionsDockerFact]
	public async Task SearchDocumentTest()
	{
		// make sure data is present
		await IndexDataAsync();

		var (payloadSender, apmAgent) = SetUpAgent();

		await apmAgent.Tracer.CaptureTransaction("Test", "Foo", SearchDocumentAsync);

		payloadSender.Spans.Should().HaveCount(2);
		var databaseSpan = payloadSender.Spans.SingleOrDefault(s => s.Type == ApiConstants.TypeDb);
		var elasticsearchSpan = databaseSpan.Should().NotBeNull().And.BeOfType<Apm.Model.Span>().Subject;

		elasticsearchSpan.Name.Should().Be("Elasticsearch: POST /{index}/_search");
		elasticsearchSpan.Outcome.Should().Be(Outcome.Success);
		elasticsearchSpan.Type = ApiConstants.TypeDb;
		elasticsearchSpan.Subtype = ApiConstants.SubtypeElasticsearch;

		elasticsearchSpan.Otel.SpanKind.ToLower().Should().Be("client");
		elasticsearchSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, object>("db.system", "elasticsearch"));
		elasticsearchSpan.Otel.Attributes.Should()
			.Contain(new KeyValuePair<string, object>("http.url",
				$"{_esClientListenerFixture.Container.GetConnectionString()}my-tweet-index/_search"));
		elasticsearchSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, object>("net.peer.name", _esClientListenerFixture.Container.Hostname));
	}

	[DisabledOnWindowsGitHubActionsDockerFact]
	public async Task UpdateDocumentTest()
	{
		// make sure data is present
		await IndexDataAsync();
		var response = await _client.GetAsync<Tweet>("my-tweet-index", 1);

		var tweet = response.Source;
		tweet.Should().NotBeNull();
		if (tweet == null)
			return;

		var (payloadSender, apmAgent) = SetUpAgent();

		await apmAgent.Tracer.CaptureTransaction("Test", "Foo", async () =>
		{
			await UpdateDocumentAsync(tweet);
		});

		payloadSender.Spans.Should().HaveCount(2);
		var databaseSpan = payloadSender.Spans.SingleOrDefault(s => s.Type == ApiConstants.TypeDb);
		var elasticsearchSpan = databaseSpan.Should().NotBeNull().And.BeOfType<Apm.Model.Span>().Subject;

		elasticsearchSpan.Should().NotBeNull();
		elasticsearchSpan.Type = ApiConstants.TypeDb;
		elasticsearchSpan.Subtype = ApiConstants.SubtypeElasticsearch;
		elasticsearchSpan.Name.Should().Be("Elasticsearch: POST /{index}/_update/{id}");
		elasticsearchSpan.Outcome.Should().Be(Outcome.Success);

		elasticsearchSpan.Otel.SpanKind.ToLower().Should().Be("client");
		elasticsearchSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, object>("db.system", "elasticsearch"));
		elasticsearchSpan.Otel.Attributes.Should()
			.Contain(new KeyValuePair<string, object>("http.url",
				$"{_esClientListenerFixture.Container.GetConnectionString()}my-tweet-index/_update/1"));
		elasticsearchSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, object>("net.peer.name", _esClientListenerFixture.Container.Hostname));
	}

	[DisabledOnWindowsGitHubActionsDockerFact]
	public async Task DeleteDocumentTest()
	{
		// make sure data is present
		await IndexDataAsync();

		var (payloadSender, apmAgent) = SetUpAgent();

		await apmAgent.Tracer.CaptureTransaction("Test", "Foo", DeleteDocumentAsync);

		payloadSender.Spans.Should().HaveCount(2);
		var databaseSpan = payloadSender.Spans.SingleOrDefault(s => s.Type == ApiConstants.TypeDb);
		var elasticsearchSpan = databaseSpan.Should().NotBeNull().And.BeOfType<Apm.Model.Span>().Subject;

		elasticsearchSpan.Name.Should().Be("Elasticsearch: DELETE /{index}/_doc/{id}");
		elasticsearchSpan.Outcome.Should().Be(Outcome.Success);
		elasticsearchSpan.Type = ApiConstants.TypeDb;
		elasticsearchSpan.Subtype = ApiConstants.SubtypeElasticsearch;

		elasticsearchSpan.Otel.SpanKind.ToLower().Should().Be("client");
		elasticsearchSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, object>("db.system", "elasticsearch"));
		elasticsearchSpan.Otel.Attributes.Should()
			.Contain(new KeyValuePair<string, object>("http.url",
				$"{_esClientListenerFixture.Container.GetConnectionString()}my-tweet-index/_doc/1"));
		elasticsearchSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, object>("net.peer.name", _esClientListenerFixture.Container.Hostname));
	}

	private (MockPayloadSender, ApmAgent) SetUpAgent()
	{
		var payloadSender = new MockPayloadSender();
		var agent = new ApmAgent(new TestAgentComponents(
			new LineWriterToLoggerAdaptor(new XunitOutputToLineWriterAdaptor(_testOutputHelper)),
			payloadSender: payloadSender, configuration: new MockConfiguration(openTelemetryBridgeEnabled: "true"),
			apmServerInfo: MockApmServerInfo.Version80));

		// Enable outgoing HTTP capturing and later assert that no HTTP span is captured for the es calls as defined in our spec.
		agent.Subscribe(new HttpDiagnosticsSubscriber());

		// `ElasticsearchDiagnosticsSubscriber` is for the old Elasticsearch client, in these tests with the new client it does not create any spans.
		// Let's turn it on and make sure it doesn't cause any trouble with the new client.
		agent.Subscribe(new ElasticsearchDiagnosticsSubscriber());

		return (payloadSender, agent);
	}

	private class Tweet
	{
		public int Id { get; set; }
		public string? User { get; set; }
		public DateTime PostDate { get; set; }
		public string? Message { get; set; }
	}

	private async Task IndexDataAsync()
	{
		var tweet = new Tweet
		{
			Id = 1,
			User = "stevejgordon",
			PostDate = new DateTime(2009, 11, 15),
			Message = "Trying out the client, so far so good?"
		};

		var response = await _client.IndexAsync(tweet, request => request.Index("my-tweet-index"));

		response.IsSuccess().Should().BeTrue("{0}", response.DebugInformation);
	}

	private async Task GetDocumentAsync()
	{
		var response = await _client.GetAsync<Tweet>(1, idx => idx.Index("my-tweet-index"));

		response.IsSuccess().Should().BeTrue("{0}", response.DebugInformation);
		var tweet = response.Source;
		tweet.Should().NotBeNull();
	}

	private async Task SearchDocumentAsync()
	{
		var response = await _client.SearchAsync<Tweet>(s => s
			.Index("my-tweet-index")
			.From(0)
			.Size(1)
			.Query(q => q
				.Term(t => t.Id, 1)
			)
		);

		if (response.IsValidResponse)
		{
			var tweet = response.Documents.FirstOrDefault();
			Console.WriteLine($"Tweet: {tweet?.Message}");
		}
	}

	private async Task UpdateDocumentAsync(Tweet tweet)
	{
		tweet.Message = "This is a new message";
		var response = await _client.UpdateAsync<Tweet, object>("my-tweet-index", 1, u => u
			.Doc(tweet));
		response.IsValidResponse.Should().BeTrue("{0}", response.DebugInformation);
	}

	private async Task DeleteDocumentAsync()
	{
		var response = await _client.DeleteAsync("my-tweet-index", 1);
		response.IsValidResponse.Should().BeTrue("{0}", response.DebugInformation);
	}
}
