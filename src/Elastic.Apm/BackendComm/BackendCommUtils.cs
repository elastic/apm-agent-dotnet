// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
#if !NET462
using System.Security.Authentication;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
#endif
using System.Text;
using System.Text.RegularExpressions;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.BackendComm
{
	internal static class BackendCommUtils
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
			/// Builds the absolute URL that points to APM server information API endpoint
			/// </summary>
			/// <param name="baseUrl">Absolute URL pointing to APM Server's base for API endpoints.</param>
			internal static Uri BuildApmServerInformationUrl(Uri baseUrl) =>
				CombineAbsoluteAndRelativeUrls(baseUrl, "/");

			/// <summary>
			/// Builds the absolute URL that points to APM server's central-config API endpoint which is used by agents to fetch configuration.
			/// Configuration is selected by the backend based on the agent's service.name and service.environment.
			/// </summary>
			/// <param name="baseUrl">Absolute URL pointing to APM Server's base for API endpoints.</param>
			/// <param name="service">Service info to pass to APM Server.
			/// service.name and service.environment are URL encoded in the returned URL.</param>
			internal static Uri BuildGetConfigAbsoluteUrl(Uri baseUrl, Service service)
			{
				var builder = new StringBuilder("config/v1/agents");
				var prefix = '?';

				if (service.Name != null)
				{
					builder.Append(prefix).Append("service.name=").Append(UrlEncode(service.Name));
					prefix = '&';
				}

				if (service.Environment != null)
					builder.Append(prefix).Append("service.environment=").Append(UrlEncode(service.Environment));

				return CombineAbsoluteAndRelativeUrls(baseUrl, builder.ToString());
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
				if (!baseAbsoluteUrlAsStr.EndsWith("/"))
					baseAbsoluteUrlAdapted = new Uri(baseAbsoluteUrlAsStr + "/", UriKind.Absolute);
				return new Uri(baseAbsoluteUrlAdapted, relativeUrl);
			}
		}

		private static void ConfigServicePoint(Uri serverUrlBase, IApmLogger logger) =>
			ConfigServicePointOnceHelper.IfNotInited?.Init(() =>
			{
				// ServicePointManager is obsolete
#pragma warning disable SYSLIB0014
				var servicePoint = ServicePointManager.FindServicePoint(serverUrlBase);
#pragma warning restore SYSLIB0014
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

		private static HttpClientHandler CreateHttpClientHandler(IConfiguration configuration, IApmLogger logger)
		{
#if NET462
			try
			{
				var systemNetHttpVersion = typeof(HttpClientHandler).Assembly.GetName().Version;
				logger.Trace()?.Log("System.Net.Http assembly version if {Version}.", systemNetHttpVersion);
			}
			catch (Exception ex)
			{
				logger.Error()?.LogException(ex, "Could not determine the assembly version of System.Net.Http.");
			}
#endif

#if !NET462
			Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> serverCertificateCustomValidationCallback = null;

			if (!configuration.VerifyServerCert)
			{
				serverCertificateCustomValidationCallback = (_, _, _, policyError) =>
				{
					if (policyError == SslPolicyErrors.None)
						return true;

					logger.Trace()?.Log("Certificate validation failed. Policy error {PolicyError}", policyError);
					return true;
				};
			}
			else if (!string.IsNullOrEmpty(configuration.ServerCert))
			{
				try
				{
					var serverCertificate = new X509Certificate2(configuration.ServerCert);
					var publicKey = serverCertificate.GetPublicKeyString();

					serverCertificateCustomValidationCallback = (_, certificate, _, policyError) =>
					{
						if (policyError == SslPolicyErrors.None)
							return true;

						if (certificate is null)
						{
							logger.Trace()?.Log("Certificate validation failed. No certificate to validate");
							return false;
						}

						var publicKeyToValidate = certificate.GetPublicKeyString();
						if (string.Equals(publicKey, publicKeyToValidate, StringComparison.Ordinal))
							return true;

						logger.Trace()
							?.Log(
								"Certificate validation failed. Public key {PublicKey} does not match {ServerCert} public key",
								publicKeyToValidate,
								nameof(configuration.ServerCert));

						return false;
					};
				}
				catch (Exception e)
				{
					logger.Error()
						?.LogException(
							e,
							"Could not configure {ConfigServerCert} at path {Path} for certificate pinning",
							nameof(configuration.ServerCert),
							configuration.ServerCert);
				}
			}
			else
			{
				// set a default callback to log the policy error
				serverCertificateCustomValidationCallback = (_, _, _, policyError) =>
				{
					if (policyError == SslPolicyErrors.None)
						return true;

					logger.Trace()?.Log("Certificate validation failed. Policy error {PolicyError}", policyError);
					return false;
				};
			}
#endif

			var httpClientHandler = new HttpClientHandler
			{
				UseDefaultCredentials = configuration.UseWindowsCredentials
			};

			// Due to the potential for binding issues (e.g.https://github.com/dotnet/runtime/issues/29314)
			// and runtime exceptions on .NET Framework versions <4.7.2, we don't attempt to set the certificate
			// validation callback or SSL protocols on net462, which should also apply to .NET Framework <4.7.2 runtimes
			// which resolve to that target.
#if !NET462
			httpClientHandler.ServerCertificateCustomValidationCallback = serverCertificateCustomValidationCallback;
			logger.Info()?.Log("CreateHttpClientHandler - Setting ServerCertificateCustomValidationCallback");

			httpClientHandler.SslProtocols |= SslProtocols.Tls12;
			logger.Info()?.Log($"CreateHttpClientHandler - SslProtocols: {httpClientHandler.SslProtocols}");
#else
			// We don't set the ServerCertificateCustomValidationCallback on ServicePointManager here as it would
			// apply to the whole AppDomain and that may not be desired. A consumer can set this themselves if they
			// need custom validation behaviour.
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			logger.Info()?.Log($"CreateHttpClientHandler - SslProtocols: {ServicePointManager.SecurityProtocol}");
#endif
			return httpClientHandler;
		}

		internal static HttpClient BuildHttpClient(IApmLogger loggerArg, IConfiguration configuration, Service service, string dbgCallerDesc
			, HttpMessageHandler httpMessageHandler = null
		)
		{
			var logger = loggerArg.Scoped(ThisClassName);

			var serverUrlBase = configuration.ServerUrl;
			ConfigServicePoint(serverUrlBase, loggerArg);

			logger.Debug()
				?.Log("Building HTTP client with BaseAddress: {ApmServerUrl} for {dbgCallerDesc}..."
					, serverUrlBase.Sanitize(), dbgCallerDesc);
			var httpClient =
				new HttpClient(httpMessageHandler ?? CreateHttpClientHandler(configuration, loggerArg)) { BaseAddress = serverUrlBase };
			httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(GetUserAgent(service));
			httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("System.Net.Http",
				AdaptUserAgentValue(typeof(HttpClient).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version)));

			if (service.Runtime != null)
			{
				try
				{
					var name = AdaptUserAgentValue(service.Runtime.Name);
					var version = AdaptUserAgentValue(service.Runtime.Version);
					//Mono uses a more complex version number e.g
					//6.12.0.182 (2020-02/6051b710727 Tue Jun 14 15:01:21 EDT 2022)
					if (name.Equals("mono", StringComparison.InvariantCultureIgnoreCase))
						version = version.Split(new[] { "__" }, StringSplitOptions.None).FirstOrDefault();


					httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(name, version));
				}
				catch (Exception e)
				{
					logger.Warning()?.LogException(e, "Failed setting user agent header");
				}
			}

			if (configuration.ApiKey != null)
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", configuration.ApiKey);
			else if (configuration.SecretToken != null)
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", configuration.SecretToken);

			return httpClient;

			// Replace invalid characters by underscore. All invalid characters can be found at
			// https://github.com/dotnet/corefx/blob/e64cac6dcacf996f98f0b3f75fb7ad0c12f588f7/src/System.Net.Http/src/System/Net/Http/HttpRuleParser.cs#L41
		}

		private static string GetUserAgent(Service service)
		{
			var value = $"apm-agent-{Consts.AgentName}/{AdaptUserAgentValue(service.Agent.Version)}";
			if (!string.IsNullOrEmpty(service.Name))
			{
				value += !string.IsNullOrEmpty(service.Version)
					? $" ({AdaptUserAgentValue(service.Name)} {AdaptUserAgentValue(service.Version)})"
					: $" ({AdaptUserAgentValue(service.Name)})";
			}
			return value;
		}

		private static string AdaptUserAgentValue(string value) => Regex.Replace(value, "[ /()<>@,={}?\\[\\]\"\\\\]", "_");
	}
}
