// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Elastic.Apm.OpenTelemetry.Tests;

/// <summary>
/// ASP.NET Core hosting starts a 'Microsoft.AspNetCore.Hosting.HttpRequestIn' activity for every incoming request.
/// The Elastic ASP.NET Core and Azure Functions integrations create the request transaction from the hosting layer's
/// diagnostic events, so while either is loaded the bridge skips the activity to avoid a duplicate. Without them the
/// activity is the only representation of the request, and skipping it left every activity started while handling the
/// request without a transaction to nest under. The bridge therefore captures it as the transaction in that case.
/// <para>
/// This test project deliberately does not reference Elastic.Apm.AspNetCore; the bridge is on its own here.
/// </para>
/// </summary>
[Collection("OpenTelemetry")]
public class AspNetCoreRequestActivityTests
{
	private const string HostingSource = "Microsoft.AspNetCore";
	private const string RemoteTraceId = "0af7651916cd43dd8448eb211c80319c";
	private const string RemoteSpanId = "b7ad6b7169203331";

	private static ActivityContext RemoteContext => ActivityContext.Parse($"00-{RemoteTraceId}-{RemoteSpanId}-01", null);

	private static Activity? StartRequestActivity(ActivitySource source, ActivityContext parent = default) =>
		source.StartActivity(KnownListeners.MicrosoftAspNetCoreHostingHttpRequestIn, ActivityKind.Server, parent);

	/// <summary>
	/// Hosting passes its tags in when the activity is created, before it starts. Anything the bridge takes from them
	/// while the request is handled, rather than when it ends, has to be read from an activity in this shape.
	/// </summary>
	private static Activity? StartRequestActivityWithTags(ActivitySource source, string method = "GET", string path = "/orders/42")
	{
		var tags = new ActivityTagsCollection
		{
			{ "server.address", "localhost" },
			{ "server.port", 5000 },
			{ "http.request.method", method },
			{ "url.scheme", "http" },
			{ "url.path", path }
		};

		return source.StartActivity(KnownListeners.MicrosoftAspNetCoreHostingHttpRequestIn, ActivityKind.Server, default(ActivityContext), tags);
	}

	/// <summary>
	/// The tags .NET 10 hosting records when the request activity starts, once the bridge has enabled them through the
	/// AppContext switch. Hosting records nothing when the request ends, so there is no route or status code.
	/// </summary>
	private static void AddNet10HostingTags(Activity activity, string method = "GET", string path = "/orders/42")
	{
		activity.SetTag("server.address", "localhost");
		activity.SetTag("server.port", 5000);
		activity.SetTag("http.request.method", method);
		activity.SetTag("url.scheme", "http");
		activity.SetTag("url.path", path);
	}

	[Fact]
	public void TheAspNetCoreIntegrationIsNotLoadedInThisTestProcess() =>
		AppDomain.CurrentDomain.GetAssemblies().Should().NotContain(a => a.GetName().Name == "Elastic.Apm.AspNetCore",
			"these tests cover the bridge without the integration; a reference to it would make them meaningless");

	/// <summary>
	/// Hosting suppresses its request tags unless this switch says otherwise, and reads it once when the web host
	/// starts. The bridge enables the tags when it starts, which is what makes the end to end test below name the
	/// transaction on .NET 10.
	/// </summary>
	[Fact]
	public void StartingTheBridgeEnablesTheRuntimesRequestActivityTags()
	{
		using (new ApmAgent(new TestAgentComponents(payloadSender: new MockPayloadSender(), apmServerInfo: MockApmServerInfo.Version716)))
		{ }

		AppContext.TryGetSwitch(ElasticActivityListener.SuppressAspNetCoreActivityTagsSwitch, out var suppressed).Should().BeTrue();
		suppressed.Should().BeFalse();
	}

	[Fact]
	public void RequestActivityBecomesARequestTransaction()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			using (var request = StartRequestActivity(new ActivitySource(HostingSource)))
				AddNet10HostingTags(request!);
		}

		payloadSender.WaitForTransactions();
		payloadSender.Transactions.Should().HaveCount(1);
		payloadSender.FirstTransaction.Type.Should().Be(ApiConstants.TypeRequest);
		payloadSender.FirstTransaction.Otel.SpanKind.Should().Be("Server");
	}

	/// <summary>
	/// Hosting never sets a display name, so the name comes from the request method and path tags .NET 10 records, in
	/// the shape the ASP.NET Core integration uses for a request with no route data.
	/// </summary>
	[Fact]
	public void TransactionIsNamedFromTheRequestMethodAndPath()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			using (var request = StartRequestActivity(new ActivitySource(HostingSource)))
				AddNet10HostingTags(request!, "POST", "/orders");
		}

		payloadSender.WaitForTransactions();
		payloadSender.FirstTransaction.Name.Should().Be("POST /orders");
	}

	[Fact]
	public void RouteIsPreferredOverPathForTheTransactionName()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			using (var request = StartRequestActivity(new ActivitySource(HostingSource)))
			{
				AddNet10HostingTags(request!);
				request!.SetTag("http.route", "/orders/{id}");
			}
		}

		payloadSender.WaitForTransactions();
		payloadSender.FirstTransaction.Name.Should().Be("GET /orders/{id}");
	}

	/// <summary>The older convention carries the query string in http.target; the name should not.</summary>
	[Fact]
	public void OlderConventionTagsAreUnderstoodAndTheQueryStringIsDropped()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			using (var request = StartRequestActivity(new ActivitySource(HostingSource)))
			{
				request!.SetTag("http.method", "GET");
				request.SetTag("http.scheme", "https");
				request.SetTag("http.target", "/search?q=apm");
			}
		}

		payloadSender.WaitForTransactions();
		payloadSender.FirstTransaction.Name.Should().Be("GET /search");
		payloadSender.FirstTransaction.Type.Should().Be(ApiConstants.TypeRequest);
	}

	/// <summary>A non-standard method is recorded as '_OTHER' with the real one alongside it.</summary>
	[Fact]
	public void NonStandardMethodUsesTheOriginalMethodName()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			using (var request = StartRequestActivity(new ActivitySource(HostingSource)))
			{
				AddNet10HostingTags(request!, "_OTHER", "/orders");
				request!.SetTag("http.request.method_original", "PURGE");
			}
		}

		payloadSender.WaitForTransactions();
		payloadSender.FirstTransaction.Name.Should().Be("PURGE /orders");
	}

	/// <summary>
	/// On .NET 8 and 9 hosting records nothing on the activity, so there is nothing to name the transaction from and it
	/// keeps the activity name. It is still an HTTP request by definition.
	/// </summary>
	[Fact]
	public void RequestActivityWithoutTagsKeepsTheActivityNameButIsStillARequest()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			using (StartRequestActivity(new ActivitySource(HostingSource)))
			{ }
		}

		payloadSender.WaitForTransactions();
		payloadSender.FirstTransaction.Name.Should().Be(KnownListeners.MicrosoftAspNetCoreHostingHttpRequestIn);
		payloadSender.FirstTransaction.Type.Should().Be(ApiConstants.TypeRequest);
	}

	/// <summary>
	/// An OpenTelemetry instrumentation library for ASP.NET Core sets the display name to the method when the request
	/// starts and to the method and route once the route is known. That final name is adopted over the tags.
	/// </summary>
	[Fact]
	public void DisplayNameSetByAnInstrumentationLibraryWinsOverTheTags()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			using (var request = StartRequestActivity(new ActivitySource(HostingSource)))
			{
				request!.DisplayName = "GET";
				AddNet10HostingTags(request);
				request.DisplayName = "GET /orders/{id}";
			}
		}

		payloadSender.WaitForTransactions();
		payloadSender.FirstTransaction.Name.Should().Be("GET /orders/{id}");
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void DisplayNameSetBeforeStartWinsOverTheTags(bool renameAfterStart)
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			using var source = new ActivitySource(HostingSource);
			using var request = source.CreateActivity(KnownListeners.MicrosoftAspNetCoreHostingHttpRequestIn,
				ActivityKind.Server, default(ActivityContext));
			request.Should().NotBeNull();
			request!.DisplayName = "GET /orders/{id}";
			AddNet10HostingTags(request);
			request.Start();
			if (renameAfterStart)
				request.DisplayName = "GET /renamed/{id}";
		}

		payloadSender.WaitForTransactions();
		payloadSender.FirstTransaction.Name.Should().Be(renameAfterStart ? "GET /renamed/{id}" : "GET /orders/{id}");
	}

	[Fact]
	public void NameSetThroughTheElasticApiIsKept()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			using (var request = StartRequestActivity(new ActivitySource(HostingSource)))
			{
				AddNet10HostingTags(request!);
				agent.Tracer.CurrentTransaction!.Name = "NamedThroughTheElasticApi";
			}
		}

		payloadSender.WaitForTransactions();
		payloadSender.FirstTransaction.Name.Should().Be("NamedThroughTheElasticApi");
	}

	[Fact]
	public void IncomingTraceContextIsContinued()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			using (StartRequestActivity(new ActivitySource(HostingSource), RemoteContext))
			{ }
		}

		payloadSender.WaitForTransactions();
		payloadSender.FirstTransaction.TraceId.Should().Be(RemoteTraceId);
		payloadSender.FirstTransaction.ParentId.Should().Be(RemoteSpanId);
	}

	/// <summary>This is the point of capturing the request: work done while handling it has a transaction to nest under.</summary>
	[Fact]
	public void ActivitiesStartedWhileHandlingTheRequestBecomeSpansOfTheTransaction()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			using (StartRequestActivity(new ActivitySource(HostingSource)))
			using (new ActivitySource("Test.Request.Work").StartActivity("query", ActivityKind.Client))
			{ }
		}

		payloadSender.WaitForTransactions();
		payloadSender.WaitForSpans();
		payloadSender.Transactions.Should().HaveCount(1);
		payloadSender.Spans.Should().HaveCount(1);
		payloadSender.FirstSpan.Name.Should().Be("query");
		payloadSender.FirstSpan.ParentId.Should().Be(payloadSender.FirstTransaction.Id);
		payloadSender.FirstSpan.TraceId.Should().Be(payloadSender.FirstTransaction.TraceId);
	}

	/// <summary>
	/// Both of these packages create the request transaction themselves, from the hosting layer's diagnostic events.
	/// </summary>
	[Theory]
	[InlineData("Elastic.Apm.AspNetCore")]
	[InlineData("Elastic.Apm.Azure.Functions")]
	public void RequestActivityIsSkippedWhileAnIntegrationWhichCreatesTheRequestTransactionIsLoaded(string assemblyName)
	{
		var payloadSender = new MockPayloadSender();
		var components = new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716);

		using (new ApmAgent(components))
		{
			components.ElasticActivityListener.CheckAssembly(assemblyName);

			using (var request = StartRequestActivity(new ActivitySource(HostingSource)))
				AddNet10HostingTags(request!);
		}

		payloadSender.Transactions.Should().BeEmpty();
		payloadSender.Spans.Should().BeEmpty();
	}

	[Fact]
	public void DetectionOfTheIntegrationIsLogged()
	{
		var logger = new TestLogger(Elastic.Apm.Logging.LogLevel.Debug);
		var components = new TestAgentComponents(logger: logger, payloadSender: new MockPayloadSender(), apmServerInfo: MockApmServerInfo.Version716);

		using (new ApmAgent(components))
			components.ElasticActivityListener.CheckAssembly("Elastic.Apm.AspNetCore");

		logger.Log.Should().Contain("Detected 'Elastic.Apm.AspNetCore'");
		logger.Log.Should().Contain(KnownListeners.MicrosoftAspNetCoreHostingHttpRequestIn);
	}

	/// <summary>
	/// APM server rebuilds the transaction's URL from the OTel attributes, but an error captured while the request is
	/// handled copies the transaction's context and carries no attributes, so the request context has to be filled.
	/// </summary>
	[Fact]
	public void RequestContextIsFilledFromTheCurrentConventionTags()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			using (var request = StartRequestActivity(new ActivitySource(HostingSource)))
			{
				AddNet10HostingTags(request!);
				request!.SetTag("url.query", "q=apm");
			}
		}

		payloadSender.WaitForTransactions();

		var context = payloadSender.FirstTransaction.Context.Request;
		context.Method.Should().Be("GET");
		context.Url.Full.Should().Be("http://localhost:5000/orders/42?q=apm");
		context.Url.Raw.Should().Be("http://localhost:5000/orders/42?q=apm");
		context.Url.HostName.Should().Be("localhost");
		context.Url.PathName.Should().Be("/orders/42");
		context.Url.Search.Should().Be("q=apm");
		context.Url.Protocol.Should().Be("HTTP");
	}

	/// <summary>The older convention records the path and the query together, and the port within the host.</summary>
	[Fact]
	public void RequestContextIsFilledFromTheOlderConventionTags()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			using (var request = StartRequestActivity(new ActivitySource(HostingSource)))
			{
				request!.SetTag("http.method", "POST");
				request.SetTag("http.scheme", "https");
				request.SetTag("http.host", "testing.invalid:8443");
				request.SetTag("http.target", "/search?q=apm");
			}
		}

		payloadSender.WaitForTransactions();

		var context = payloadSender.FirstTransaction.Context.Request;
		context.Method.Should().Be("POST");
		context.Url.Full.Should().Be("https://testing.invalid:8443/search?q=apm");
		context.Url.HostName.Should().Be("testing.invalid");
		context.Url.PathName.Should().Be("/search");
		context.Url.Search.Should().Be("q=apm");
	}

	/// <summary>
	/// The host is recorded separately from the port under the current convention, while the older 'http.host' carries
	/// both. An IPv6 literal has to be bracketed to form a valid authority, whether or not it is recorded that way.
	/// </summary>
	[Theory]
	[InlineData("server.address", "localhost", "http://localhost:5000/orders/42")]
	[InlineData("server.address", "[::1]", "http://[::1]:5000/orders/42")]
	[InlineData("server.address", "::1", "http://[::1]:5000/orders/42")]
	[InlineData("net.host.name", "127.0.0.1", "http://127.0.0.1:5000/orders/42")]
	public void HostAndPortAreCombinedIntoTheUrl(string hostAttribute, string host, string expectedUrl)
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			using (var request = StartRequestActivity(new ActivitySource(HostingSource)))
			{
				request!.SetTag("http.request.method", "GET");
				request.SetTag("url.scheme", "http");
				request.SetTag("url.path", "/orders/42");
				request.SetTag(hostAttribute, host);
				request.SetTag("server.port", 5000);
			}
		}

		payloadSender.WaitForTransactions();
		payloadSender.FirstTransaction.Context.Request.Url.Full.Should().Be(expectedUrl);
	}

	/// <summary>An instrumentation library may record the whole URL, in which case there is nothing to rebuild.</summary>
	[Fact]
	public void FullUrlRecordedOnTheActivityIsUsedAsIs()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			using (var request = StartRequestActivity(new ActivitySource(HostingSource)))
			{
				AddNet10HostingTags(request!);
				request!.SetTag("url.full", "http://elastic.invalid/orders/42");
			}
		}

		payloadSender.WaitForTransactions();
		payloadSender.FirstTransaction.Context.Request.Url.Full.Should().Be("http://elastic.invalid/orders/42");
	}

	/// <summary>Without a host or a scheme there is no absolute URL to build, but the path is worth recording alone.</summary>
	[Fact]
	public void PathOnlyIsRecordedWhenTheUrlCannotBeBuilt()
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			using (var request = StartRequestActivity(new ActivitySource(HostingSource)))
			{
				request!.SetTag("http.request.method", "GET");
				request.SetTag("url.path", "/orders/42");
			}
		}

		payloadSender.WaitForTransactions();

		var url = payloadSender.FirstTransaction.Context.Request.Url;
		url.Full.Should().BeNull();
		url.PathName.Should().Be("/orders/42");
	}

	/// <summary>
	/// On .NET 8 and 9 hosting records no tags, so there is nothing to build a request context from. The intake also
	/// requires a method on it, so an activity with a URL but no method gets none either.
	/// </summary>
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void NoRequestContextWithoutAMethod(bool withUrl)
	{
		var payloadSender = new MockPayloadSender();
		using (new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			using (var request = StartRequestActivity(new ActivitySource(HostingSource)))
			{
				if (withUrl)
				{
					request!.SetTag("url.scheme", "http");
					request.SetTag("url.path", "/orders/42");
				}
			}
		}

		payloadSender.WaitForTransactions();
		payloadSender.FirstTransaction.Context.Request.Should().BeNull();
	}

	/// <summary>A request context set through the Elastic API is not replaced.</summary>
	[Fact]
	public void RequestContextSetThroughTheElasticApiIsKept()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			using (var request = StartRequestActivity(new ActivitySource(HostingSource)))
			{
				AddNet10HostingTags(request!);
				agent.Tracer.CurrentTransaction!.Context.Request =
					new Request("PUT", new Url { Full = "http://set.through.the.api/" });
			}
		}

		payloadSender.WaitForTransactions();

		var context = payloadSender.FirstTransaction.Context.Request;
		context.Method.Should().Be("PUT");
		context.Url.Full.Should().Be("http://set.through.the.api/");
	}

	/// <summary>
	/// This is what the request context is for: the error copies the transaction's context as it stands when it is
	/// captured, and carries no OTel attributes of its own for APM server to rebuild a URL from.
	/// </summary>
	[Fact]
	public void ErrorCapturedWhileHandlingTheRequestCarriesTheUrl()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716)))
		{
			using (StartRequestActivityWithTags(new ActivitySource(HostingSource)))
				agent.Tracer.CurrentTransaction!.CaptureException(new Exception("boom"));
		}

		payloadSender.WaitForTransactions();
		payloadSender.WaitForErrors();

		payloadSender.FirstError.Context.Request.Url.Full.Should().Be("http://localhost:5000/orders/42");
		payloadSender.FirstError.Context.Request.Method.Should().Be("GET");
	}

	[Fact]
	public async Task DeniedHostingSourceDoesNotCaptureRequestsThroughTheLoggingFallback()
	{
		var payloadSender = new MockPayloadSender();
		using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716,
			configuration: new MockConfiguration(openTelemetryBridgeDeniedActivitySources: HostingSource)));
		using var hostingSource = new ActivitySource(HostingSource);
		hostingSource.HasListeners().Should().BeFalse();

		var builder = WebApplication.CreateBuilder();
		builder.Logging.ClearProviders();
		builder.Logging.AddConsole();
		builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Information);
		builder.WebHost.UseUrls("http://127.0.0.1:0");

		Activity? requestActivity = null;
		await using var app = builder.Build();
		app.MapGet("/orders/{id:int}", (int id) =>
		{
			requestActivity = Activity.Current;
			return Results.Ok(id);
		});

		await app.StartAsync();
		try
		{
			using var client = new HttpClient();
			client.DefaultRequestHeaders.Add("traceparent", $"00-{RemoteTraceId}-{RemoteSpanId}-01");
			using var response = await client.GetAsync($"{app.Urls.First()}/orders/42");
			response.EnsureSuccessStatusCode();
		}
		finally
		{
			await app.StopAsync();
		}

		requestActivity.Should().NotBeNull();
		requestActivity!.OperationName.Should().Be(KnownListeners.MicrosoftAspNetCoreHostingHttpRequestIn);
		requestActivity.Source.Name.Should().BeEmpty("hosting must use its sourceless fallback");
		payloadSender.Transactions.Should().BeEmpty();
		payloadSender.Spans.Should().BeEmpty();
	}

	/// <summary>
	/// End to end against a real ASP.NET Core server, which is what decides whether the runtime's own request activity
	/// reaches the bridge in the shape the tests above assume. The client sends a traceparent of its own, so the
	/// transaction has to continue that trace.
	/// </summary>
	[Fact]
	public async Task BridgeOnlyAspNetCoreApplicationProducesARequestTransaction()
	{
		var payloadSender = new MockPayloadSender();
		using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, apmServerInfo: MockApmServerInfo.Version716));

		var builder = WebApplication.CreateBuilder();
		builder.Logging.ClearProviders();
		builder.WebHost.UseUrls("http://127.0.0.1:0");

		await using var app = builder.Build();
		app.MapGet("/orders/{id:int}", (int id) =>
		{
			using (new ActivitySource("Test.Request.Handler").StartActivity("load-order", ActivityKind.Client))
			{ }
			return Results.Ok(id);
		});

		await app.StartAsync();
		var baseUrl = app.Urls.First();
		try
		{
			using var client = new HttpClient();
			client.DefaultRequestHeaders.Add("traceparent", $"00-{RemoteTraceId}-{RemoteSpanId}-01");
			var response = await client.GetAsync($"{baseUrl}/orders/42");
			response.EnsureSuccessStatusCode();
		}
		finally
		{
			await app.StopAsync();
		}

		payloadSender.WaitForTransactions();
		payloadSender.WaitForSpans();

		var transaction = payloadSender.Transactions.Should().ContainSingle().Which;
		transaction.Type.Should().Be(ApiConstants.TypeRequest);
		transaction.TraceId.Should().Be(RemoteTraceId);
		transaction.ParentId.Should().Be(RemoteSpanId);
#if NET10_0_OR_GREATER
		transaction.Name.Should().Be("GET /orders/42");
		transaction.Context.Request.Method.Should().Be("GET");
		transaction.Context.Request.Url.Full.Should().Be($"{baseUrl}/orders/42");
		transaction.Context.Request.Url.PathName.Should().Be("/orders/42");
#else
		transaction.Name.Should().Be(KnownListeners.MicrosoftAspNetCoreHostingHttpRequestIn);
		transaction.Context.Request.Should().BeNull("hosting records no tags before .NET 10");
#endif

		var span = payloadSender.Spans.Should().ContainSingle().Which;
		span.Name.Should().Be("load-order");
		span.ParentId.Should().Be(transaction.Id);
	}
}
