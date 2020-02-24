using System.Threading.Tasks;
using Elastic.Apm.Feature.Tests.FeatureContexts;
using TechTalk.SpecFlow;

namespace Elastic.Apm.Feature.Tests
{
	[Binding]
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

		[When(@"^a secret_token is set in the config$")]
		public void WhenSecretTokenIsSet() => _apiKeyFeatureContext.SetSecretToken(SecretToken);

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
