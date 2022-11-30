// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm.AspNetCore.Tests;
using Elastic.Apm.Config;
using Elastic.Apm.Extensions.Hosting.Config;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.XUnit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using SampleAspNetCoreApp;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetCore.Static.Tests
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
		[Theory]
		[MemberData(nameof(MemberData.TestWithDiagnosticSourceOnly), MemberType = typeof(MemberData))]
		public async Task AgentDisabledInAppConfig(bool withDiagnosticSourceOnly)
		{
			var defaultServerUrlConnectionMade = false;

			using var localServer = LocalServer.Create("http://localhost:8200/", context =>
			{
				if (context.Request.HttpMethod != HttpMethod.Post.Method) return;

				var body = new StreamReader(context.Request.InputStream).ReadToEnd();
				if (context.Request.HttpMethod.Equals(HttpMethod.Post.Method, StringComparison.OrdinalIgnoreCase)) return;

				// In CI with parallel tests running we could have multiple tests running and some of them may send HTTP post to the default server url
				// So we only make the test fail, when the request body contains the sample app's url
				if (body.ToLower().Contains("starttransactionwithagentapi")) defaultServerUrlConnectionMade = true;
			});

			var configReader = new MicrosoftExtensionsConfig(new ConfigurationBuilder()
				.AddJsonFile($"TestConfigs{Path.DirectorySeparatorChar}appsettings_agentdisabled.json")
				.Build(), new NoopLogger(), "test");

			var capturedPayload = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(
				new NoopLogger(),
				new ConfigurationSnapshotFromReader(configReader, "MicrosoftExtensionsConfigReader")));

			var client = Helper.ConfigureHttpClient(true, withDiagnosticSourceOnly, agent, _factory);
			if (withDiagnosticSourceOnly)
				Agent.Setup(agent);

			var response = await client.GetAsync("/Home/StartTransactionWithAgentApi");
			response.IsSuccessStatusCode.Should().BeTrue();

			var isParsed = bool.TryParse(await response.Content.ReadAsStringAsync(), out var boolVal);

			isParsed.Should().BeTrue();
			boolVal.Should().BeFalse();
			capturedPayload.Transactions.Should().BeNullOrEmpty();
			capturedPayload.Spans.Should().BeNullOrEmpty();
			capturedPayload.Errors.Should().BeNullOrEmpty();
			// Make the test fail if there was a connection to the server URL made with the sample app's url
			defaultServerUrlConnectionMade.Should().BeFalse();
		}
	}
}
