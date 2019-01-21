using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
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
	public class AspNetCoreMiddlewareTests
		: IClassFixture<WebApplicationFactory<Startup>>
	{
		private readonly WebApplicationFactory<Startup> _factory;
		private HttpClient _client;
		private readonly MockPayloadSender _capturedPayload;
		private readonly ApmAgent _agent;

		public AspNetCoreMiddlewareTests(WebApplicationFactory<Startup> factory)
		{
			_factory = factory;
			_agent = new ApmAgent(new TestAgentComponents());
			_capturedPayload = _agent.PayloadSender as MockPayloadSender;
			_client = Helper.GetClient(_agent, _factory);
		}

		/// <summary>
		/// Simulates an HTTP GET call to /home/about and asserts on what the agent should send to the server
		/// </summary>
		[Fact]
		public async Task HomeAboutTransactionTest()
		{
			var response = await _client.GetAsync("/Home/About");

			Assert.Single(_capturedPayload.Payloads);
			Assert.Single(_capturedPayload.Payloads[0].Transactions);

			var payload = _capturedPayload.Payloads[0];

			//test payload
			Assert.Equal(Assembly.GetEntryAssembly()?.GetName()?.Name, payload.Service.Name);
			Assert.Equal(Consts.AgentName, payload.Service.Agent.Name);
			Assert.Equal(Consts.AgentVersion, payload.Service.Agent.Version);

			var transaction = _capturedPayload.Payloads[0].Transactions[0];

			//test transaction
			Assert.Equal($"{response.RequestMessage.Method} {response.RequestMessage.RequestUri.AbsolutePath}", transaction.Name);
			Assert.Equal("HTTP 2xx", transaction.Result);
			Assert.True(transaction.Duration > 0);
			Assert.Equal("request", transaction.Type);
			Assert.True(transaction.Id != Guid.Empty);

			//test transaction.context.response
			Assert.Equal(200, transaction.Context.Response.StatusCode);

			//test transaction.context.request
			Assert.Equal("2.0", transaction.Context.Request.HttpVersion);
			Assert.Equal("GET", transaction.Context.Request.Method);

			//test transaction.context.request.url
			Assert.Equal(response.RequestMessage.RequestUri.AbsolutePath, transaction.Context.Request.Url.Full);
			Assert.Equal("localhost", transaction.Context.Request.Url.HostName);
			Assert.Equal("HTTP", transaction.Context.Request.Url.Protocol);

			//test transaction.context.request.encrypted
			Assert.False(transaction.Context.Request.Socket.Encrypted);
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
			Assert.NotEmpty(transaction.Spans);

			//one of the spans is a DB call:
			Assert.Contains(transaction.Spans, n => n.Context.Db != null);
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
		}
	}
}
