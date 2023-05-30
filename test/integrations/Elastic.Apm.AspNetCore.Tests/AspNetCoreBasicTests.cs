// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.Config;
using Elastic.Apm.Extensions.Hosting;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.XUnit;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using SampleAspNetCoreApp;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Uses the samples/SampleAspNetCoreApp as the test application and tests the agent with it.
	/// It's basically an integration test.
	/// Originally called `AspNetCoreMiddlewareTests`
	/// It runs all tests wit both the ApmMiddleware and with AspNetCorePageLoadDiagnosticSource
	/// </summary>
	[Collection("DiagnosticListenerTest")] //To avoid tests from DiagnosticListenerTests running in parallel with this we add them to 1 collection.
	public class AspNetCoreBasicTests : LoggingTestBase, IClassFixture<WebApplicationFactory<Startup>>
	{
		private readonly MockPayloadSender _capturedPayload;
		private readonly WebApplicationFactory<Startup> _factory;

		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		private readonly IApmLogger _logger;

		public AspNetCoreBasicTests(WebApplicationFactory<Startup> factory, ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper)
		{
			_logger = LoggerBase.Scoped(nameof(AspNetCoreBasicTests));
			_factory = factory;
			_capturedPayload = new MockPayloadSender();
			_agent = new ApmAgent(new TestAgentComponents(
				_logger,
				new MockConfiguration(_logger, captureBody: ConfigConsts.SupportedValues.CaptureBodyAll, exitSpanMinDuration: "0"),
				_capturedPayload,
				// _agent needs to share CurrentExecutionSegmentsContainer with Agent.Instance
				// because the sample application used by the tests (SampleAspNetCoreApp) uses Agent.Instance.Tracer.CurrentTransaction/CurrentSpan
				Agent.Instance.TracerInternal.CurrentExecutionSegmentsContainer)
			);

			HostBuilderExtensions.UpdateServiceInformation(_agent.Service);
		}

		private ApmAgent _agent;

		private HttpClient _client;

		/// <summary>
		/// Simulates an HTTP GET call to /home/simplePage and asserts on what the agent should send to the server
		/// </summary>
		[Theory]
		[MemberData(nameof(MemberData.TestWithDiagnosticSourceOnly), MemberType = typeof(MemberData))]
		public async Task HomeSimplePageTransactionTest(bool withDiagnosticSourceOnly)
		{
			_client = Helper.ConfigureHttpClient(true, withDiagnosticSourceOnly, _agent, _factory);

			var headerKey = "X-Additional-Header";
			var headerValue = "For-Elastic-Apm-Agent";
			_client.DefaultRequestHeaders.Add(headerKey, headerValue);
			var response = await _client.GetAsync("/Home/SimplePage");

			//test service
			_capturedPayload.WaitForTransactions();
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
#if NET5_0
			_agent.Service.Runtime.Name.Should().Be(Runtime.DotNetName + " 5");
#elif NET6_0
			_agent.Service.Runtime.Name.Should().Be(Runtime.DotNetName + " 6");
#elif NET7_0
			_agent.Service.Runtime.Name.Should().Be(Runtime.DotNetName + " 7");
#else
			_agent.Service.Runtime.Name.Should().Be(Runtime.DotNetCoreName);
#endif
			_agent.Service.Runtime.Version.Should().StartWith(Directory.GetParent(typeof(object).Assembly.Location).Name);

			var transaction = _capturedPayload.FirstTransaction;
			var transactionName = $"{response.RequestMessage.Method} Home/SimplePage";
			transaction.Name.Should().Be(transactionName);
			transaction.Result.Should().Be("HTTP 2xx");
			transaction.Duration.Should().BeGreaterThan(0);
			transaction.Outcome.Should().Be(Outcome.Success);

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
#if NET5_0_OR_GREATER
			transaction.Context.Request.HttpVersion.Should().Be("1.1");
#elif NETCOREAPP3_0 || NETCOREAPP3_1
			transaction.Context.Request.HttpVersion.Should().Be("2");
#else
			transaction.Context.Request.HttpVersion.Should().Be("2.0");
#endif
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
		}

		/// <summary>
		/// Creates an agent with Enabled=false and makes sure the agent does not capture anything.
		/// </summary>
		/// <param name="withDiagnosticSourceOnly"></param>
		/// <returns></returns>
		[Theory]
		[MemberData(nameof(MemberData.TestWithDiagnosticSourceOnly), MemberType = typeof(MemberData))]
		public async Task HomeIndexTransactionWithEnabledFalse(bool withDiagnosticSourceOnly)
		{
			_agent = new ApmAgent(new TestAgentComponents(
				_logger,
				new MockConfiguration(_logger, enabled: "false", exitSpanMinDuration:"0"), _capturedPayload));

			_client = Helper.ConfigureHttpClient(true, withDiagnosticSourceOnly, _agent, _factory);

			var response = await _client.GetAsync("/Home/Index");

			response.IsSuccessStatusCode.Should().BeTrue();

			_capturedPayload.WaitForAny(TimeSpan.FromSeconds(5));
			_capturedPayload.Transactions.Should().BeNullOrEmpty();
			_capturedPayload.Spans.Should().BeNullOrEmpty();
			_capturedPayload.Errors.Should().BeNullOrEmpty();
		}

		[Theory]
		[MemberData(nameof(MemberData.TestWithDiagnosticSourceOnly), MemberType = typeof(MemberData))]
		public async Task HomeIndexTransactionWithToggleRecording(bool withDiagnosticSourceOnly)
		{
			_agent = new ApmAgent(new TestAgentComponents(
				_logger, new MockConfiguration(recording: "false", exitSpanMinDuration: "0"), _capturedPayload));

			_client = Helper.ConfigureHttpClient(true, withDiagnosticSourceOnly, _agent, _factory);

			var response = await _client.GetAsync("/Home/Index");

			response.IsSuccessStatusCode.Should().BeTrue();

			_capturedPayload.WaitForAny(TimeSpan.FromSeconds(5));

			_capturedPayload.Transactions.Should().BeNullOrEmpty();
			_capturedPayload.Spans.Should().BeNullOrEmpty();
			_capturedPayload.Errors.Should().BeNullOrEmpty();

			//flip recording to true
			_agent.ConfigurationStore.CurrentSnapshot = new MockConfiguration(recording: "true");

			response = await _client.GetAsync("/Home/Index");
			response.IsSuccessStatusCode.Should().BeTrue();

			_capturedPayload.WaitForTransactions();
			_capturedPayload.Transactions.Should().NotBeEmpty();

			_capturedPayload.WaitForSpans();
			_capturedPayload.Spans.Should().NotBeEmpty();

			_capturedPayload.WaitForErrors(TimeSpan.FromSeconds(5));
			_capturedPayload.Errors.Should().BeNullOrEmpty();
		}

		/// <summary>
		/// Simulates an HTTP POST call to /home/simplePage and asserts on what the agent should send to the server
		/// to test the 'CaptureBody' configuration option
		/// </summary>
		[Theory]
		[MemberData(nameof(MemberData.TestWithDiagnosticSourceOnly), MemberType = typeof(MemberData))]
		public async Task HomeSimplePagePostTransactionTest(bool withDiagnosticSourceOnly)
		{
			_client = Helper.ConfigureHttpClient(true, withDiagnosticSourceOnly, _agent, _factory);
			var headerKey = "X-Additional-Header";
			var headerValue = "For-Elastic-Apm-Agent";
			_client.DefaultRequestHeaders.Add(headerKey, headerValue);
			_client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			var body = "{\"id\" : \"1\"}";
			var response = await _client.PostAsync("api/Home/Post", new StringContent(body, Encoding.UTF8, "application/json"));

			_capturedPayload.WaitForTransactions();

			_agent.Service.Name.Should()
				.NotBeNullOrWhiteSpace()
				.And.NotBe(ConfigConsts.DefaultValues.UnknownServiceName);

			_agent.Service.Agent.Name.Should().Be(Apm.Consts.AgentName);
			var apmVersion = typeof(Agent).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
			_agent.Service.Agent.Version.Should().Be(apmVersion);

			_agent.Service.Framework.Name.Should().Be("ASP.NET Core");

			// test major.minor values only
			var aspNetCoreVersion = Assembly.Load("Microsoft.AspNetCore").GetName().Version.ToString(2);
			_agent.Service.Framework.Version.Should().StartWith(aspNetCoreVersion);

#if NET5_0
			_agent.Service.Runtime.Name.Should().Be(Runtime.DotNetName + " 5");
#elif NET6_0
			_agent.Service.Runtime.Name.Should().Be(Runtime.DotNetName + " 6");
#elif NET7_0
			_agent.Service.Runtime.Name.Should().Be(Runtime.DotNetName + " 7");
#else
			_agent.Service.Runtime.Name.Should().Be(Runtime.DotNetCoreName);
#endif

			_agent.Service.Runtime.Version.Should().StartWith(Directory.GetParent(typeof(object).Assembly.Location).Name);


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
#if NET5_0_OR_GREATER
			transaction.Context.Request.HttpVersion.Should().Be("1.1");
#elif NETCOREAPP3_0 || NETCOREAPP3_1
			transaction.Context.Request.HttpVersion.Should().Be("2");
#else
			transaction.Context.Request.HttpVersion.Should().Be("2.0");
#endif
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
		}

		/// <summary>
		/// Simulates an HTTP GET call to /home/index and asserts that the agent captures spans.
		/// Prerequisite: The /home/index has to generate spans (which should be the case).
		/// </summary>
		[Theory]
		[MemberData(nameof(MemberData.TestWithDiagnosticSourceOnly), MemberType = typeof(MemberData))]
		public async Task HomeIndexSpanTest(bool withDiagnosticSourceOnly)
		{
			_client = Helper.ConfigureHttpClient(true, withDiagnosticSourceOnly, _agent, _factory);
			var response = await _client.GetAsync("/Home/Index");

			response.IsSuccessStatusCode.Should().BeTrue();

			_capturedPayload.WaitForSpans(count: 5);
			_capturedPayload.WaitForTransactions();
			_capturedPayload.SpansOnFirstTransaction.Should().NotBeEmpty().And.Contain(n => n.Context.Db != null);
		}

		/// <summary>
		/// Simulates an HTTP GET call to /home/index and asserts that the agent captures Span.Context.Destination fields.
		/// Prerequisite: The /home/index has to generate spans (which should be the case).
		/// It also assumes that /home/index makes a requrst to github.com
		/// </summary>
		[Theory]
		[MemberData(nameof(MemberData.TestWithDiagnosticSourceOnly), MemberType = typeof(MemberData))]
		public async Task HomeIndexDestinationTest(bool withDiagnosticSourceOnly)
		{
			_client = Helper.ConfigureHttpClient(true, withDiagnosticSourceOnly, _agent, _factory);
			var response = await _client.GetAsync("/Home/Index");

			response.IsSuccessStatusCode.Should().BeTrue();
			_capturedPayload.WaitForTransactions();
			_capturedPayload.WaitForSpans(count: 5);
			_capturedPayload.SpansOnFirstTransaction.Should().NotBeEmpty().And.Contain(n => n.Context.Http != null);
			_capturedPayload.SpansOnFirstTransaction.First(n => n.Context.Http != null).Context.Destination.Should().NotBeNull();
			_capturedPayload.SpansOnFirstTransaction.First(n => n.Context.Http != null).Context.Destination.Service.Should().NotBeNull();

			_capturedPayload.SpansOnFirstTransaction.First(n => n.Context.Http != null)
				.Context.Destination.Service.Resource.ToLower()
				.Should()
				.Be("api.github.com:443");
		}

		/// <summary>
		/// Simulates an HTTP GET call to /Home/Index?captureControllerActionAsSpan=true
		/// and asserts that all automatically captured spans are children of the span for controller's action.
		/// </summary>
		[Theory]
		[MemberData(nameof(MemberData.TestWithDiagnosticSourceOnly), MemberType = typeof(MemberData))]
		public async Task HomeIndexAutoCapturedSpansAreChildrenOfControllerActionAsSpan(bool withDiagnosticSourceOnly)
		{
			_client = Helper.ConfigureHttpClient(true, withDiagnosticSourceOnly, _agent, _factory);
			var response = await _client.GetAsync("/Home/Index?captureControllerActionAsSpan=true");

			response.IsSuccessStatusCode.Should().BeTrue();

			_capturedPayload.WaitForTransactions();
			_capturedPayload.WaitForSpans(count: 6);
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
				dbSpan.Context.Db.Type.Should().Be(Database.TypeSql);
				dbSpan.Context.Destination.Should().NotBeNull();
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
		[Theory]
		[MemberData(nameof(MemberData.TestWithDiagnosticSourceOnly), MemberType = typeof(MemberData))]
		public async Task FailingRequestWithoutConfiguredExceptionPage(bool withDiagnosticSourceOnly)
		{
			_client = Helper.ConfigureHttpClient(false, withDiagnosticSourceOnly, _agent, _factory);

			Func<Task> act = async () => await _client.GetAsync("Home/TriggerError");
			await act.Should().ThrowAsync<Exception>();

			_capturedPayload.WaitForTransactions();
			_capturedPayload.Transactions.Should().ContainSingle();

			_capturedPayload.WaitForErrors();
			_capturedPayload.Errors.Should().NotBeEmpty();
			_capturedPayload.Errors.Should().ContainSingle();

			//also make sure the label is captured
			var error = _capturedPayload.Errors[0] as Error;
			error.Should().NotBeNull();

			var errorDetail = error?.Exception;
			errorDetail.Should().NotBeNull();

			var labels = error?.Context.InternalLabels.Value.InnerDictionary;
			labels.Should().NotBeEmpty().And.ContainKey("foo");
			var val = labels?["foo"];
			val.Should().NotBeNull();
			if (val != null)
				labels["foo"].Value.Should().Be("bar");
		}

		/// <summary>
		/// Configures an ASP.NET Core application without an error page.
		/// With other words: there is no error page with an exception handler configured in the ASP.NET Core pipeline.
		/// Makes sure that we still capture the failed request along with the request body
		/// </summary>
		[Theory]
		[MemberData(nameof(MemberData.TestWithDiagnosticSourceOnly), MemberType = typeof(MemberData))]
		public async Task FailingPostRequestWithoutConfiguredExceptionPage(bool withDiagnosticSourceOnly)
		{
			_client = Helper.ConfigureHttpClient(false, withDiagnosticSourceOnly, _agent, _factory);

			_client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			var body = "{\"id\" : \"1\"}";
			Func<Task> act = async () => await _client.PostAsync("api/Home/PostError", new StringContent(body, Encoding.UTF8, "application/json"));

			await act.Should().ThrowAsync<Exception>();

			_capturedPayload.WaitForTransactions();
			_capturedPayload.Transactions.Should().ContainSingle();
			_capturedPayload.WaitForErrors();
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

		[Fact]
		public void AspNetCoreErrorDiagnosticsSubscriber_Should_Be_Registered_Only_Once()
		{
			var builder = _factory
				.WithWebHostBuilder(n => n.Configure(app =>
					app.UseElasticApm(_agent, _agent.Logger, new AspNetCoreErrorDiagnosticsSubscriber())));

			builder.CreateClient();

			_agent.Disposables.Should().NotBeNull();
			_agent.SubscribedListeners.Should().Contain(typeof(AspNetCoreErrorDiagnosticListener));
		}

		/// <summary>
		/// An HTTP call to an action method which manually sets <see cref="IExecutionSegment.Outcome"/>.
		/// Makes sure auto instrumentation does not overwrite the outcome.
		/// </summary>
		[Theory]
		[MemberData(nameof(MemberData.TestWithDiagnosticSourceOnly), MemberType = typeof(MemberData))]
		public async Task ManualTransactionOutcomeTest(bool withDiagnosticSourceOnly)
		{
			_client = Helper.ConfigureHttpClient(true, withDiagnosticSourceOnly, _agent, _factory);

			await _client.GetAsync("/Home/SampleWithManuallySettingOutcome");

			_capturedPayload.WaitForTransactions();
			_capturedPayload.Transactions.Should().ContainSingle();
			_capturedPayload.FirstTransaction.Outcome.Should().Be(Outcome.Failure);
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
