using System;
using System.Threading.Tasks;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SampleAspNetCoreApp;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Tests the transaction name in ASP.NET Core.
	/// Specifically: optional parameters in the route should be templateed in the Transaction.Name.
	/// E.g. url localhost/user/info/1 should get have Transaction.Name GET user/info {id}
	/// </summary>
	[Collection("DiagnosticListenerTest")]
	public class TransactionNameTests : IClassFixture<WebApplicationFactory<Startup>>, IDisposable
	{
		private readonly ApmAgent _agent;
		private readonly WebApplicationFactory<Startup> _factory;
		private readonly MockPayloadSender _payloadSender = new MockPayloadSender();

		public TransactionNameTests(WebApplicationFactory<Startup> factory)
		{
			_factory = factory;
			_agent = new ApmAgent(new TestAgentComponents(payloadSender: _payloadSender));
			ApmMiddlewareExtension.UpdateServiceInformation(_agent.Service);
		}

		/// <summary>
		/// Calls a URL that maps to a route with optional parameter (id).
		/// Makes sure the Transaction.Name contains "{id}" instead of the value.
		/// </summary>
		[Fact]
		public async Task OptionalRouteParameter()
		{
			var httpClient = Helper.GetClient(_agent, _factory);
			await httpClient.GetAsync("home/sample/3");
			await httpClient.GetAsync("home/sample/2");

			_payloadSender.Transactions.Should().OnlyContain(n => n.Name == "GET home/sample {id}");
		}

		/// <summary>
		/// Calls a URL and sets custom transaction name.
		/// Makes sure the Transaction.Name can be set to custom name.
		/// </summary>
		[Fact]
		public async Task CustomTransactionName()
		{
			var httpClient = Helper.GetClient(_agent, _factory);
			await httpClient.GetAsync($"home/TransactionWithCustomName");

			_payloadSender.Transactions.Should().OnlyContain(n => n.Name == "custom");
		}

		/// <summary>
		/// Calls a URL that would map to a route with optional parameter (id), but does not pass the id value.
		/// Makes sure no template value is captured in Transaction.Name.
		/// </summary>
		[Fact]
		public async Task OptionalRouteParameterWithNull()
		{
			var httpClient = Helper.GetClient(_agent, _factory);
			await httpClient.GetAsync("home/sample");

			_payloadSender.Transactions.Should().OnlyContain(n => n.Name == "GET home/sample");
		}

		/// <summary>
		/// Tests a URL that maps to a route with default values. Calls "/", which maps to "home/index".
		/// Makes sure "Home/Index" is in the Transaction.Name.
		/// </summary>
		[Fact]
		public async Task DefaultRouteParameterValues()
		{
			var httpClient = Helper.GetClient(_agent, _factory);
			await httpClient.GetAsync("/");

			_payloadSender.FirstTransaction.Name.Should().Be("GET Home/Index");
			_payloadSender.FirstTransaction.Context.Request.Url.Full.Should().Be("http://localhost/");
		}

		public void Dispose()
		{
			_agent?.Dispose();
			_factory?.Dispose();
		}
	}
}
