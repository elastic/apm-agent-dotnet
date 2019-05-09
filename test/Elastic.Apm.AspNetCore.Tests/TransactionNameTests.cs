using System;
using System.Threading.Tasks;
using Elastic.Apm.AspNetCore.Tests.Factories;
using Elastic.Apm.AspNetCore.Tests.Fakes;
using Elastic.Apm.AspNetCore.Tests.Helpers;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Tests the transaction name in ASP.NET Core.
	/// Specifically: optional parameters in the route should be templated in the Transaction.Name.
	/// E.g. URL localhost/user/info/1 should get have Transaction.Name GET user/info {id}
	/// </summary>
	[Collection("DiagnosticListenerTest")]
	public class TransactionNameTests : IClassFixture<CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup>>, IDisposable
	{
		private readonly CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup> _factory;

		public TransactionNameTests(CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup> factory) => _factory = factory;

		/// <summary>
		/// Calls a URL that maps to a route with optional parameter (id).
		/// Makes sure the Transaction.Name contains "{id}" instead of the value.
		/// </summary>
		[Fact]
		public async Task OptionalRouteParameter()
		{
			using (var agent = GetAgent())
			{
				using (var client = TestHelper.GetClient(_factory, agent))
				{
					await client.GetAsync("Home/Sample/3");
					await client.GetAsync("Home/Sample/2");
				}

				((MockPayloadSender)agent.PayloadSender).Transactions.Should().OnlyContain(n => n.Name == "GET Home/Sample {id}");
			}
		}

		/// <summary>
		/// Calls a URL that would map to a route with optional parameter (id), but does not pass the id value.
		/// Makes sure no template value is captured in Transaction.Name.
		/// </summary>
		[Fact]
		public async Task OptionalRouteParameterWithNull()
		{
			using (var agent = GetAgent())
			{
				using (var client = TestHelper.GetClient(_factory, agent))
				{
					await client.GetAsync("Home/Sample");
				}

				((MockPayloadSender)agent.PayloadSender).Transactions.Should().OnlyContain(n => n.Name == "GET Home/Sample");
			}
		}

		/// <summary>
		/// Tests a URL that maps to a route with default values. Calls "/", which maps to "home/index".
		/// Makes sure "Home/Index" is in the Transaction.Name.
		/// </summary>
		[Fact]
		public async Task DefaultRouteParameterValues()
		{
			using (var agent = GetAgent())
			{
				using (var client = TestHelper.GetClient(_factory, agent))
				{
					await client.GetAsync("/");
				}

				var capturedPayload = (MockPayloadSender)agent.PayloadSender;
				capturedPayload.FirstTransaction.Name.Should().Be("GET Home/Index");
				capturedPayload.FirstTransaction.Context.Request.Url.Full.Should().Be("http://localhost/");
			}
		}

		private static ApmAgent GetAgent()
		{
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: new MockPayloadSender()));
			ApmMiddlewareExtension.UpdateServiceInformation(agent.Service);
			return agent;
		}

		public void Dispose() => _factory.Dispose();
	}
}
