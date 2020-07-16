using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm.Extensions.Hosting;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SampleAspNetCoreApp;
using Xunit;
using Xunit.Abstractions;

#if NETCOREAPP3_0 || NETCOREAPP3_1
using System;
#endif

namespace Elastic.Apm.AspNetCore.Tests
{
	[Collection("DiagnosticListenerTest")]
	public class TransactionIgnoreUrlsTest : LoggingTestBase, IClassFixture<WebApplicationFactory<Startup>>
	{
		private const string ThisClassName = nameof(AspNetCoreMiddlewareTests);
		private readonly ApmAgent _agent;
		private readonly MockPayloadSender _capturedPayload;
		private readonly WebApplicationFactory<Startup> _factory;

		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		private readonly IApmLogger _logger;

		private readonly HttpClient _client;


		public TransactionIgnoreUrlsTest(WebApplicationFactory<Startup> factory, ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper)
		{
			_factory = factory;
			_logger = LoggerBase.Scoped(ThisClassName);

			_agent = new ApmAgent(new TestAgentComponents(
				_logger,
				new MockConfigSnapshot(_logger, transactionIgnoreUrls: "*simplepage"),
				// _agent needs to share CurrentExecutionSegmentsContainer with Agent.Instance
				// because the sample application used by the tests (SampleAspNetCoreApp) uses Agent.Instance.Tracer.CurrentTransaction/CurrentSpan
				currentExecutionSegmentsContainer: Agent.Instance.TracerInternal.CurrentExecutionSegmentsContainer)
			);
			HostBuilderExtensions.UpdateServiceInformation(_agent.Service);

			_capturedPayload = _agent.PayloadSender as MockPayloadSender;
			_client = Helper.GetClient(_agent, _factory);
#if NETCOREAPP3_0 || NETCOREAPP3_1
			_client.DefaultRequestVersion = new Version(2, 0);
#endif
		}


		/// <summary>
		/// In the ctor we add `*SimplePage` to the ignoreUrl list. This test makes sure that /home/SimplePage is indeed ignored.
		/// </summary>
		/// <returns></returns>
		[Fact]
		public async Task IgnoreSimplePage()
		{
			var response = await _client.GetAsync("/Home/SimplePage");

			response.IsSuccessStatusCode.Should().BeTrue();
			_capturedPayload.Transactions.Should().BeEmpty();
			_capturedPayload.Spans.Should().BeEmpty();
			_capturedPayload.Errors.Should().BeEmpty();
		}
	}
}
