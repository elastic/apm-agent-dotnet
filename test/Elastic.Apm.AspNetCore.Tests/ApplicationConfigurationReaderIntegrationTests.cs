using System;
using System.IO;
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
	/// Tests that use a real ASP.NET Core application.
	/// </summary>
	[Collection("DiagnosticListenerTest")] // To avoid tests from DiagnosticListenerTests running in parallel with this we add them to 1 collection.
	public class ApplicationConfigurationReaderIntegrationTests : IClassFixture<CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup>>, IDisposable
	{
		private readonly CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup> _factory;

		public ApplicationConfigurationReaderIntegrationTests(CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup> factory) => _factory = factory;

		/// <summary>
		/// Starts the app with an invalid config and makes sure the agent logs that the URL was invalid.
		/// </summary>
		[Fact]
		public async Task InvalidUrlTest()
		{
			var logger = new TestLogger();

			// The agent is instantiated with ApmMiddlewareExtension.GetService, so we can also test the calculation of the service instance (e.g. ASP.NET Core version).
			var config = new ApplicationConfigurationReader(ApplicationConfigurationReaderTests.GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_invalid.json"), logger);

			using (var agent = new ApmAgent(new AgentComponents(payloadSender: new MockPayloadSender(), configurationReader: config, logger: logger)))
			using (var client = TestHelper.GetClient(_factory, agent))
			{
				var response = await client.GetAsync("/Home/Index");
				response.IsSuccessStatusCode.Should().BeTrue();
			}

			logger.Lines.Should().NotBeEmpty().And.Contain(n => n.Contains("Failed parsing server URL from"));
		}

		public void Dispose() => _factory.Dispose();
	}
}
