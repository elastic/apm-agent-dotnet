using System;
using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm.Extensions.Hosting;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SampleAspNetCoreApp;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	[Collection("DiagnosticListenerTest")]
	public class TransactionIgnoreUrlsTest : IClassFixture<WebApplicationFactory<Startup>>, IDisposable
	{
		private readonly ApmAgent _agent;
		private readonly MockPayloadSender _capturedPayload;
		private readonly WebApplicationFactory<Startup> _factory;

		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		private readonly IApmLogger _logger;

		public TransactionIgnoreUrlsTest(WebApplicationFactory<Startup> factory)
		{
			_factory = factory;
			_logger = new NoopLogger(); // _logger.Scoped(ThisClassName);

			_agent = new ApmAgent(new TestAgentComponents(
				_logger,
				new MockConfigSnapshot(_logger, transactionIgnoreUrls: "*simplepage"),
				// _agent needs to share CurrentExecutionSegmentsContainer with Agent.Instance
				// because the sample application used by the tests (SampleAspNetCoreApp) uses Agent.Instance.Tracer.CurrentTransaction/CurrentSpan
				currentExecutionSegmentsContainer: Agent.Instance.TracerInternal.CurrentExecutionSegmentsContainer)
			);
			HostBuilderExtensions.UpdateServiceInformation(_agent.Service);

			_capturedPayload = _agent.PayloadSender as MockPayloadSender;
		}

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
		/// In the ctor we add `*SimplePage` to the ignoreUrl list. This test makes sure that /home/SimplePage is indeed ignored.
		/// </summary>
		/// <returns></returns>
		[InlineData(true)]
		[InlineData(false)]
		[Theory]
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
