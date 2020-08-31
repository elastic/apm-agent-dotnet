// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm.Extensions.Hosting;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using SampleAspNetCoreApp;

namespace Elastic.Apm.AspNetCore.Tests
{
	[Collection("DiagnosticListenerTest")]
	public class TraceIdWithActivityTests : IClassFixture<WebApplicationFactory<Startup>>, IDisposable
	{
		private readonly ApmAgent _agent;
		private readonly WebApplicationFactory<Startup> _factory;
		private readonly MockPayloadSender _payloadSender = new MockPayloadSender();
		private readonly HttpClient _client;

		public TraceIdWithActivityTests(WebApplicationFactory<Startup> factory)
		{
			_factory = factory;

			_agent = new ApmAgent(new TestAgentComponents(payloadSender: _payloadSender,
				// _agent needs to share CurrentExecutionSegmentsContainer with Agent.Instance
				// because the sample application used by the tests (SampleAspNetCoreApp) uses Agent.Instance.Tracer.CurrentTransaction/CurrentSpan
				currentExecutionSegmentsContainer: Agent.Instance.TracerInternal.CurrentExecutionSegmentsContainer));
			HostBuilderExtensions.UpdateServiceInformation(_agent.Service);
			_client = Helper.GetClient(_agent, _factory, true);
		}

		public void Dispose()
		{
			_agent?.Dispose();
			_factory?.Dispose();
		}

		/// <summary>
		/// Makes sure that for the HTTP request in ASP.NET Core the generated transaction has the same trace id as Activity.Current.TraceId
		/// </summary>
		[Fact]
		public async Task ActivityFromAspNetCoreAndTransactionWithSameTraceId()
		{
			Activity.DefaultIdFormat = ActivityIdFormat.W3C;
			var res = await _client.GetAsync("Home/TraceId");
			var activityId = await res.Content.ReadAsStringAsync();
			_payloadSender.FirstTransaction.TraceId.Should().Be(activityId);
		}
	}
}
