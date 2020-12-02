// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Elastic.Apm.Extensions.Hosting.Config;
using Elastic.Apm.Tests.Mocks;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using SampleAspNetCoreApp;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetCore.Tests
{
	[Collection("DiagnosticListenerTest")] //To avoid tests from DiagnosticListenerTests running in parallel with this we add them to 1 collection.
	public class ConfigTests : LoggingTestBase, IClassFixture<WebApplicationFactory<Startup>>
	{
		private readonly WebApplicationFactory<Startup> _factory;

		public ConfigTests(WebApplicationFactory<Startup> factory, ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) =>
			_factory = factory;

		/// <summary>
		/// Loads an appsetting.json with `enabled=false` and calls an HTTP endpoint which uses the public agent API.
		/// Makes sure the agent does not capture a real transaction and no connection is made to the default APM Server endpoint.
		/// Tests for: https://github.com/elastic/apm-agent-dotnet/issues/1077
		/// </summary>
		/// <param name="withDiagnosticSourceOnly"></param>
		[InlineData(true)]
		[InlineData(false)]
		[Theory]
		public async Task AgentDisabledInAppConfig(bool withDiagnosticSourceOnly)
		{
			var defaultServerUrlConnectionMade = false;
			using var localServer = new LocalServer(context => defaultServerUrlConnectionMade = true, "http://localhost:8200/");
			var configReader = new MicrosoftExtensionsConfig(new ConfigurationBuilder()
				.AddJsonFile($"TestConfigs{Path.DirectorySeparatorChar}appsettings_agentdisabled.json")
				.Build(), new NoopLogger(), "test");

			var capturedPayload = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(
				new NoopLogger(),
				new ConfigSnapshotFromReader(configReader, "MicrosoftExtensionsConfigReader"), capturedPayload));

			var client = Helper.ConfigureHttpClient(true, withDiagnosticSourceOnly, agent, _factory);


			var response = await client.GetAsync("/Home/StartTransactionWithAgentApi");
			response.IsSuccessStatusCode.Should().BeTrue();

			var isParsed = bool.TryParse(await response.Content.ReadAsStringAsync(), out var boolVal);

			isParsed.Should().BeTrue();
			boolVal.Should().BeFalse();
			capturedPayload.Transactions.Should().BeNullOrEmpty();
			capturedPayload.Spans.Should().BeNullOrEmpty();
			capturedPayload.Errors.Should().BeNullOrEmpty();
			defaultServerUrlConnectionMade.Should().BeFalse();
		}
	}
}
