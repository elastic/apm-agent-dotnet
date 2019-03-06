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
		private readonly Service _service;

		public AspNetCoreMiddlewareTests(WebApplicationFactory<Startup> factory)
		{
			_factory = factory;
			_service = ApmMiddlewareExtension.GetService(new TestAgentConfigurationReader(new TestLogger()));
			//The agent is instantiated with ApmMiddlewareExtension.GetService, so we can also test the calculation of the service instance.
			//(e.g. ASP.NET Core version)
			_agent = new ApmAgent(
				new TestAgentComponents(service: _service));
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

			Assert.Single(_capturedPayload.Transactions);

			//test payload
			Assert.Equal(Assembly.GetEntryAssembly()?.GetName()?.Name, _service.Name);
			Assert.Equal(Consts.AgentName, _service.Agent.Name);
			Assert.Equal(Assembly.Load("Elastic.Apm").GetName().Version.ToString(), _service.Agent.Version);
			Assembly.CreateQualifiedName("ASP.NET Core", _service.Framework.Name);
			Assembly.CreateQualifiedName(Assembly.Load("Microsoft.AspNetCore").GetName().Version.ToString(), _service.Framework.Version);

			var transaction = _capturedPayload.FirstTransaction;

			//test transaction
			Assert.Equal($"{response.RequestMessage.Method} {response.RequestMessage.RequestUri.AbsolutePath}", transaction.Name);
			Assert.Equal("HTTP 2xx", transaction.Result);
			Assert.True(transaction.Duration > 0);
			Assert.Equal("request", transaction.Type);
			Assert.False(string.IsNullOrEmpty(transaction.Id));

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

			var transaction = _capturedPayload.FirstTransaction;
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

			Assert.Single(_capturedPayload.Transactions);

			Assert.NotEmpty(_capturedPayload.Errors);
			Assert.Single(_capturedPayload.Errors);

			//also make sure the tag is captured
			Assert.Equal(_capturedPayload.FirstError.Context.Tags["foo"], "bar");
		}

		public void Dispose()
		{
			_agent?.Dispose();
			_factory?.Dispose();
			_client?.Dispose();
		}
	}
}
