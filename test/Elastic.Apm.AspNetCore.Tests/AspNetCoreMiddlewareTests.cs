using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Elastic.Apm.AspNetCore.Tests.Factories;
using Elastic.Apm.AspNetCore.Tests.Fakes;
using Elastic.Apm.AspNetCore.Tests.Helpers;
using Elastic.Apm.Config;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

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

		public AspNetCoreMiddlewareTests(CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup> factory) => _factory = factory;

		/// <summary>
		/// Simulates an HTTP GET call to /home/simplePage and asserts on what the agent should send to the server
		/// </summary>
		[Fact]
		public async Task HomeSimplePageTransactionTest()
		{
			const string headerKey = "X-Additional-Header";
			const string headerValue = "For-Elastic-Apm-Agent";

			using (var agent = GetAgent())
			{
				HttpResponseMessage response;
				using (var client = TestHelper.GetClient(_factory, agent))
				{
					client.DefaultRequestHeaders.Add(headerKey, headerValue);
					response = await client.GetAsync("/Home/SimplePage");
				}

				var capturedPayload = (MockPayloadSender)agent.PayloadSender;

				// Test service.
				capturedPayload.Transactions.Should().ContainSingle();

				agent.Service.Name.Should().NotBeNullOrWhiteSpace().And.NotBe(ConfigConsts.DefaultValues.UnknownServiceName);

				agent.Service.Agent.Name.Should().Be(Apm.Consts.AgentName);
				agent.Service.Agent.Version.Should().Be(typeof(Agent).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);

				agent.Service.Framework.Name.Should().Be("ASP.NET Core");

				var aspNetCoreVersion = Assembly.Load("Microsoft.AspNetCore").GetName().Version.ToString();
				agent.Service.Framework.Version.Should().Be(aspNetCoreVersion);

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

				// Test transaction.context.request
				transaction.Context.Request.HttpVersion.Should().Be("2.0");
				transaction.Context.Request.Method.Should().Be("GET");

				// Test transaction.context.request.url
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

				// Test transaction.context.request.encrypted
				transaction.Context.Request.Socket.Encrypted.Should().BeFalse();
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
			{
				var response = await client.GetAsync("/Home/Index");
				response.IsSuccessStatusCode.Should().BeTrue();

				((MockPayloadSender)agent.PayloadSender).SpansOnFirstTransaction.Should().NotBeEmpty().And.Contain(n => n.Context.Db != null);
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
					Func<Task> act = async () => await client.GetAsync("Home/TriggerError");
					await act.Should().ThrowAsync<Exception>();
				}

				var capturedPayload = (MockPayloadSender)agent.PayloadSender;

				capturedPayload.Transactions.Should().ContainSingle();
				capturedPayload.Errors.Should().NotBeEmpty();
				capturedPayload.Errors.Should().ContainSingle();

				// Also make sure the tag is captured.
				var error = capturedPayload.Errors.Single() as Error;
				error.Should().NotBeNull();

				error.Exception.Should().NotBeNull();
				error.Context.Tags.Should().NotBeEmpty().And.ContainKey("foo").And.Contain("foo", "bar");
			}
		}

		private static ApmAgent GetAgent()
		{
			var agent = new ApmAgent(new TestAgentComponents(new TestAgentConfigurationReader(new TestLogger())));
			ApmMiddlewareExtension.UpdateServiceInformation(agent.Service);
			return agent;
		}

		public void Dispose() => _factory.Dispose();
	}
}
