// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Libraries.Newtonsoft.Json;
using Elastic.Apm.Libraries.Newtonsoft.Json.Linq;

namespace Elastic.Apm.ServerInfo
{
	internal class ApmServerInfoProvider
	{
		internal static async Task FillApmServerInfo(IApmServerInfo apmServerInfo, IApmLogger logger, IConfigurationSnapshot configurationSnapshot,
			HttpClient httpClient
		)
		{
			try
			{
				using var requestMessage = new HttpRequestMessage(HttpMethod.Get, configurationSnapshot.ServerUrl);
				requestMessage.Headers.Add("Metadata", "true");

				var responseMessage = await httpClient.SendAsync(requestMessage).ConfigureAwait(false);

				if (responseMessage.IsSuccessStatusCode)
				{
					using var stream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
					using var streamReader = new StreamReader(stream, Encoding.UTF8);
					using var jsonReader = new JsonTextReader(streamReader);

					var serializer = new JsonSerializer();
					var metadata = serializer.Deserialize<JObject>(jsonReader);
					var version = metadata["version"];
					var strVersion = version?.Value<string>();
					if (strVersion != null)
					{
						try
						{
							apmServerInfo.Version = new ElasticVersion(strVersion);
						}
						catch (Exception e)
						{
							logger.Warning()?.LogException(e, "Failed parsing APM Server version - version string: {VersionString}", strVersion);
						}
					}
				}
				else
					logger.Warning()?.Log("Failed reading APM server info, response from server: {ResponseCode}", responseMessage.StatusCode);
			}
			catch (Exception e)
			{
				logger.Warning()?.LogException(e, "Failed reading APM server info");
			}
		}
	}
}
