using Elastic.Apm.Tests.Utilities;
using Xunit;
using Elastic.Apm;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Elasticsearch;
using Elastic.Apm.Tests.Utilities.Docker;
using FluentAssertions;
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

		await apmAgent.Tracer.CaptureTransaction("Test", "Foo", async () =>
		{
			await IndexData();
		});

		payloadSender.Spans.Should().HaveCount(1);
		payloadSender.FirstSpan.Name.Should().Be("Elasticsearch: PUT /{index}/_doc/{id}");
		payloadSender.FirstSpan.Outcome.Should().Be(Outcome.Success);
		payloadSender.FirstSpan.Type = ApiConstants.TypeDb;
		payloadSender.FirstSpan.Subtype = ApiConstants.SubtypeElasticsearch;

		payloadSender.FirstSpan.Otel.SpanKind.ToLower().Should().Be("client");
		payloadSender.FirstSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, string>("db.system", "elasticsearch"));
		payloadSender.FirstSpan.Otel.Attributes.Should()
			.Contain(new KeyValuePair<string, string>("http.url",
				$"{_esClientListenerFixture.Container.ConnectionString}/my-tweet-index/_doc/1"));
		payloadSender.FirstSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, string>("net.peer.name", _esClientListenerFixture.Container.Hostname));
	}


	[DockerFact]
	public async Task GetDocumentTest()
	{
		// make sure data is present
		await IndexData();

		var (payloadSender, apmAgent) = SetUpAgent();

		await apmAgent.Tracer.CaptureTransaction("Test", "Foo", async () =>
		{
			await GetDocument();
		});

		payloadSender.Spans.Should().HaveCount(1);
		payloadSender.FirstSpan.Name.Should().Be("Elasticsearch: GET /{index}/_doc/{id}");
		payloadSender.FirstSpan.Outcome.Should().Be(Outcome.Success);
		payloadSender.FirstSpan.Type = ApiConstants.TypeDb;
		payloadSender.FirstSpan.Subtype = ApiConstants.SubtypeElasticsearch;

		payloadSender.FirstSpan.Otel.SpanKind.ToLower().Should().Be("client");
		payloadSender.FirstSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, string>("db.system", "elasticsearch"));
		payloadSender.FirstSpan.Otel.Attributes.Should()
			.Contain(new KeyValuePair<string, string>("http.url",
				$"{_esClientListenerFixture.Container.ConnectionString}/my-tweet-index/_doc/1"));
		payloadSender.FirstSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, string>("net.peer.name", _esClientListenerFixture.Container.Hostname));
	}

	[DockerFact]
	public async Task SearchDocumentTest()
	{
		// make sure data is present
		await IndexData();

		var (payloadSender, apmAgent) = SetUpAgent();

		await apmAgent.Tracer.CaptureTransaction("Test", "Foo", async () =>
		{
			await SearchDocument();
		});

		payloadSender.Spans.Should().HaveCount(1);
		payloadSender.FirstSpan.Name.Should().Be("Elasticsearch: POST /{index}/_search");
		payloadSender.FirstSpan.Outcome.Should().Be(Outcome.Success);
		payloadSender.FirstSpan.Type = ApiConstants.TypeDb;
		payloadSender.FirstSpan.Subtype = ApiConstants.SubtypeElasticsearch;

		payloadSender.FirstSpan.Otel.SpanKind.ToLower().Should().Be("client");
		payloadSender.FirstSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, string>("db.system", "elasticsearch"));
		payloadSender.FirstSpan.Otel.Attributes.Should()
			.Contain(new KeyValuePair<string, string>("http.url",
				$"{_esClientListenerFixture.Container.ConnectionString}/my-tweet-index/_search"));
		payloadSender.FirstSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, string>("net.peer.name", _esClientListenerFixture.Container.Hostname));
	}

	[DockerFact]
	public async Task UpdateDocumentTest()
	{
		// make sure data is present
		await IndexData();
		var response = await _client.GetAsync<Tweet>("my-tweet-index", 1);

		var tweet = response.Source;
		tweet.Should().NotBeNull();
		if (tweet == null) return;

		var (payloadSender, apmAgent) = SetUpAgent();

		await apmAgent.Tracer.CaptureTransaction("Test", "Foo", async () =>
		{
			await UpdateDocument(tweet);
		});

		payloadSender.Spans.Should().HaveCount(1);

		var updateSpan = payloadSender.FirstSpan;

		updateSpan.Should().NotBeNull();
		updateSpan.Type = ApiConstants.TypeDb;
		updateSpan.Subtype = ApiConstants.SubtypeElasticsearch;
		updateSpan.Name.Should().Be("Elasticsearch: POST /{index}/_update/{id}");
		updateSpan.Outcome.Should().Be(Outcome.Success);
		updateSpan.Otel.SpanKind.ToLower().Should().Be("client");
		updateSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, string>("db.system", "elasticsearch"));
		updateSpan.Otel.Attributes.Should()
			.Contain(new KeyValuePair<string, string>("http.url",
				$"{_esClientListenerFixture.Container.ConnectionString}/my-tweet-index/_update/1"));
		updateSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, string>("net.peer.name", _esClientListenerFixture.Container.Hostname));
	}

	[DockerFact]
	public async Task DeleteDocumentTest()
	{
		// make sure data is present
		await IndexData();

		var (payloadSender, apmAgent) = SetUpAgent();

		await apmAgent.Tracer.CaptureTransaction("Test", "Foo", async () =>
		{
			await DeleteDocument();
		});

		payloadSender.Spans.Should().HaveCount(1);
		payloadSender.FirstSpan.Name.Should().Be("Elasticsearch: DELETE /{index}/_doc/{id}");
		payloadSender.FirstSpan.Outcome.Should().Be(Outcome.Success);
		payloadSender.FirstSpan.Type = ApiConstants.TypeDb;
		payloadSender.FirstSpan.Subtype = ApiConstants.SubtypeElasticsearch;


		payloadSender.FirstSpan.Otel.SpanKind.ToLower().Should().Be("client");
		payloadSender.FirstSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, string>("db.system", "elasticsearch"));
		payloadSender.FirstSpan.Otel.Attributes.Should()
			.Contain(new KeyValuePair<string, string>("http.url",
				$"{_esClientListenerFixture.Container.ConnectionString}/my-tweet-index/_doc/1"));
		payloadSender.FirstSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, string>("net.peer.name", _esClientListenerFixture.Container.Hostname));
	}

	private (MockPayloadSender, ApmAgent) SetUpAgent()
	{
		var payloadSender = new MockPayloadSender();
		var agent = new ApmAgent(new TestAgentComponents(
			new LineWriterToLoggerAdaptor(new XunitOutputToLineWriterAdaptor(_testOutputHelper)),
			payloadSender: payloadSender, configuration: new MockConfiguration(enableOpenTelemetryBridge: "true"),
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

	private async Task IndexData()
	{
		var tweet = new Tweet
		{
			Id = 1, User = "stevejgordon", PostDate = new DateTime(2009, 11, 15), Message = "Trying out the client, so far so good?"
		};

		var response = await _client.IndexAsync(tweet, request => request.Index("my-tweet-index"));

		response.IsSuccess().Should().BeTrue();
	}

	private async Task GetDocument()
	{
		var response = await _client.GetAsync<Tweet>(1, idx => idx.Index("my-tweet-index"));

		response.IsSuccess().Should().BeTrue();
		var tweet = response.Source;
		tweet.Should().NotBeNull();
	}

	private async Task SearchDocument()
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

	private async Task UpdateDocument(Tweet tweet)
	{
		tweet.Message = "This is a new message";
		var response2 = await _client.UpdateAsync<Tweet, object>("my-tweet-index", 1, u => u
			.Doc(tweet));
		response2.IsValidResponse.Should().BeTrue();
	}

	private async Task DeleteDocument()
	{
		var response = await _client.DeleteAsync("my-tweet-index", 1);
		response.IsValidResponse.Should().BeTrue();
	}
}
