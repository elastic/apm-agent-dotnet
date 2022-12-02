using System.Runtime.Serialization;
using Elastic.Apm.Tests.Utilities;
using Xunit;
using Elastic.Apm;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Libraries.Newtonsoft.Json;
using Elastic.Apm.Model;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Transport;
using FluentAssertions;
using Moq;
using Xunit.Abstractions;

namespace Elastic.Clients.Elasticsearch.Tests;

public class ElasticSearchTests : IClassFixture<ElasticSearchTestFixture>
{
	private readonly ITestOutputHelper _testOutputHelper;
	private readonly ElasticSearchTestFixture _esClientListenerFixture;

	public ElasticSearchTests(ITestOutputHelper testOutputHelper, ElasticSearchTestFixture esClientListenerFixture)
	{
		_testOutputHelper = testOutputHelper;
		_esClientListenerFixture = esClientListenerFixture;
	}

	[Fact]
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

		payloadSender.FirstSpan.Otel.SpanKind.ToLower().Should().Be("client");
		payloadSender.FirstSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, string>("db.system", "elasticsearch"));
		payloadSender.FirstSpan.Otel.Attributes.Should()
			.Contain(new KeyValuePair<string, string>("http.url",
				$"https://localhost:{_esClientListenerFixture.Container.Port}/my-tweet-index/_doc/1"));
		payloadSender.FirstSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, string>("net.peer.name", "localhost"));
	}


	[Fact]
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

		payloadSender.FirstSpan.Otel.SpanKind.ToLower().Should().Be("client");
		payloadSender.FirstSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, string>("db.system", "elasticsearch"));
		payloadSender.FirstSpan.Otel.Attributes.Should()
			.Contain(new KeyValuePair<string, string>("http.url",
				$"https://localhost:{_esClientListenerFixture.Container.Port}/my-tweet-index/_doc/1"));
		payloadSender.FirstSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, string>("net.peer.name", "localhost"));
	}

	[Fact]
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

		payloadSender.FirstSpan.Otel.SpanKind.ToLower().Should().Be("client");
		payloadSender.FirstSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, string>("db.system", "elasticsearch"));
		payloadSender.FirstSpan.Otel.Attributes.Should()
			.Contain(new KeyValuePair<string, string>("http.url",
				$"https://localhost:{_esClientListenerFixture.Container.Port}/my-tweet-index/_search"));
		payloadSender.FirstSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, string>("net.peer.name", "localhost"));
	}

	[Fact]
	public async Task UpdateDocumentTest()
	{
		// make sure data is present
		await IndexData();

		if (_esClientListenerFixture.Cleint == null)
		{
			_testOutputHelper.WriteLine("ES Client is null - exiting test");
			return;
		}
		var response = await _esClientListenerFixture.Cleint.GetAsync<Tweet>("my-tweet-index", 1);

		var tweet = response.Source;
		tweet.Should().NotBeNull();
		if(tweet ==null)return;

		var (payloadSender, apmAgent) = SetUpAgent();

		await apmAgent.Tracer.CaptureTransaction("Test", "Foo", async () =>
		{
			await UpdateDocument(tweet);
		});

		var updateSpan = payloadSender.FirstSpan;
		updateSpan.Should().NotBeNull();

		payloadSender.Spans.Should().HaveCount(1);
		updateSpan.Name.Should().Be("Elasticsearch: POST /{index}/_update/{id}");
		updateSpan.Outcome.Should().Be(Outcome.Success);
		updateSpan.Otel.SpanKind.ToLower().Should().Be("client");
		updateSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, string>("db.system", "elasticsearch"));
		updateSpan.Otel.Attributes.Should()
			.Contain(new KeyValuePair<string, string>("http.url",
				$"https://localhost:{_esClientListenerFixture.Container.Port}/my-tweet-index/_update/1"));
		updateSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, string>("net.peer.name", "localhost"));
	}

	[Fact]
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

		payloadSender.FirstSpan.Otel.SpanKind.ToLower().Should().Be("client");
		payloadSender.FirstSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, string>("db.system", "elasticsearch"));
		payloadSender.FirstSpan.Otel.Attributes.Should()
			.Contain(new KeyValuePair<string, string>("http.url",
				$"https://localhost:{_esClientListenerFixture.Container.Port}/my-tweet-index/_doc/1"));
		payloadSender.FirstSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, string>("net.peer.name", "localhost"));
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

		if (_esClientListenerFixture.Cleint == null)
		{
			_testOutputHelper.WriteLine("ES Client is null - exiting test");
			return;
		}

		var response = await _esClientListenerFixture.Cleint.IndexAsync(tweet, request => request.Index("my-tweet-index"));

		response.IsSuccess().Should().BeTrue();
	}

	private async Task GetDocument()
	{
		if (_esClientListenerFixture.Cleint == null)
		{
			_testOutputHelper.WriteLine("ES Client is null - exiting test");
			return;
		}
		var response = await _esClientListenerFixture.Cleint.GetAsync<Tweet>(1, idx => idx.Index("my-tweet-index"));

		response.IsSuccess().Should().BeTrue();
		var tweet = response.Source;
		tweet.Should().NotBeNull();
	}

	private async Task SearchDocument()
	{
		if (_esClientListenerFixture.Cleint == null)
		{
			_testOutputHelper.WriteLine("ES Client is null - exiting test");
			return;
		}
		var response = await _esClientListenerFixture.Cleint.SearchAsync<Tweet>(s => s
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
		if (_esClientListenerFixture.Cleint == null)
		{
			_testOutputHelper.WriteLine("ES Client is null - exiting test");
			return;
		}

		tweet.Message = "This is a new message";
		var response2 = await _esClientListenerFixture.Cleint.UpdateAsync<Tweet, object>("my-tweet-index", 1, u => u
			.Doc(tweet));
		response2.IsValidResponse.Should().BeTrue();
	}

	private async Task DeleteDocument()
	{
		if (_esClientListenerFixture.Cleint == null)
		{
			_testOutputHelper.WriteLine("ES Client is null - exiting test");
			return;
		}

		var response = await _esClientListenerFixture.Cleint.DeleteAsync("my-tweet-index", 1);
		response.IsValidResponse.Should().BeTrue();
	}
}
