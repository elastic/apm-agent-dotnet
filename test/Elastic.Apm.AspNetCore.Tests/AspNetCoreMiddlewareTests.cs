using System;
using System.Reflection;
using System.Threading.Tasks;
using Elastic.Apm.Tests;
using Elastic.Apm.Tests.Mocks;
using Microsoft.AspNetCore.Mvc.Testing;
using SampleAspNetCoreApp;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	public class AspNetCoreMiddlewareTests
		: IClassFixture<WebApplicationFactory<Startup>>
	{
		private readonly WebApplicationFactory<Startup> _factory;

		public AspNetCoreMiddlewareTests(WebApplicationFactory<Startup> factory) => _factory = factory;

		/// <summary>
		/// Simulates and HTTP GET call to /home/about and asserts on what the agent should send to the server
		/// </summary>
		[Theory]
		[InlineData("/Home/About")]
		public async Task HomeAboutTransactionTest(string url)
		{
			var agent = new ApmAgent(new TestAgentComponents());
			var capturedPayload = agent.PayloadSender as MockPayloadSender;
			var client = Helper.GetClient(agent, _factory);

			var response = await client.GetAsync(url);

			Assert.Single(capturedPayload.Payloads);
			Assert.Single(capturedPayload.Payloads[0].Transactions);

			var payload = capturedPayload.Payloads[0];

			//test payload
			Assert.Equal(Assembly.GetEntryAssembly()?.GetName()?.Name, payload.Service.Name);
			Assert.Equal(Consts.AgentName, payload.Service.Agent.Name);
			Assert.Equal(Consts.AgentVersion, payload.Service.Agent.Version);

			var transaction = capturedPayload.Payloads[0].Transactions[0];

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
		/// Configures an ASP.NET Core application without an error page.
		/// With other words: there is no error page with an exception handler configured in the ASP.NET Core pipeline.
		/// Makes sure that we still capture the failed request.
		/// </summary>
		[Fact]
		public async Task FailingRequestWithoutConfiguredExceptionPage()
		{
			var agent = new ApmAgent(new TestAgentComponents());
			var capturedPayload = agent.PayloadSender as MockPayloadSender;
			var client = Helper.GetClientWithoutExceptionPage(agent, _factory);

			await Assert.ThrowsAsync<Exception>(async () => { await client.GetAsync("Home/TriggerError"); });

			Assert.Single(capturedPayload.Payloads);
			Assert.Single(capturedPayload.Payloads[0].Transactions);

			Assert.Single(capturedPayload.Errors);
			Assert.Single(capturedPayload.Errors[0].Errors);
		}
	}
}
