// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using TechTalk.SpecFlow;

namespace Elastic.Apm.Feature.Tests
{
	[Binding]
	public class UserAgentSteps
	{
		private readonly ScenarioContext _scenarioContext;

		public UserAgentSteps(ScenarioContext scenarioContext) => _scenarioContext = scenarioContext;

		[Then("^the User-Agent header matches regex '(.*?)'$")]
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
	}
}
