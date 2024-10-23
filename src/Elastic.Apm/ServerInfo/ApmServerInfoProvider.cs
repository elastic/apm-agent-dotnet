// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Report.Serialization;

namespace Elastic.Apm.ServerInfo
{
	internal class ApmServerInfoProvider
	{
		internal static async Task FillApmServerInfo(IApmServerInfo apmServerInfo, IApmLogger logger, IConfiguration configuration,
			HttpClient httpClient, Action<bool, IApmServerInfo> callbackOnFinish
		)
		{
			try
			{
				using var requestMessage = new HttpRequestMessage(HttpMethod.Get, configuration.ServerUrl);
				requestMessage.Headers.Add("Metadata", "true");
				requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

				var responseMessage = await httpClient.SendAsync(requestMessage).ConfigureAwait(false);

				if (responseMessage.IsSuccessStatusCode)
				{
					using var stream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);

					var metadata = PayloadItemSerializer.Default.Deserialize<JsonObject>(stream);
					var version = metadata?["version"];
					var strVersion = version?.GetValue<string>();
					if (strVersion != null)
					{
						try
						{
							apmServerInfo.Version = new ElasticVersion(strVersion);
							callbackOnFinish?.Invoke(true, apmServerInfo);
						}
						catch (Exception e)
						{
							logger.Warning()?.LogException(e, "Failed parsing APM Server version - version string: {VersionString}", strVersion);
							callbackOnFinish?.Invoke(false, apmServerInfo);
						}
					}
					else
					{
						logger.Warning()?.Log("Failed parsing APM Server version - version string not available");
						callbackOnFinish?.Invoke(false, apmServerInfo);
					}
				}
				else
				{
					logger.Warning()?.Log("Failed reading APM server info, response from server: {ResponseCode}", responseMessage.StatusCode);
					callbackOnFinish?.Invoke(false, apmServerInfo);
				}
			}
			catch (Exception e)
			{
				logger.Warning()?.LogException(e, "Failed reading APM server info");
				callbackOnFinish?.Invoke(false, apmServerInfo);
			}
		}
	}
}
