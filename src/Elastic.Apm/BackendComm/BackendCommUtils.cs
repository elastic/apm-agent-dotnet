using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.BackendComm
{
	internal class BackendCommUtils
	{
		private const string ThisClassName = nameof(BackendCommUtils);

		private static readonly LazyContextualInit ConfigServicePointOnceHelper = new LazyContextualInit();

		private static readonly TimeSpan DnsTimeout = TimeSpan.FromMinutes(1);

		private static void ConfigServicePoint(Uri serverUrlBase, IApmLogger logger) =>
			ConfigServicePointOnceHelper.IfNotInited?.Init(() =>
			{
				var servicePoint = ServicePointManager.FindServicePoint(serverUrlBase);

				try
				{
					servicePoint.ConnectionLeaseTimeout = (int)DnsTimeout.TotalMilliseconds;
				}
				catch (Exception e)
				{
					logger.Warning()
						?.LogException(e,
							"Failed setting servicePoint.ConnectionLeaseTimeout - default ConnectionLeaseTimeout from HttpClient will be used. "
							+ "Unless you notice connection issues between the APM Server and the agent, no action needed.");
				}

				servicePoint.ConnectionLimit = 20;
			});


		internal static HttpClient BuildHttpClient(IApmLogger loggerArg, IConfigSnapshot config, Service service, string dbgCallerDesc
			, HttpMessageHandler httpMessageHandler = null
		)
		{
			var logger = loggerArg.Scoped(ThisClassName);

			var serverUrlBase = config.ServerUrls.First();
			ConfigServicePoint(serverUrlBase, loggerArg);

			logger.Debug()
				?.Log("Building HTTP client with BaseAddress: {ApmServerUrl} for {dbgCallerDesc}..."
					, serverUrlBase, dbgCallerDesc);
			var httpClient = new HttpClient(httpMessageHandler ?? new HttpClientHandler()) { BaseAddress = serverUrlBase };
			httpClient.DefaultRequestHeaders.UserAgent.Add(
				new ProductInfoHeaderValue($"elasticapm-{Consts.AgentName}", AdaptUserAgentValue(service.Agent.Version)));
			httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("System.Net.Http",
				AdaptUserAgentValue(typeof(HttpClient).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version)));
			httpClient.DefaultRequestHeaders.UserAgent.Add(
				new ProductInfoHeaderValue(AdaptUserAgentValue(service.Runtime.Name), AdaptUserAgentValue(service.Runtime.Version)));

			if (config.SecretToken != null)
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.SecretToken);

			return httpClient;

			// Replace invalid characters by underscore. All invalid characters can be found at
			// https://github.com/dotnet/corefx/blob/e64cac6dcacf996f98f0b3f75fb7ad0c12f588f7/src/System.Net.Http/src/System/Net/Http/HttpRuleParser.cs#L41
			string AdaptUserAgentValue(string value)
			{
				return Regex.Replace(value, "[ /()<>@,:;={}?\\[\\]\"\\\\]", "_");
			}
		}

		internal static class ApmServerEndpoints
		{
			internal static string IntakeV2Events = "intake/v2/events";

			internal static string GetConfig(Service service)
			{
				var strBuilder = new StringBuilder("/config/v1/agents");
				var prefix = '?';

				if (service.Name != null)
				{
					strBuilder.Append(prefix).Append($"service.name={service.Name}");
					prefix = '&';
				}

				if (service.Environment != null) strBuilder.Append(prefix).Append($"service.environment={service.Environment}");

				return strBuilder.ToString();
			}
		}
	}
}
