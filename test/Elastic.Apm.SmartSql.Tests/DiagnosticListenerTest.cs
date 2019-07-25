using System;
using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.SmartSql;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SmartSqlAspNetCodeApp;
using Xunit;

namespace Elastic.Apm.SmartSql.Tests
{
	/// <summary>
	/// Tests subscribing and unsubscribing from diagnostic source events.
	/// </summary>
	[Collection("DiagnosticListenerTest")]
	public class DiagnosticListenerTest : IClassFixture<WebApplicationFactory<Startup>>, IDisposable
	{
		private readonly ApmAgent _agent;
		private readonly MockPayloadSender _capturedPayload;

		public DiagnosticListenerTest(WebApplicationFactory<Startup> factory)
		{
			_agent = new ApmAgent(new TestAgentComponents());
			_capturedPayload = _agent.PayloadSender as MockPayloadSender;
			_client = Helper.GetClientWithoutDiagnosticListeners(_agent, factory);
		}

		private readonly HttpClient _client;

		/// <summary>
		/// Manually starts <see cref="SmartSqlDiagnosticsSubscriber" /> and does 1 HTTP call
		/// that triggers db calls,
		/// then it disposes the <see cref="SmartSqlDiagnosticsSubscriber" /> (aka unsubsribes)
		/// and does another HTTP call that triggers db calls.
		/// It makes sure that for the 1. HTTP call the db calls are captured and for the 2. they aren't.
		/// </summary>
		[Fact]
		public async Task SubscribeAndUnsubscribeSmartSqlDiagnosticListener()
		{
			using (_agent.Subscribe(new SmartSqlDiagnosticsSubscriber()))
			{
				var response=await _client.GetAsync("/Home/Index");

				_capturedPayload.Transactions.Should().ContainSingle();

				_capturedPayload.SpansOnFirstTransaction.Should()
					.NotBeEmpty()
					.And.Contain(n => n.Context.Db != null);
			} //here we unsubscribe, so no errors should be captured after this line.

			_capturedPayload.Clear();

			await _client.GetAsync("/Home/Index");

			_capturedPayload.Transactions.Should().ContainSingle();

			_capturedPayload.SpansOnFirstTransaction.Should().BeEmpty();
		}

		public void Dispose()
		{
			_client?.Dispose();
			_agent?.Dispose();
		}
	}
}
