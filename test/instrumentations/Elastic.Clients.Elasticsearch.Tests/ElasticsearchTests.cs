// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm;
using Elastic.Apm.Api;
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

	[DockerFact]
	public async Task IndexDataTest()
	{
		var (payloadSender, apmAgent) = SetUpAgent();

		await apmAgent.Tracer.CaptureTransaction("Test", "Foo", IndexDataAsync);

		payloadSender.WaitForSpans();
		payloadSender.Spans.Should().HaveCount(2);
		var databaseSpan = payloadSender.Spans.SingleOrDefault(s => s.Type == ApiConstants.TypeDb);
		var elasticsearchSpan = databaseSpan.Should().NotBeNull().And.BeOfType<Apm.Model.Span>().Subject;

		elasticsearchSpan.Name.Should().Be("index");
		elasticsearchSpan.Outcome.Should().Be(Outcome.Success);
		elasticsearchSpan.Type = ApiConstants.TypeDb;
		elasticsearchSpan.Subtype = ApiConstants.SubtypeElasticsearch;

		elasticsearchSpan.Otel.SpanKind.ToLower().Should().Be("client");
		elasticsearchSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, object>("db.operation", "index"));
		elasticsearchSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, object>("http.request.method", "PUT"));

		VerifyCommonAttributes(elasticsearchSpan.Otel.Attributes);

		elasticsearchSpan.Otel.Attributes.Should()
			.Contain(new KeyValuePair<string, object>("url.full",
				$"{_esClientListenerFixture.Container.GetConnectionString()}my-tweet-index/_doc/1"));
	}

	[DockerFact]
	public async Task GetDocumentTest()
	{
		// make sure data is present
		await IndexDataAsync();

		var (payloadSender, apmAgent) = SetUpAgent();

		await apmAgent.Tracer.CaptureTransaction("Test", "Foo", GetDocumentAsync);

		payloadSender.WaitForSpans();
		payloadSender.Spans.Should().HaveCount(2);
		var databaseSpan = payloadSender.Spans.SingleOrDefault(s => s.Type == ApiConstants.TypeDb);
		var elasticsearchSpan = databaseSpan.Should().NotBeNull().And.BeOfType<Apm.Model.Span>().Subject;

		elasticsearchSpan.Name.Should().Be("get");
		elasticsearchSpan.Outcome.Should().Be(Outcome.Success);
		elasticsearchSpan.Type = ApiConstants.TypeDb;
		elasticsearchSpan.Subtype = ApiConstants.SubtypeElasticsearch;

		elasticsearchSpan.Otel.SpanKind.ToLower().Should().Be("client");
		elasticsearchSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, object>("db.operation", "get"));
		elasticsearchSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, object>("http.request.method", "GET"));

		VerifyCommonAttributes(elasticsearchSpan.Otel.Attributes);

		elasticsearchSpan.Otel.Attributes.Should()
			.Contain(new KeyValuePair<string, object>("url.full",
				$"{_esClientListenerFixture.Container.GetConnectionString()}my-tweet-index/_doc/1"));
	}

	[DockerFact]
	public async Task SearchDocumentTest()
	{
		// make sure data is present
		await IndexDataAsync();

		var (payloadSender, apmAgent) = SetUpAgent();

		await apmAgent.Tracer.CaptureTransaction("Test", "Foo", SearchDocumentAsync);

		payloadSender.Spans.Should().HaveCount(2);
		var databaseSpan = payloadSender.Spans.SingleOrDefault(s => s.Type == ApiConstants.TypeDb);
		var elasticsearchSpan = databaseSpan.Should().NotBeNull().And.BeOfType<Apm.Model.Span>().Subject;

		elasticsearchSpan.Name.Should().Be("search");
		elasticsearchSpan.Outcome.Should().Be(Outcome.Success);
		elasticsearchSpan.Type = ApiConstants.TypeDb;
		elasticsearchSpan.Subtype = ApiConstants.SubtypeElasticsearch;

		elasticsearchSpan.Otel.SpanKind.ToLower().Should().Be("client");
		elasticsearchSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, object>("db.operation", "search"));
		elasticsearchSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, object>("http.request.method", "POST"));

		VerifyCommonAttributes(elasticsearchSpan.Otel.Attributes, false);

		elasticsearchSpan.Otel.Attributes.Should()
			.Contain(new KeyValuePair<string, object>("url.full",
				$"{_esClientListenerFixture.Container.GetConnectionString()}my-tweet-index/_search"));
	}

	[DockerFact]
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
		elasticsearchSpan.Name.Should().Be("update");
		elasticsearchSpan.Outcome.Should().Be(Outcome.Success);

		elasticsearchSpan.Otel.SpanKind.ToLower().Should().Be("client");
		elasticsearchSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, object>("db.operation", "update"));
		elasticsearchSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, object>("http.request.method", "POST"));

		VerifyCommonAttributes(elasticsearchSpan.Otel.Attributes);

		elasticsearchSpan.Otel.Attributes.Should()
			.Contain(new KeyValuePair<string, object>("url.full",
				$"{_esClientListenerFixture.Container.GetConnectionString()}my-tweet-index/_update/1"));
	}

	[DockerFact]
	public async Task DeleteDocumentTest()
	{
		// make sure data is present
		await IndexDataAsync();

		var (payloadSender, apmAgent) = SetUpAgent();

		await apmAgent.Tracer.CaptureTransaction("Test", "Foo", DeleteDocumentAsync);

		payloadSender.Spans.Should().HaveCount(2);
		var databaseSpan = payloadSender.Spans.SingleOrDefault(s => s.Type == ApiConstants.TypeDb);
		var elasticsearchSpan = databaseSpan.Should().NotBeNull().And.BeOfType<Apm.Model.Span>().Subject;

		elasticsearchSpan.Name.Should().Be("delete");
		elasticsearchSpan.Outcome.Should().Be(Outcome.Success);
		elasticsearchSpan.Type = ApiConstants.TypeDb;
		elasticsearchSpan.Subtype = ApiConstants.SubtypeElasticsearch;

		elasticsearchSpan.Otel.SpanKind.ToLower().Should().Be("client");
		elasticsearchSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, object>("db.operation", "delete"));
		elasticsearchSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, object>("http.request.method", "DELETE"));

		VerifyCommonAttributes(elasticsearchSpan.Otel.Attributes);

		elasticsearchSpan.Otel.Attributes.Should()
			.Contain(new KeyValuePair<string, object>("url.full",
				$"{_esClientListenerFixture.Container.GetConnectionString()}my-tweet-index/_doc/1"));
	}

	private void VerifyCommonAttributes(Dictionary<string, object> attributes, bool expectId = true)
	{
		attributes.Should().ContainKey("db.elasticsearch.schema_url");
		attributes.Should().Contain(new KeyValuePair<string, object>("db.system", "elasticsearch"));
		attributes.Should().Contain(new KeyValuePair<string, object>("server.address", _esClientListenerFixture.Container.Hostname));

		attributes.Should().Contain(new KeyValuePair<string, object>("db.elasticsearch.path_parts.index", "my-tweet-index"));

		if (expectId)
			attributes.Should().Contain(new KeyValuePair<string, object>("db.elasticsearch.path_parts.id", "1"));

		attributes.Should().Contain(new KeyValuePair<string, object>("elastic.transport.product.name", "elasticsearch-net"));

		attributes.Should().ContainKey("elastic.transport.product.version");
		attributes["elastic.transport.product.version"].Should().BeOfType<string>().Subject.Should().StartWith("8.12.0+");

		attributes.Should().Contain(new KeyValuePair<string, object>("elastic.transport.attempted_nodes", 1));
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
