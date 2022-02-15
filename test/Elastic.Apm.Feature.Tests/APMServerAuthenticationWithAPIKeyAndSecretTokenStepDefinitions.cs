// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using Elastic.Apm.Feature.Tests.FeatureContexts;
using TechTalk.SpecFlow;

namespace Elastic.Apm.Feature.Tests
{
    [Binding]
    public class APMServerAuthenticationWithAPIKeyAndSecretTokenStepDefinitions
	{
		private readonly ApiKeyFeatureContext _apiKeyFeatureContext;

		public APMServerAuthenticationWithAPIKeyAndSecretTokenStepDefinitions(ApiKeyFeatureContext apiKeyFeatureContext) => _apiKeyFeatureContext = apiKeyFeatureContext;


		[Given(@"an agent configured with")]
		[Scope(Feature = "APM server authentication with API key and secret token")]
		public void GivenAnAgentConfiguredWith(Table table)
		{
			_apiKeyFeatureContext.New();
			foreach (var row in table.Rows)
			{
				if (row[0] == "secret_token")
					_apiKeyFeatureContext.SetSecretToken(row[1]);
				if (row[0] == "api_key")
					_apiKeyFeatureContext.SetApiKey(row[1]);
			}
		}

		[When(@"the agent sends a request to APM server")]
		public void WhenTheAgentSendsARequestToAPMServer() { }

		[Then(@"the Authorization header of the request is '([^']*)'")]
		public async Task ThenTheAuthorizationHeaderOfTheRequestIs(string p0) => await _apiKeyFeatureContext.CheckAuthorizationHeader(p0);

	}
}
