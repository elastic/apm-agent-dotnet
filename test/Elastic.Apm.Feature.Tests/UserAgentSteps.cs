// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.XUnit;
using FluentAssertions;
using RichardSzalay.MockHttp;
using TechTalk.SpecFlow;
using Xunit.Abstractions;

using static Elastic.Apm.BackendComm.BackendCommUtils.ApmServerEndpoints;
using MockHttpMessageHandler = RichardSzalay.MockHttp.MockHttpMessageHandler;

namespace Elastic.Apm.Feature.Tests
{
	[Binding]
	public class UserAgentSteps
	{
		private readonly ScenarioContext _scenarioContext;

		public UserAgentSteps(ScenarioContext scenarioContext) => _scenarioContext = scenarioContext;

		[Then("the User-Agent header of the request matches regex '([^']*)'")]
		public void ThenTheUserAgentHeaderMatchesRegex(string match)
		{
			var regex = new Regex(match);
			var payloadCollector = _scenarioContext.Get<PayloadCollector>();
			var agent = _scenarioContext.Get<ApmAgent>();

			// capture a transaction so that the agent sends a payload
			agent.Tracer.CaptureTransaction("User agent", "app", () => { });
			payloadCollector.Wait();

			var userAgent = payloadCollector.Payloads.First().Headers.UserAgent;
			userAgent.Should().NotBeNull().And.HaveCountGreaterOrEqualTo(1);

			var values = string.Join(" ", userAgent.Select(u => u.ToString()));
			regex.IsMatch(values).Should().BeTrue($"user agent values {values} should match {match}");
		}

		[Given(@"an agent configured with")]
		public void GivenAnAgentConfiguredWith(Table table)
		{

			var output = _scenarioContext.ScenarioContainer.Resolve<ITestOutputHelper>();
			var logger = new XUnitLogger(LogLevel.Trace, output);
			var configuration = new TestConfiguration();
			_scenarioContext.Set(configuration);

			var payloadCollector = new PayloadCollector();
			_scenarioContext.Set(payloadCollector);

			var handler = new MockHttpMessageHandler();
			handler.When(BuildIntakeV2EventsAbsoluteUrl(configuration.ServerUrl).AbsoluteUri)
				.Respond(r =>
				{
					payloadCollector.ProcessPayload(r);
					return new HttpResponseMessage(HttpStatusCode.OK);
				});

			var environmentVariables = new TestEnvironmentVariables();
			_scenarioContext.Set(environmentVariables);
			_scenarioContext.Set(() =>
			{
				var payloadSender = new PayloadSenderV2(
					logger,
					configuration,
					Service.GetDefaultService(configuration, new NoopLogger()),
					new Api.System(),
					MockApmServerInfo.Version710,
					handler,
					environmentVariables: environmentVariables);

				return new ApmAgent(new TestAgentComponents(logger, configuration, payloadSender));
			});
		}

		[Then(@"the User-Agent header matches regex '([^']*)'")]
		public void ThenTheUser_AgentHeaderMatchesRegex(string p0)
		{
			var regex = new Regex(p0);
			var payloadCollector = _scenarioContext.Get<PayloadCollector>();
			var agent = _scenarioContext.Get<ApmAgent>();

			// capture a transaction so that the agent sends a payload
			agent.Tracer.CaptureTransaction("User agent", "app", () => { });
			payloadCollector.Wait();

			var userAgent = payloadCollector.Payloads.First().Headers.UserAgent;
			userAgent.Should().NotBeNull().And.HaveCountGreaterOrEqualTo(1);

			var values = string.Join(" ", userAgent.Select(u => u.ToString()));
			regex.IsMatch(values).Should().BeTrue($"user agent values {values} should match {p0}");
		}

	}
}
