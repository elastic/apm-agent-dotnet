// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.EntityFrameworkCore;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SampleAspNetCoreApp;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Tests subscribing and unsubscribing from diagnostic source events.
	/// </summary>
	[Collection("DiagnosticListenerTest")]
	public class DiagnosticListenerTests : IClassFixture<WebApplicationFactory<Startup>>, IDisposable
	{
		private readonly ApmAgent _agent;
		private readonly MockPayloadSender _capturedPayload;

		private readonly HttpClient _client;

		public DiagnosticListenerTests(WebApplicationFactory<Startup> factory)
		{
			_agent = new ApmAgent(new TestAgentComponents());
			_capturedPayload = _agent.PayloadSender as MockPayloadSender;

			//This registers the middleware without activating any listeners,
			//so no error capturing and no EFCore listener.
			_client = Helper.GetClientWithoutDiagnosticListeners(_agent, factory);
		}

		/// <summary>
		/// Manually starts <see cref="AspNetCoreErrorDiagnosticsSubscriber" /> and does 1 HTTP call
		/// that throws an exception,
		/// then it disposes the <see cref="AspNetCoreErrorDiagnosticsSubscriber" /> (aka unsubsribes)
		/// and does another HTTP call that throws an exception.
		/// It makes sure that for the 1. HTTP call the errors is captured and for the 2. it isn't.
		/// </summary>
		[Fact]
		public async Task SubscribeAndUnsubscribeAspNetCoreDiagnosticListener()
		{
			//For reference: unsubscribing from AspNetCoreDiagnosticListener does not seem to work.
			//TODO: this should be investigated. This is more relevant for testing.
			//			using (_agent.Subscribe(new AspNetCoreDiagnosticsSubscriber()))
			//			{
			//				await _client.GetAsync("/Home/TriggerError");
			//
			//				_capturedPayload.Transactions.Should().ContainSingle();
			//
			//				_capturedPayload.Errors.Should().NotBeEmpty();
			//				_capturedPayload.Errors.Should().ContainSingle();
			//				 _capturedPayload.Errors[0].CapturedException.Type.Should().Be(typeof(Exception).FullName);
			//			} //here we unsubsribe, so no errors should be captured after this line.

			_agent.Dispose();

			_capturedPayload.Clear();

			await _client.GetAsync("/Home/TriggerError");

			_capturedPayload.Transactions.Should().ContainSingle();
			_capturedPayload.Errors.Should().BeEmpty();
		}

		/// <summary>
		/// Manually starts <see cref="EfCoreDiagnosticsSubscriber" /> and does 1 HTTP call
		/// that triggers db calls,
		/// then it disposes the <see cref="EfCoreDiagnosticsSubscriber" /> (aka unsubsribes)
		/// and does another HTTP call that triggers db calls.
		/// It makes sure that for the 1. HTTP call the db calls are captured and for the 2. they aren't.
		/// </summary>
		[Fact]
		public async Task SubscribeAndUnsubscribeEfCoreDiagnosticListener()
		{
			using (_agent.Subscribe(new EfCoreDiagnosticsSubscriber()))
			{
				await _client.GetAsync("/Home/Index");

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
