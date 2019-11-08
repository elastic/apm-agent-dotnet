using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Elastic.Apm.Tests.Data;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using SampleAspNetCoreApp;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Tests that use a real ASP.NET Core application.
	/// </summary>
	[Collection("DiagnosticListenerTest")] //To avoid tests from DiagnosticListenerTests running in parallel with this we add them to 1 collection.
	public class ApplicationConfigurationReaderIntegrationTests : IClassFixture<WebApplicationFactory<Startup>>, IDisposable
	{
		private readonly ApmAgent _agent;
		private readonly HttpClient _client;
		private readonly WebApplicationFactory<Startup> _factory;
		private readonly TestLogger _logger;

		public ApplicationConfigurationReaderIntegrationTests(WebApplicationFactory<Startup> factory)
		{
			_factory = factory;
			_logger = new TestLogger();
			var capturedPayload = new MockPayloadSender();

			var config = new ApplicationConfigurationReader(ApplicationConfigurationReaderTests.GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_invalid.json"), _logger, "test");

			_agent = new ApmAgent(new AgentComponents(payloadSender: capturedPayload, configurationReader: config, logger: _logger));
			_client = Helper.GetClient(_agent, _factory);
		}

		/// <summary>
		/// Starts the app with an invalid config and
		/// makes sure the agent logs that the url was invalid.
		/// </summary>
		[Fact]
		public async Task InvalidUrlTest()
		{
			var response = await _client.GetAsync("/Home/Index");
			response.IsSuccessStatusCode.Should().BeTrue();

			_logger.Lines.Should().NotBeEmpty().And.Contain(n => n.Contains("Failed parsing server URL from"));
		}

		[Theory]
		[ClassData(typeof(TransactionMaxSpansTestData))]
		public void TransactionMaxSpansTest(string configurationValue, int expectedValue)
		{
			// Arrange
			var logger = new TestLogger();

			var configurationBuilder = new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string> { { ConfigConsts.KeyNames.TransactionMaxSpans, configurationValue } });

			var reader = new ApplicationConfigurationReader(configurationBuilder.Build(), logger, "test");

			// Act
			var transactionMaxSpans = reader.TransactionMaxSpans;

			// Assert
			transactionMaxSpans.Should().Be(expectedValue);
		}

		public void Dispose()
		{
			_factory?.Dispose();
			_agent?.Dispose();
			_client?.Dispose();
		}
	}
}
