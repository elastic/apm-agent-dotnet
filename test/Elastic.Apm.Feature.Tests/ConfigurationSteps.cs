// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using TechTalk.SpecFlow;

namespace Elastic.Apm.Feature.Tests
{
	[Binding]
	public class ConfigurationSteps
	{
		private readonly ScenarioContext _scenarioContext;

		public ConfigurationSteps(ScenarioContext scenarioContext) => _scenarioContext = scenarioContext;

		[When("^service name is not set$")]
		public void WhenServiceNameIsNotSet() { }

		[When("^service version is not set$")]
		public void WhenServiceVersionIsNotSet() { }

		[When("^service name is set to '(.*?)'$")]
		public void WhenServiceNameIsSetTo(string name)
		{
			var configuration = _scenarioContext.Get<TestConfiguration>();
			configuration.ServiceName = name;
		}

		[When("^service version is set to '(.*?)'$")]
		public void WhenServiceVersionIsSetTo(string version)
		{
			var configuration = _scenarioContext.Get<TestConfiguration>();
			configuration.ServiceVersion = version;
		}
	}
}
