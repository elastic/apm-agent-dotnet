// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm.Extensions.Hosting;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.XUnit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SampleAspNetCoreApp;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	[Collection("DiagnosticListenerTest")]
	public class TransactionIgnoreUrlsTest : IClassFixture<WebApplicationFactory<Startup>>, IDisposable
	{
		private readonly WebApplicationFactory<Startup> _factory;

		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		private readonly IApmLogger _logger;

		public TransactionIgnoreUrlsTest(WebApplicationFactory<Startup> factory)
		{
			_factory = factory;
			_logger = new NoopLogger(); // _logger.Scoped(ThisClassName);

			_agent = new ApmAgent(new TestAgentComponents(
				_logger,
				new MockConfiguration(_logger, transactionIgnoreUrls: "*simplepage"),
				// _agent needs to share CurrentExecutionSegmentsContainer with Agent.Instance
				// because the sample application used by the tests (SampleAspNetCoreApp) uses Agent.Instance.Tracer.CurrentTransaction/CurrentSpan
				currentExecutionSegmentsContainer: Agent.Instance.TracerInternal.CurrentExecutionSegmentsContainer)
			);
			HostBuilderExtensions.UpdateServiceInformation(_agent.Service);

			_capturedPayload = _agent.PayloadSender as MockPayloadSender;
		}

		private ApmAgent _agent;
		private MockPayloadSender _capturedPayload;

		private HttpClient _client;

		private void Setup(bool useOnlyDiagnosticSource)
		{
#pragma warning disable IDE0022 // Use expression body for methods
			_client = Helper.GetClient(_agent, _factory, useOnlyDiagnosticSource);
#pragma warning restore IDE0022 // Use expression body for methods
#if NETCOREAPP3_0 || NETCOREAPP3_1
			_client.DefaultRequestVersion = new Version(2, 0);
#endif
		}

		/// <summary>
		/// Changes the transactionIgnoreUrls during startup and asserts that the agent reacts accordingly.
		/// </summary>
		/// <param name="useDiagnosticSourceOnly"></param>
		/// <returns></returns>
		[Theory]
		[MemberData(nameof(MemberData.TestWithDiagnosticSourceOnly), MemberType = typeof(MemberData))]
		public async Task ChangeTransactionIgnoreUrlsAfterStart(bool useDiagnosticSourceOnly)
		{
			// Start with default config
			var startConfigSnapshot = new MockConfiguration(new NoopLogger());
			_capturedPayload = new MockPayloadSender();

			var agentComponents = new TestAgentComponents(
				_logger,
				startConfigSnapshot, _capturedPayload,
				new CurrentExecutionSegmentsContainer());

			_agent = new ApmAgent(agentComponents);
			_client = Helper.GetClient(_agent, _factory, useDiagnosticSourceOnly);

			_client.DefaultRequestHeaders.Add("foo", "bar");
			await _client.GetAsync("/Home/SimplePage");

			_capturedPayload.WaitForTransactions();
			_capturedPayload.Transactions.Should().ContainSingle();
			_capturedPayload.FirstTransaction.Context.Request.Url.Full.ToLower().Should().Contain("simplepage");

			//change config to ignore urls with SimplePage
			var updateConfigSnapshot = new MockConfiguration(
				new NoopLogger()
				, transactionIgnoreUrls: "*SimplePage*"
			);
			_agent.ConfigurationStore.CurrentSnapshot = updateConfigSnapshot;

			await _client.GetAsync("/Home/SimplePage");

			_capturedPayload.WaitForTransactions(TimeSpan.FromSeconds(5));

			//assert that no more transaction is captured - so still 1 captured transaction
			_capturedPayload.Transactions.Should().ContainSingle();

			//update config again
			updateConfigSnapshot = new MockConfiguration(
				new NoopLogger()
				, transactionIgnoreUrls: "FooBar"
			);
			_agent.ConfigurationStore.CurrentSnapshot = updateConfigSnapshot;

			await _client.GetAsync("/Home/SimplePage");

			_capturedPayload.WaitForTransactions();

			//assert that the number of captured transaction increased
			_capturedPayload.Transactions.Count.Should().Be(2);
		}

		/// <summary>
		/// In the ctor we add `*SimplePage` to the ignoreUrl list. This test makes sure that /home/SimplePage is indeed ignored.
		/// </summary>
		/// <returns></returns>
		[Theory]
		[MemberData(nameof(MemberData.TestWithDiagnosticSourceOnly), MemberType = typeof(MemberData))]
		public async Task IgnoreSimplePage(bool useOnlyDiagnosticSource)
		{
			Setup(useOnlyDiagnosticSource);
			var response = await _client.GetAsync("/Home/SimplePage?myUrlParam=123");

			response.IsSuccessStatusCode.Should().BeTrue();
			_capturedPayload.Transactions.Should().BeNullOrEmpty();
			_capturedPayload.Spans.Should().BeNullOrEmpty();
			_capturedPayload.Errors.Should().BeNullOrEmpty();
		}

		public void Dispose()
		{
			_agent?.Dispose();
			_factory?.Dispose();
			_client?.Dispose();
		}
	}
}
