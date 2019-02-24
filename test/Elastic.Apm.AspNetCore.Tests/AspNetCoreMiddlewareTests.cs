using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Tests.Mocks;
using Microsoft.AspNetCore.Mvc.Testing;
using SampleAspNetCoreApp;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Uses the samples/SampleAspNetCoreApp as the test application and tests the agent with it.
	/// It's basically an integration test.
	/// </summary>
	[Collection("DiagnosticListenerTest")] //To avoid tests from DiagnosticListenerTests running in parallel with this we add them to 1 collection.
	public class AspNetCoreMiddlewareTests
		: IClassFixture<WebApplicationFactory<Startup>>, IDisposable
	{
		private readonly ApmAgent _agent;
		private readonly MockPayloadSender _capturedPayload;
		private readonly WebApplicationFactory<Startup> _factory;

		public AspNetCoreMiddlewareTests(WebApplicationFactory<Startup> factory)
		{
			_factory = factory;
			//The agent is instantiated with ApmMiddlewareExtension.GetService, so we can also test the calculation of the service instance.
			//(e.g. ASP.NET Core version)
			_agent = new ApmAgent(
				new TestAgentComponents(service: ApmMiddlewareExtension.GetService(new TestAgentConfigurationReader(new TestLogger()))));
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
			var response = await _client.GetAsync("/Home/SimplePage");

			Assert.Single(_capturedPayload.Payloads);
			Assert.Single(_capturedPayload.Payloads[0].Transactions);

			var payload = _capturedPayload.Payloads[0];

			//test payload
			Assert.Equal(Assembly.GetEntryAssembly()?.GetName()?.Name, payload.Service.Name);
			Assert.Equal(Consts.AgentName, payload.Service.Agent.Name);
			Assert.Equal(Assembly.Load("Elastic.Apm").GetName().Version.ToString(), payload.Service.Agent.Version);
			Assembly.CreateQualifiedName("ASP.NET Core", payload.Service.Framework.Name);
			Assembly.CreateQualifiedName(Assembly.Load("Microsoft.AspNetCore").GetName().Version.ToString(), payload.Service.Framework.Version);

			var transaction = _capturedPayload.FirstTransaction;

			//test transaction
			Assert.Equal($"{response.RequestMessage.Method} {response.RequestMessage.RequestUri.AbsolutePath}", transaction.Name);
			Assert.Equal("HTTP 2xx", transaction.Result);
			Assert.True(transaction.Duration > 0);
			Assert.Equal("request", transaction.Type);
			Assert.True(transaction.Id != Guid.Empty);

			var request = transaction.Context.Request.Value;

			//test transaction.context.response
			Assert.Equal(200, transaction.Context.Response.Value.StatusCode);

			//test transaction.context.request
			Assert.Equal("2.0", request.HttpVersion);
			Assert.Equal("GET", request.Method);

			//test transaction.context.request.url
			Assert.Equal(response.RequestMessage.RequestUri.AbsolutePath, request.Url.Full);
			Assert.Equal("localhost", request.Url.HostName);
			Assert.Equal("HTTP", request.Url.Protocol);

			//test transaction.context.request.encrypted
			Assert.False(request.Socket.Encrypted);
		}

		/// <summary>
		/// Simulates an HTTP GET call to /home/index and asserts that the agent captures spans.
		/// Prerequisite: The /home/index has to generate spans (which should be the case).
		/// </summary>
		[Fact]
		public async Task HomeIndexSpanTest()
		{
			var response = await _client.GetAsync("/Home/Index");
			Assert.True(response.IsSuccessStatusCode);

			var transaction = _capturedPayload.Payloads[0].Transactions[0];
			Assert.NotEmpty(_capturedPayload.SpansOnFirstTransaction);

			//one of the spans is a DB call:
			Assert.Contains(_capturedPayload.SpansOnFirstTransaction, n => n.Context.Db != null);
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
			await Assert.ThrowsAsync<Exception>(async () => { await _client.GetAsync("Home/TriggerError"); });

			Assert.Single(_capturedPayload.Payloads);
			Assert.Single(_capturedPayload.Payloads[0].Transactions);

			Assert.NotEmpty(_capturedPayload.Errors);
			Assert.Single(_capturedPayload.Errors[0].Errors);

			//also make sure the tag is captured
			Assert.Equal(((_capturedPayload.Errors[0] as Error)?.Errors[0] as Error.ErrorDetail)?.Context.Tags["foo"], "bar");
		}

		public void Dispose()
		{
			_agent?.Dispose();
			_factory?.Dispose();
			_client?.Dispose();
		}
	}
}
