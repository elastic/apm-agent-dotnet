using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Mocks;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SampleAspNetCoreApp;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Uses the samples/SampleAspNetCoreApp as the test application and tests the agent with it.
	/// It's basically an integration test.
	/// </summary>
	[Collection("DiagnosticListenerTest")] //To avoid tests from DiagnosticListenerTests running in parallel with this we add them to 1 collection.
	public class AspNetCoreMiddlewareTests : LoggingTestBase, IClassFixture<WebApplicationFactory<Startup>>
	{
		private const string ThisClassName = nameof(AspNetCoreMiddlewareTests);
		private readonly ApmAgent _agent;
		private readonly MockPayloadSender _capturedPayload;
		private readonly WebApplicationFactory<Startup> _factory;

		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		private readonly IApmLogger _logger;

		public AspNetCoreMiddlewareTests(WebApplicationFactory<Startup> factory, ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper)
		{
			_logger = LoggerBase.Scoped(ThisClassName);
			_factory = factory;

			// We need to ensure Agent.Instance is created because we need _agent to use Agent.Instance CurrentExecutionSegmentsContainer
			AgentSingletonUtils.EnsureInstanceCreated();
			_agent = new ApmAgent(new TestAgentComponents(
				_logger,
				new MockConfigSnapshot(_logger, captureBody: ConfigConsts.SupportedValues.CaptureBodyAll),
				// _agent needs to share CurrentExecutionSegmentsContainer with Agent.Instance
				// because the sample application used by the tests (SampleAspNetCoreApp) uses Agent.Instance.Tracer.CurrentTransaction/CurrentSpan
				currentExecutionSegmentsContainer: Agent.Instance.TracerInternal.CurrentExecutionSegmentsContainer)
			);
			ApmMiddlewareExtension.UpdateServiceInformation(_agent.Service);

			_capturedPayload = _agent.PayloadSender as MockPayloadSender;
			_client = Helper.GetClient(_agent, _factory);
		}

		private HttpClient _client;

		/// <summary>
		/// Simulates an HTTP GET call to /home/simplePage and asserts on what the agent should send to the server
		/// </summary>
		[Fact]
		public async Task HomeSimplePageTransactionTest()
		{
			var headerKey = "X-Additional-Header";
			var headerValue = "For-Elastic-Apm-Agent";
			_client.DefaultRequestHeaders.Add(headerKey, headerValue);
			var response = await _client.GetAsync("/Home/SimplePage");

			//test service
			_capturedPayload.Transactions.Should().ContainSingle();

			_agent.Service.Name.Should()
				.NotBeNullOrWhiteSpace()
				.And.NotBe(ConfigConsts.DefaultValues.UnknownServiceName);

			_agent.Service.Agent.Name.Should().Be(Apm.Consts.AgentName);
			var apmVersion = typeof(Agent).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
			_agent.Service.Agent.Version.Should().Be(apmVersion);

			_agent.Service.Framework.Name.Should().Be("ASP.NET Core");

			var aspNetCoreVersion = Assembly.Load("Microsoft.AspNetCore").GetName().Version.ToString();
			_agent.Service.Framework.Version.Should().Be(aspNetCoreVersion);

			_agent.Service.Runtime.Name.Should().Be(Runtime.DotNetCoreName);
			_agent.Service.Runtime.Version.Should().Be(Directory.GetParent(typeof(object).Assembly.Location).Name);

			_capturedPayload.Transactions.Should().ContainSingle();
			var transaction = _capturedPayload.FirstTransaction;
			var transactionName = $"{response.RequestMessage.Method} Home/SimplePage";
			transaction.Name.Should().Be(transactionName);
			transaction.Result.Should().Be("HTTP 2xx");
			transaction.Duration.Should().BeGreaterThan(0);

			transaction.Type.Should().Be("request");
			transaction.Id.Should().NotBeEmpty();

			//test transaction.context.response
			transaction.Context.Response.StatusCode.Should().Be(200);
			if (_agent.ConfigurationReader.CaptureHeaders)
			{
				transaction.Context.Response.Headers.Should().NotBeNull();
				transaction.Context.Response.Headers.Should().NotBeEmpty();

				transaction.Context.Response.Headers.Should().ContainKeys(headerKey);
				transaction.Context.Response.Headers[headerKey].Should().Be(headerValue);
			}

			//test transaction.context.request
			transaction.Context.Request.HttpVersion.Should().Be("2.0");
			transaction.Context.Request.Method.Should().Be("GET");

			//test transaction.context.request.url
			transaction.Context.Request.Url.Full.Should().Be(response.RequestMessage.RequestUri.AbsoluteUri);
			transaction.Context.Request.Url.HostName.Should().Be("localhost");
			transaction.Context.Request.Url.Protocol.Should().Be("HTTP");

			if (_agent.ConfigurationReader.CaptureHeaders)
			{
				transaction.Context.Request.Headers.Should().NotBeNull();
				transaction.Context.Request.Headers.Should().NotBeEmpty();

				transaction.Context.Request.Headers.Should().ContainKeys(headerKey);
				transaction.Context.Request.Headers[headerKey].Should().Be(headerValue);
			}

			//test transaction.context.request.encrypted
			transaction.Context.Request.Socket.Encrypted.Should().BeFalse();
		}

		/// <summary>
		/// Simulates an HTTP POST call to /home/simplePage and asserts on what the agent should send to the server
		/// to test the 'CaptureBody' configuration option
		/// </summary>
		[Fact]
		public async Task HomeSimplePagePostTransactionTest()
		{
			var headerKey = "X-Additional-Header";
			var headerValue = "For-Elastic-Apm-Agent";
			_client.DefaultRequestHeaders.Add(headerKey, headerValue);
			_client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			var body = "{\"id\" : \"1\"}";
			var response = await _client.PostAsync("api/Home/Post", new StringContent(body, Encoding.UTF8, "application/json"));

			_capturedPayload.Transactions.Should().ContainSingle();

			_agent.Service.Name.Should()
				.NotBeNullOrWhiteSpace()
				.And.NotBe(ConfigConsts.DefaultValues.UnknownServiceName);

			_agent.Service.Agent.Name.Should().Be(Apm.Consts.AgentName);
			var apmVersion = typeof(Agent).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
			_agent.Service.Agent.Version.Should().Be(apmVersion);

			_agent.Service.Framework.Name.Should().Be("ASP.NET Core");

			var aspNetCoreVersion = Assembly.Load("Microsoft.AspNetCore").GetName().Version.ToString();
			_agent.Service.Framework.Version.Should().Be(aspNetCoreVersion);

			_agent.Service.Runtime.Name.Should().Be(Runtime.DotNetCoreName);
			_agent.Service.Runtime.Version.Should().Be(Directory.GetParent(typeof(object).Assembly.Location).Name);

			_capturedPayload.Transactions.Should().ContainSingle();
			var transaction = _capturedPayload.FirstTransaction;
			var transactionName = "POST Home/Post";
			transaction.Name.Should().Be(transactionName);
			transaction.Result.Should().Be("HTTP 2xx");
			transaction.Duration.Should().BeGreaterThan(0);

			transaction.Type.Should().Be("request");
			transaction.Id.Should().NotBeEmpty();

			//test transaction.context.response
			transaction.Context.Response.StatusCode.Should().Be(200);
			if (_agent.ConfigurationReader.CaptureHeaders)
			{
				transaction.Context.Response.Headers.Should().NotBeNull();
				transaction.Context.Response.Headers.Should().NotBeEmpty();
			}

			if (_agent.ConfigurationReader.CaptureBody != "off")
			{
				transaction.Context.Request.Body.Should().NotBeNull();
				transaction.Context.Request.Body.Should().Be(body);
			}

			//test transaction.context.request
			transaction.Context.Request.HttpVersion.Should().Be("2.0");
			transaction.Context.Request.Method.Should().Be("POST");

			//test transaction.context.request.url
			transaction.Context.Request.Url.Full.Should().Be(response.RequestMessage.RequestUri.AbsoluteUri);
			transaction.Context.Request.Url.HostName.Should().Be("localhost");
			transaction.Context.Request.Url.Protocol.Should().Be("HTTP");

			if (_agent.ConfigurationReader.CaptureHeaders)
			{
				transaction.Context.Request.Headers.Should().NotBeNull();
				transaction.Context.Request.Headers.Should().NotBeEmpty();

				transaction.Context.Request.Headers.Should().ContainKeys(headerKey);
				transaction.Context.Request.Headers[headerKey].Should().Be(headerValue);
			}

			//test transaction.context.request.encrypted
			transaction.Context.Request.Socket.Encrypted.Should().BeFalse();
		}

		/// <summary>
		/// Simulates an HTTP GET call to /home/index and asserts that the agent captures spans.
		/// Prerequisite: The /home/index has to generate spans (which should be the case).
		/// </summary>
		[Fact]
		public async Task HomeIndexSpanTest()
		{
			var response = await _client.GetAsync("/Home/Index");

			response.IsSuccessStatusCode.Should().BeTrue();
			_capturedPayload.SpansOnFirstTransaction.Should().NotBeEmpty().And.Contain(n => n.Context.Db != null);
		}

		/// <summary>
		/// Simulates an HTTP GET call to /Home/Index?captureControllerActionAsSpan=true
		/// and asserts that all automatically captured spans are children of the span for controller's action.
		/// </summary>
		[Fact]
		public async Task HomeIndexAutoCapturedSpansAreChildrenOfControllerActionAsSpan()
		{
			var response = await _client.GetAsync("/Home/Index?captureControllerActionAsSpan=true");

			response.IsSuccessStatusCode.Should().BeTrue();
			var spans = _capturedPayload.SpansOnFirstTransaction;
			spans.Should().NotBeEmpty();
			var controllerActionSpan = spans.Last();
			controllerActionSpan.Name.Should().Be("Index_span_name");
			controllerActionSpan.Type.Should().Be("Index_span_type");
			var dbSpans = spans.Where(span => span.Context.Db != null);
			// ReSharper disable PossibleMultipleEnumeration
			dbSpans.Should().NotBeEmpty();
			foreach (var dbSpan in dbSpans)
			{
				dbSpan.Type.Should().Be(ApiConstants.TypeDb);
				dbSpan.Subtype.Should().Be(ApiConstants.SubtypeSqLite);
				dbSpan.ParentId.Should().Be(controllerActionSpan.Id);
			}
			var httpSpans = spans.Where(span => span.Context.Http != null);
			httpSpans.Should().NotBeEmpty();
			foreach (var httpSpan in httpSpans) httpSpan.ParentId.Should().Be(controllerActionSpan.Id);
			// ReSharper restore PossibleMultipleEnumeration
		}

		/// <summary>
		/// Configures an ASP.NET Core application without an error page.
		/// With other words: there is no error page with an exception handler configured in the ASP.NET Core pipeline.
		/// Makes sure that we still capture the failed request.
		/// </summary>
		[Fact]
		public async Task FailingRequestWithoutConfiguredExceptionPage()
		{
			_client = Helper.GetClientWithoutExceptionPage(_agent, _factory);

			Func<Task> act = async () => await _client.GetAsync("Home/TriggerError");
			await act.Should().ThrowAsync<Exception>();

			_capturedPayload.Transactions.Should().ContainSingle();

			_capturedPayload.Errors.Should().NotBeEmpty();

			_capturedPayload.Errors.Should().ContainSingle();

			//also make sure the label is captured
			var error = _capturedPayload.Errors[0] as Error;
			error.Should().NotBeNull();

			var errorDetail = error?.Exception;
			errorDetail.Should().NotBeNull();

			var labels = error?.Context.Labels;
			labels.Should().NotBeEmpty().And.ContainKey("foo").And.Contain("foo", "bar");
		}

		/// <summary>
		/// Configures an ASP.NET Core application without an error page.
		/// With other words: there is no error page with an exception handler configured in the ASP.NET Core pipeline.
		/// Makes sure that we still capture the failed request along with the request body
		/// </summary>
		[Fact]
		public async Task FailingPostRequestWithoutConfiguredExceptionPage()
		{
			_client = Helper.GetClientWithoutExceptionPage(_agent, _factory);

			_client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			var body = "{\"id\" : \"1\"}";
			Func<Task> act = async () => await _client.PostAsync("api/Home/PostError", new StringContent(body, Encoding.UTF8, "application/json"));

			await act.Should().ThrowAsync<Exception>();

			_capturedPayload.Transactions.Should().ContainSingle();
			_capturedPayload.Errors.Should().NotBeEmpty();
			_capturedPayload.Errors.Should().ContainSingle();

			var error = _capturedPayload.Errors[0] as Error;
			error.Should().NotBeNull();

			var errorDetail = error?.Exception;
			errorDetail.Should().NotBeNull();
			// ReSharper disable PossibleNullReferenceException
			errorDetail.Message.Should().Be("This is a post method test exception!");
			errorDetail.Type.Should().Be(typeof(Exception).FullName);
			errorDetail.Handled.Should().BeFalse();

			//Verify the URL, method and also that the body is correctly captured
			var context = error.Context;
			context.Request.Url.Full.Should().Be("http://localhost/api/Home/PostError");
			context.Request.Method.Should().Be(HttpMethod.Post.Method);
			context.Request.Body.Should().Be(body);
			// ReSharper restore PossibleNullReferenceException
		}

		public override void Dispose()
		{
			_agent?.Dispose();
			_factory?.Dispose();
			_client?.Dispose();

			base.Dispose();
		}
	}
}
