using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Elastic.Apm.Model;
using Elastic.Apm.Report;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;

namespace Elastic.Apm.Feature.Tests.FeatureContexts
{
	public class ApiKeyFeatureContext
	{
		private string _secretToken;
		private string _apiKey;

		public void New()
		{
			_secretToken = null;
			_apiKey = null;
		}

		public void SetApiKey(string apiKey) => _apiKey = apiKey;

		public void SetSecretToken(string secretToken) => _secretToken = secretToken;

		public async Task CheckAuthorizationHeader(string authorizationHeader)
		{
			var isRequestFinished = new TaskCompletionSource<object>();

			AuthenticationHeaderValue authHeader = null;
			var handler = new MockHttpMessageHandler((r, c) =>
			{
				authHeader = r.Headers.Authorization;
				isRequestFinished.SetResult(null);
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
			});

			var logger = new NoopLogger();
			var mockConfig = new MockConfigSnapshot(logger, secretToken: _secretToken, apiKey: _apiKey, flushInterval: "1s");
			var payloadSender = new PayloadSenderV2(logger, mockConfig,
				Api.Service.GetDefaultService(mockConfig, logger), new Api.System(), handler, /* dbgName: */ nameof(ApiKeyFeatureContext));

			using (var agent = new ApmAgent(new TestAgentComponents(logger, mockConfig, payloadSender)))
			{
				agent.PayloadSender.QueueTransaction(new Transaction(agent, "TestName", "TestType"));
				await isRequestFinished.Task;
			}

			authHeader.Should().NotBeNull();
			authHeader.ToString().Should().Be(authorizationHeader);
		}
	}
}
