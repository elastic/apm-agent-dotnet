// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using Elastic.Apm.Feature.Tests.FeatureContexts;
using TechTalk.SpecFlow;

namespace Elastic.Apm.Feature.Tests
{
	[Binding]
	[Scope(Feature = "API Key.")]
	public class ApiKeySteps
	{
		private readonly ApiKeyFeatureContext _apiKeyFeatureContext;

		private const string SecretToken = "12345";
		private const string ApiKey = "abcd:efgh";

		public ApiKeySteps(ApiKeyFeatureContext apiKeyFeatureContext) => _apiKeyFeatureContext = apiKeyFeatureContext;

		[Given(@"^an agent$")]
		public void GetAgent() => _apiKeyFeatureContext.New();

		[When(@"^an api key is set to '(.*)' in the config$")]
		public void WhenApiKeyIsSetTo(string apiKey) => _apiKeyFeatureContext.SetApiKey(apiKey);

		[When(@"^an api key is set in the config$")]
		public void WhenApiKeyIsSet() => _apiKeyFeatureContext.SetApiKey(ApiKey);

		[When(@"a secret_token is set to '(.*)' in the config")]
		public void WhenSecretTokenIsSet(string secretToken) => _apiKeyFeatureContext.SetSecretToken(secretToken);

		[When(@"^an api key is not set in the config$")]
		public void WhenApiKeyIsNotSet() { }

		[Then(@"^the Authorization header is '(.*)'$")]
		public async Task ThenAuthorizationHeaderIs(string authorizationHeader) =>
			await _apiKeyFeatureContext.CheckAuthorizationHeader(authorizationHeader);

		[Then(@"^the secret token is sent in the Authorization header$")]
		public async Task ThenSecretTokenIsSentInAuthorizationHeader() =>
			await _apiKeyFeatureContext.CheckAuthorizationHeader($"Bearer {SecretToken}");

		[Then(@"^the api key is sent in the Authorization header$")]
		public async Task ThenApiKeyIsSentInAuthorizationHeader() => await _apiKeyFeatureContext.CheckAuthorizationHeader($"ApiKey {ApiKey}");
	}
}
