using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.AspNetCore.Tests.Factories;
using Elastic.Apm.AspNetCore.Tests.Fakes;
using Elastic.Apm.AspNetCore.Tests.Helpers;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Mocks;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Uses the samples/AspNetCoreSampleApp as the test application and tests the agent with it.
	/// It's basically an integration test.
	/// </summary>
	[Collection("DiagnosticListenerTest")] // To avoid tests from DiagnosticListenerTests running in parallel with this we add them to 1 collection.
	public class AspNetCoreMiddlewareTests : IClassFixture<CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup>>, IDisposable
	{
		private readonly CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup> _factory;
		private readonly IApmLogger _logger;

		public AspNetCoreMiddlewareTests(CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup> factory, ITestOutputHelper testOutputHelper)
		{
			_factory = factory;
			_logger = new XunitOutputLogger(testOutputHelper).Scoped(nameof(AspNetCoreMiddlewareTests));
		}

		/// <summary>
		/// Simulates an HTTP GET call to /home/simplePage and asserts on what the agent should send to the server
		/// </summary>
		[Fact]
		public async Task HomeSimplePageTransactionTest()
		{
			const string headerKey = "X-Additional-Header";
			const string headerValue = "For-Elastic-Apm-Agent";

			using (var agent = GetAgent())
			using (var client = TestHelper.GetClient(_factory, agent))
			{
				client.DefaultRequestHeaders.Add(headerKey, headerValue);
				using (var response = await client.GetAsync("/Home/SimplePage"))
				{
					client.DefaultRequestHeaders.Add(headerKey, headerValue);

					var capturedPayload = (MockPayloadSender)agent.PayloadSender;

					// Test service.
					capturedPayload.Transactions.Should().ContainSingle();

					agent.Service.Name.Should().NotBeNullOrWhiteSpace().And.NotBe(ConfigConsts.DefaultValues.UnknownServiceName);

					agent.Service.Agent.Name.Should().Be(Apm.Consts.AgentName);
					agent.Service.Agent.Version.Should().Be(typeof(Agent).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);

					agent.Service.Framework.Name.Should().Be("ASP.NET Core");
					agent.Service.Framework.Version.Should().Be(Assembly.Load("Microsoft.AspNetCore").GetName().Version.ToString());

					agent.Service.Runtime.Name.Should().Be(Runtime.DotNetCoreName);
					agent.Service.Runtime.Version.Should().Be(Directory.GetParent(typeof(object).Assembly.Location).Name);

					capturedPayload.Transactions.Should().ContainSingle();
					var transaction = capturedPayload.FirstTransaction;
					transaction.Name.Should().Be($"{response.RequestMessage.Method} Home/SimplePage");
					transaction.Result.Should().Be("HTTP 2xx");
					transaction.Duration.Should().BePositive();

					transaction.Type.Should().Be("request");
					transaction.Id.Should().NotBeEmpty();

					// Test transaction.context.response
					transaction.Context.Response.StatusCode.Should().Be(200);
					if (agent.ConfigurationReader.CaptureHeaders)
					{
						transaction.Context.Response.Headers.Should().NotBeNull();
						transaction.Context.Response.Headers.Should().NotBeEmpty();

						transaction.Context.Response.Headers.Should().ContainKeys(headerKey);
						transaction.Context.Response.Headers[headerKey].Should().Be(headerValue);
					}

					// Test transaction.Context.Request.
					transaction.Context.Request.HttpVersion.Should().Be("2.0");
					transaction.Context.Request.Method.Should().Be("GET");

					// Test transaction.Context.Request.Url.
					transaction.Context.Request.Url.Full.Should().Be(response.RequestMessage.RequestUri.AbsoluteUri);
					transaction.Context.Request.Url.HostName.Should().Be("localhost");
					transaction.Context.Request.Url.Protocol.Should().Be("HTTP");

					if (agent.ConfigurationReader.CaptureHeaders)
					{
						transaction.Context.Request.Headers.Should().NotBeNull();
						transaction.Context.Request.Headers.Should().NotBeEmpty();

						transaction.Context.Request.Headers.Should().ContainKeys(headerKey);
						transaction.Context.Request.Headers[headerKey].Should().Be(headerValue);
					}

					// Test transaction.Context.Request.Socket.Encrypted.
					transaction.Context.Request.Socket.Encrypted.Should().BeFalse();
				}
			}
		}

		/// <summary>
		/// Simulates an HTTP POST call to /home/simplePage and asserts on what the agent should send to the server
		/// to test the 'CaptureBody' configuration option
		/// </summary>
		[Fact]
		public async Task HomeSimplePagePostTransactionTest()
		{
			const string body = "{\"id\" : \"1\"}";

			using (var agent = GetAgent())
			using (var client = TestHelper.GetClient(_factory, agent))
			{
				const string headerKey = "X-Additional-Header";
				const string headerValue = "For-Elastic-Apm-Agent";
				client.DefaultRequestHeaders.Add(headerKey, headerValue);
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

				using (var response = await client.PostAsync("api/Home/Post", new StringContent(body, Encoding.UTF8, "application/json")))
				{
					var capturedPayload = (MockPayloadSender)agent.PayloadSender;
					capturedPayload.Transactions.Should().ContainSingle();

					agent.Service.Name.Should().NotBeNullOrWhiteSpace().And.NotBe(ConfigConsts.DefaultValues.UnknownServiceName);

					agent.Service.Agent.Name.Should().Be(Apm.Consts.AgentName);
					var apmVersion = typeof(Agent).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
					agent.Service.Agent.Version.Should().Be(apmVersion);

					agent.Service.Framework.Name.Should().Be("ASP.NET Core");

					agent.Service.Framework.Version.Should().Be(Assembly.Load("Microsoft.AspNetCore").GetName().Version.ToString());

					agent.Service.Runtime.Name.Should().Be(Runtime.DotNetCoreName);
					agent.Service.Runtime.Version.Should().Be(Directory.GetParent(typeof(object).Assembly.Location).Name);

					capturedPayload.Transactions.Should().ContainSingle();
					var transaction = capturedPayload.FirstTransaction;
					transaction.Name.Should().Be("POST Home/Post");
					transaction.Result.Should().Be("HTTP 2xx");
					transaction.Duration.Should().BeGreaterThan(0);

					transaction.Type.Should().Be("request");
					transaction.Id.Should().NotBeEmpty();

					// Test transaction.Context.Response.
					transaction.Context.Response.StatusCode.Should().Be(200);
					if (agent.ConfigurationReader.CaptureHeaders)
					{
						transaction.Context.Response.Headers.Should().NotBeNull();
						transaction.Context.Response.Headers.Should().NotBeEmpty();
					}

					if (agent.ConfigurationReader.CaptureBody != "off")
					{
						transaction.Context.Request.Body.Should().NotBeNull();
						transaction.Context.Request.Body.Should().Be(body);
					}

					// Test transaction.Context.Request.
					transaction.Context.Request.HttpVersion.Should().Be("2.0");
					transaction.Context.Request.Method.Should().Be("POST");

					// Test transaction.Context.Request.Url.
					transaction.Context.Request.Url.Full.Should().Be(response.RequestMessage.RequestUri.AbsoluteUri);
					transaction.Context.Request.Url.HostName.Should().Be("localhost");
					transaction.Context.Request.Url.Protocol.Should().Be("HTTP");

					if (agent.ConfigurationReader.CaptureHeaders)
					{
						transaction.Context.Request.Headers.Should().NotBeNull();
						transaction.Context.Request.Headers.Should().NotBeEmpty();

						transaction.Context.Request.Headers.Should().ContainKeys(headerKey);
						transaction.Context.Request.Headers[headerKey].Should().Be(headerValue);
					}

					// Test transaction.Context.Request.Socket.Encrypted.
					transaction.Context.Request.Socket.Encrypted.Should().BeFalse();
				}
			}
		}

		/// <summary>
		/// Simulates an HTTP GET call to /home/index and asserts that the agent captures spans.
		/// Prerequisite: The /home/index has to generate spans (which should be the case).
		/// </summary>
		[Fact]
		public async Task HomeIndexSpanTest()
		{
			using (var agent = GetAgent())
			using (var client = TestHelper.GetClient(_factory, agent))
			using (var response = await client.GetAsync("/Home/Index"))
			{
				response.IsSuccessStatusCode.Should().BeTrue();

				((MockPayloadSender)agent.PayloadSender).SpansOnFirstTransaction.Should().NotBeEmpty().And.Contain(n => n.Context.Db != null);
			}
		}

		/// <summary>
		/// Simulates an HTTP GET call to /Home/Index?captureControllerActionAsSpan=true
		/// and asserts that all automatically captured spans are children of the span for controller's action.
		/// </summary>
		[Fact]
		public async Task HomeIndexAutoCapturedSpansAreChildrenOfControllerActionAsSpan()
		{
			using (var agent = GetAgent())
			using (var client = TestHelper.GetClient(_factory, agent))
			using (var response = await client.GetAsync("/Home/Index?captureControllerActionAsSpan=true"))
			{
				var capturedPayload = (MockPayloadSender)agent.PayloadSender;

				response.IsSuccessStatusCode.Should().BeTrue();
				var spans = capturedPayload.SpansOnFirstTransaction;
				spans.Should().NotBeEmpty();
				var controllerActionSpan = spans.Last();
				controllerActionSpan.Name.Should().Be("Index_span_name");
				controllerActionSpan.Type.Should().Be("Index_span_type");
				var httpSpans = spans.Where(span => span.Context.Db != null).ToArray();
				httpSpans.Should().NotBeEmpty();
				foreach (var httpSpan in httpSpans) httpSpan.ParentId.Should().Be(controllerActionSpan.Id);
				var dbSpans = spans.Where(span => span.Context.Http != null).ToArray();
				dbSpans.Should().NotBeEmpty();
				foreach (var dbSpan in dbSpans) dbSpan.ParentId.Should().Be(controllerActionSpan.Id);
			}
		}

		/// <summary>
		/// Configures an ASP.NET Core application without an error page.
		/// In other words, there is no error page with an exception handler configured in the ASP.NET Core pipeline.
		/// Makes sure that we still capture the failed request.
		/// </summary>
		[Fact]
		public async Task FailingRequestWithoutConfiguredExceptionPage()
		{
			using (var agent = GetAgent())
			{
				using (var client = TestHelper.GetClientWithoutDeveloperExceptionPage(_factory, agent))
				{
					var localClient = client;
					Func<Task> act = async () => await localClient.GetAsync("Home/TriggerError");
					await act.Should().ThrowAsync<Exception>();
				}

				var capturedPayload = (MockPayloadSender)agent.PayloadSender;

				capturedPayload.Transactions.Should().ContainSingle();
				capturedPayload.Errors.Should().NotBeEmpty();
				capturedPayload.Errors.Should().ContainSingle();

				// Also make sure the label is captured.
				var error = capturedPayload.Errors[0] as Error;
				error.Should().NotBeNull();

				error?.Exception.Should().NotBeNull();
				error?.Context.Labels.Should().NotBeEmpty().And.ContainKey("foo").And.Contain("foo", "bar");
			}
		}

		/// <summary>
		/// Configures an ASP.NET Core application without an error page.
		/// With other words: there is no error page with an exception handler configured in the ASP.NET Core pipeline.
		/// Makes sure that we still capture the failed request along with the request body
		/// </summary>
		[Fact]
		public async Task FailingPostRequestWithoutConfiguredExceptionPage()
		{
			using (var agent = GetAgent())
			using (var client = TestHelper.GetClient(_factory, agent, false))
			{
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

				const string body = "{\"id\" : \"1\"}";
				Func<Task> act = async () => await client.PostAsync("api/Home/PostError", new StringContent(body, Encoding.UTF8, "application/json"));

				await act.Should().ThrowAsync<Exception>();

				var capturedPayload = (MockPayloadSender)agent.PayloadSender;
				capturedPayload.Transactions.Should().ContainSingle();
				capturedPayload.Errors.Should().NotBeEmpty();
				capturedPayload.Errors.Should().ContainSingle();

				var error = capturedPayload.Errors.Single() as Error;
				error.Should().NotBeNull();

				var errorDetail = error?.Exception;
				errorDetail.Should().NotBeNull();
				// ReSharper disable once PossibleNullReferenceException
				errorDetail.Message.Should().Be("This is a post method test exception!");
				errorDetail.Type.Should().Be(typeof(Exception).FullName);
				errorDetail.Handled.Should().BeFalse();

				// Verify the URL, method and also that the body is correctly captured.
				// ReSharper disable once PossibleNullReferenceException
				var context = error.Context;
				context.Request.Url.Full.Should().Be("http://localhost/api/Home/PostError");
				context.Request.Method.Should().Be(HttpMethod.Post.Method);
				context.Request.Body.Should().Be(body);
			}
		}

		private ApmAgent GetAgent()
		{
			// We need to ensure Agent.Instance is created because we need the ApmAgent to use TracerCurrentExecutionSegmentsContainer.
			var agent = new ApmAgent(new TestAgentComponents(
				_logger,
				captureBody: ConfigConsts.SupportedValues.CaptureBodyAll,
				// The ApmAgent needs to share CurrentExecutionSegmentsContainer with Agent.Instance because the sample application
				// used by the tests (SampleAspNetCoreApp) uses Agent.Instance.Tracer.CurrentTransaction/CurrentSpan.
				currentExecutionSegmentsContainer: Agent.Instance.TracerInternal.CurrentExecutionSegmentsContainer));

			ApmMiddlewareExtension.UpdateServiceInformation(agent.Service);
			return agent;
		}

		public void Dispose() => _factory.Dispose();
	}
}
