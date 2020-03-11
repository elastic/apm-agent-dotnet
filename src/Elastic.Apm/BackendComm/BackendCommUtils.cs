using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
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

		internal static class ApmServerEndpoints
		{
			/// <summary>
			/// Builds the absolute URL that points to APM server's intake API endpoint which is used by agents to send events.
			/// </summary>
			/// <param name="baseUrl">Absolute URL pointing to APM Server's base for API endpoints.</param>
			internal static Uri BuildIntakeV2EventsAbsoluteUrl(Uri baseUrl) =>
				CombineAbsoluteAndRelativeUrls(baseUrl, "intake/v2/events");

			/// <summary>
			/// Builds the absolute URL that points to APM server's central-config API endpoint which is used by agents to fetch configuration.
			/// Configuration is selected by the backend based on the agent's service.name and service.environment.
			/// </summary>
			/// <param name="baseUrl">Absolute URL pointing to APM Server's base for API endpoints.</param>
			/// <param name="service">Service info to pass to APM Server.
			/// service.name and service.environment are URL encoded in the returned URL.</param>
			internal static Uri BuildGetConfigAbsoluteUrl(Uri baseUrl, Service service)
			{
				var strBuilder = new StringBuilder("config/v1/agents");
				var prefix = '?';

				if (service.Name != null)
				{
					strBuilder.Append(prefix).Append($"service.name={UrlEncode(service.Name)}");
					prefix = '&';
				}

				if (service.Environment != null)
					strBuilder.Append(prefix).Append($"service.environment={UrlEncode(service.Environment)}");

				return CombineAbsoluteAndRelativeUrls(baseUrl, /* relativeUri: */ strBuilder.ToString());
			}

			/// <summary>
			/// Credit: System.Net.Http.FormUrlEncodedContent.Encode
			/// https://github.com/dotnet/corefx/blob/450f49a1a80663529b31d3defafbd5e59822a16a/src/System.Net.Http/src/System/Net/Http/FormUrlEncodedContent.cs#L53
			/// </summary>
			private static string UrlEncode(string decodedStr)
			{
				decodedStr.ThrowIfArgumentNull(nameof(decodedStr));

				return decodedStr.IsEmpty() ? string.Empty : Uri.EscapeDataString(decodedStr).Replace("%20", "+");
			}

			private static Uri CombineAbsoluteAndRelativeUrls(Uri baseAbsoluteUrl, string relativeUrl)
			{
				if (!baseAbsoluteUrl.IsAbsoluteUri)
				{
					throw new ArgumentException( /* message: */ $"{nameof(baseAbsoluteUrl)} should be an absolute URL."
						+ $" {nameof(baseAbsoluteUrl)}: `{baseAbsoluteUrl}'."
						, /* paramName: */ nameof(baseAbsoluteUrl));
				}

				// We need to make sure base URL ends with slash because according to
				// https://docs.microsoft.com/en-us/dotnet/api/system.uri.-ctor?view=netstandard-2.0#System_Uri__ctor_System_Uri_System_String_
				// If the baseUri has relative parts (like /api), then the relative part must be terminated with a slash, (like /api/),
				//    if the relative part of baseUri is to be preserved in the constructed Uri.
				var baseAbsoluteUrlAdapted = baseAbsoluteUrl;
				var baseAbsoluteUrlAsStr = baseAbsoluteUrl.ToString();
				if (!baseAbsoluteUrlAsStr.EndsWith("/")) baseAbsoluteUrlAdapted = new Uri(baseAbsoluteUrlAsStr + "/", UriKind.Absolute);
				return new Uri(baseAbsoluteUrlAdapted, relativeUrl);
			}
		}

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

		private static HttpClientHandler CreateHttpClientHandler(bool verifyServerCert, IApmLogger logger)
		{
			bool ServerCertificateCustomValidationCallback(HttpRequestMessage message, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors policyError)
			{
				if (policyError == SslPolicyErrors.None) return true;

				logger.Trace()?.Log("Certificate validation failed. Policy error {PolicyError}", policyError);
				return !verifyServerCert;
			}

			return new HttpClientHandler { ServerCertificateCustomValidationCallback = ServerCertificateCustomValidationCallback };
		}

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
			var httpClient =
				new HttpClient(httpMessageHandler ?? CreateHttpClientHandler(config.VerifyServerCert, loggerArg)) { BaseAddress = serverUrlBase };
			httpClient.DefaultRequestHeaders.UserAgent.Add(
				new ProductInfoHeaderValue($"elasticapm-{Consts.AgentName}", AdaptUserAgentValue(service.Agent.Version)));
			httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("System.Net.Http",
				AdaptUserAgentValue(typeof(HttpClient).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version)));
			httpClient.DefaultRequestHeaders.UserAgent.Add(
				new ProductInfoHeaderValue(AdaptUserAgentValue(service.Runtime.Name), AdaptUserAgentValue(service.Runtime.Version)));

			if (config.ApiKey != null)
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", config.ApiKey);
			else if (config.SecretToken != null)
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.SecretToken);

			return httpClient;

			// Replace invalid characters by underscore. All invalid characters can be found at
			// https://github.com/dotnet/corefx/blob/e64cac6dcacf996f98f0b3f75fb7ad0c12f588f7/src/System.Net.Http/src/System/Net/Http/HttpRuleParser.cs#L41
			string AdaptUserAgentValue(string value)
			{
				return Regex.Replace(value, "[ /()<>@,:;={}?\\[\\]\"\\\\]", "_");
			}
		}
	}
}
