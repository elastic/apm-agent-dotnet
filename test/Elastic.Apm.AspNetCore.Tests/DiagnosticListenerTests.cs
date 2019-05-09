using System;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.AspNetCore.Tests.Factories;
using Elastic.Apm.AspNetCore.Tests.Fakes;
using Elastic.Apm.AspNetCore.Tests.Helpers;
using Elastic.Apm.EntityFrameworkCore;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Tests subscribing and unsubscribing from diagnostic source events.
	/// </summary>
	[Collection("DiagnosticListenerTest")]
	public class DiagnosticListenerTests : IClassFixture<CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup>>, IDisposable
	{
		private readonly CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup> _factory;

		public DiagnosticListenerTests(CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup> factory) => _factory = factory;

		/// <summary>
		/// Manually starts <see cref="AspNetCoreDiagnosticsSubscriber" /> and does one HTTP call that throws an exception, then it disposes
		/// the <see cref="AspNetCoreDiagnosticsSubscriber" /> (aka unsubscribes) and does another HTTP call that throws an exception.
		/// It makes sure that for the first HTTP call the errors is captured and for the second it isn't.
		/// </summary>
		[Fact]
		public async Task SubscribeAndUnsubscribeAspNetCoreDiagnosticListener()
		{
			using (var agent = GetAgent())
			{
				var capturedPayload = (MockPayloadSender)agent.PayloadSender;

				using (var client = TestHelper.GetClientWithoutSubscribers(_factory, agent))
				{
					using (agent.Subscribe(new AspNetCoreDiagnosticsSubscriber()))
					{
						await client.GetAsync("/Home/TriggerError");

						capturedPayload.Transactions.Should().ContainSingle();
						capturedPayload.Errors.Should().NotBeEmpty();
						capturedPayload.Errors.Should().ContainSingle();
						((Error)capturedPayload.Errors.First()).Exception.Type.Should().Be(typeof(Exception).FullName);
					} // Here we unsubscribe, so no errors should be captured after this line.

					capturedPayload.Clear();

					await client.GetAsync("/Home/TriggerError");
				}

				capturedPayload.Transactions.Should().ContainSingle();
				capturedPayload.Errors.Should().BeEmpty();
			}
		}

		/// <summary>
		/// Manually starts <see cref="EfCoreDiagnosticsSubscriber" /> and does 1 HTTP call that triggers DB calls,
		/// then it disposes the <see cref="EfCoreDiagnosticsSubscriber" /> (aka unsubscribes) and does another HTTP
		/// call that triggers DB calls.
		/// It makes sure that for the first HTTP call the DB calls are captured and for the second they aren't.
		/// </summary>
		[Fact]
		public async Task SubscribeAndUnsubscribeEfCoreDiagnosticListener()
		{
			using (var agent = GetAgent())
			{
				var capturedPayload = (MockPayloadSender)agent.PayloadSender;

				using (var client = TestHelper.GetClientWithoutSubscribers(_factory, agent))
				{
					using (agent.Subscribe(new EfCoreDiagnosticsSubscriber()))
					{
						await client.GetAsync("/Home/Index");

						capturedPayload.Transactions.Should().ContainSingle();
						capturedPayload.SpansOnFirstTransaction.Should().NotBeEmpty().And.Contain(n => n.Context.Db != null);
					} // Here we unsubscribe, so no errors should be captured after this line.

					capturedPayload.Clear();

					await client.GetAsync("/Home/Index");
				}

				capturedPayload.Transactions.Should().ContainSingle();
				capturedPayload.SpansOnFirstTransaction.Should().BeEmpty();
			}
		}

		private static ApmAgent GetAgent() => new ApmAgent(new TestAgentComponents());

		public void Dispose() => _factory.Dispose();
	}
}
