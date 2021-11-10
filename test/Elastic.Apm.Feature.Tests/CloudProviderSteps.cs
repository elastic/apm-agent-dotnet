// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Net.Http;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.XUnit;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using RichardSzalay.MockHttp;
using TechTalk.SpecFlow;
using Xunit.Abstractions;
using static Elastic.Apm.BackendComm.BackendCommUtils.ApmServerEndpoints;
using MockHttpMessageHandler = RichardSzalay.MockHttp.MockHttpMessageHandler;

namespace Elastic.Apm.Feature.Tests
{
	[Binding]
	public class CloudProviderSteps
	{

		private readonly ScenarioContext _scenarioContext;

		public CloudProviderSteps(ScenarioContext scenarioContext) => _scenarioContext = scenarioContext;

		[Given(@"^an instrumented application is configured to collect cloud provider metadata for (.*?)$")]
		public void AgentWithCloudMetadata(string cloudProvider)
		{
			var output = _scenarioContext.ScenarioContainer.Resolve<ITestOutputHelper>();
			var logger = new XUnitLogger(LogLevel.Trace, output);
			var config = new MockConfiguration(logger, cloudProvider: cloudProvider, flushInterval: "0");

			var payloadCollector = new PayloadCollector();
			_scenarioContext.Set(payloadCollector);

			var handler = new MockHttpMessageHandler();
			handler.When(BuildIntakeV2EventsAbsoluteUrl(config.ServerUrl).AbsoluteUri)
				.Respond(r =>
				{
					payloadCollector.ProcessPayload(r);
					return new HttpResponseMessage(HttpStatusCode.OK);
				});

			var environmentVariables = new TestEnvironmentVariables();
			_scenarioContext.Set(environmentVariables);

			var payloadSender = new PayloadSenderV2(
					logger,
					config,
					Service.GetDefaultService(config, new NoopLogger()),
					new Api.System(),
					MockApmServerInfo.Version710,
					handler,
					environmentVariables: environmentVariables);

			_scenarioContext.Set(() => new ApmAgent(new TestAgentComponents(logger, config, payloadSender)));
		}

		[Given("^the following environment variables are present$")]
		public void EnvironmentVariablesSet(Table table)
		{
			var environmentVariables = _scenarioContext.Get<TestEnvironmentVariables>();

			foreach(var row in table.Rows)
				environmentVariables[row[0]] = row[1];
		}

		[When("^cloud metadata is collected$")]
		public void CollectCloudMetadata()
		{
			// create the agent and capture a transaction to send metadata
			var agent = _scenarioContext.Get<ApmAgent>();
			agent.Tracer.CaptureTransaction("Transaction", "feature", () => { });

			var payloadCollector = _scenarioContext.Get<PayloadCollector>();

			payloadCollector.Wait();
		}

		[Then("^cloud metadata is not null$")]
		public void CloudMetadataIsNotNull()
		{
			var payloadCollector = _scenarioContext.Get<PayloadCollector>();

			payloadCollector.Payloads.Should().NotBeEmpty();
			var cloudMetadata = payloadCollector.Payloads[0].Body[0]["metadata"]["cloud"];
			cloudMetadata.Should().NotBeNull();
		}

		[Then("^cloud metadata is null$")]
		public void CloudMetadataIsNull()
		{
			var payloadCollector = _scenarioContext.Get<PayloadCollector>();

			payloadCollector.Payloads.Should().NotBeEmpty();
			var cloudMetadata = payloadCollector.Payloads[0].Body[0]["metadata"]["cloud"];
			cloudMetadata.Should().BeNull();
		}

		[Then("^cloud metadata '(.*?)' is '(.*?)'$")]
		public void CloudMetadataKeyEqualsValue(string key, string value)
		{
			var payloadCollector = _scenarioContext.Get<PayloadCollector>();
			var token = payloadCollector.Payloads[0].Body[0].SelectToken($"metadata.cloud.{key}");

			token.Should().NotBeNull();
			token.Value<string>().Should().Be(value);
		}
	}
}
