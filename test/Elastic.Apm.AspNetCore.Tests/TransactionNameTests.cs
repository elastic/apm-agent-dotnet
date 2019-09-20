using System;
using System.Threading.Tasks;
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
	/// Tests the transaction name in ASP.NET Core.
	/// Specifically: optional parameters in the route should be templated in the Transaction.Name.
	/// E.g. url localhost/user/info/1 should get have Transaction.Name GET user/info {id}
	/// </summary>
	[Collection("DiagnosticListenerTest")]
	public class TransactionNameTests : LoggingTestBase, IClassFixture<WebApplicationFactory<Startup>>
	{
		private readonly ApmAgent _agent;
		private readonly WebApplicationFactory<Startup> _factory;
		private readonly MockPayloadSender _payloadSender = new MockPayloadSender();

		public TransactionNameTests(WebApplicationFactory<Startup> factory, ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper)
		{
			_factory = factory;

			// We need to ensure Agent.Instance is created because we need _agent to use Agent.Instance CurrentExecutionSegmentsContainer
			AgentSingletonUtils.EnsureInstanceCreated();
			_agent = new ApmAgent(new TestAgentComponents(payloadSender: _payloadSender,
				// _agent needs to share CurrentExecutionSegmentsContainer with Agent.Instance
				// because the sample application used by the tests (SampleAspNetCoreApp) uses Agent.Instance.Tracer.CurrentTransaction/CurrentSpan
				currentExecutionSegmentsContainer: Agent.Instance.TracerInternal.CurrentExecutionSegmentsContainer));
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
			await httpClient.GetAsync("home/TransactionWithCustomName");

			_payloadSender.Transactions.Should().OnlyContain(n => n.Name == "custom");
		}

		/// <summary>
		/// Calls a URL that sets custom transaction name by using $"{HttpContext.Request.Method} {HttpContext.Request.Path}"
		/// Makes sure the Transaction.Name is $"{HttpContext.Request.Method} {HttpContext.Request.Path}" and the agent does not
		/// change that.
		/// See: https://github.com/elastic/apm-agent-dotnet/pull/258#discussion_r291025014
		/// </summary>
		[Fact]
		public async Task CustomTransactionNameWithNameUsingRequestInfo()
		{
			var httpClient = Helper.GetClient(_agent, _factory);
			await httpClient.GetAsync("home/TransactionWithCustomNameUsingRequestInfo");

			_payloadSender.Transactions.Should().OnlyContain(n => n.Name == "GET /home/TransactionWithCustomNameUsingRequestInfo");
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

		public override void Dispose()
		{
			_agent?.Dispose();
			_factory?.Dispose();

			base.Dispose();
		}
	}
}
