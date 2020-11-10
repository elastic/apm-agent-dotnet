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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elastic.Apm.ServerInfo
{
	internal class ServerInfo : IServerInfo
	{
		public ServerInfo(IConfigSnapshot configSnapshot, IApmLogger logger) => (_configSnapshot, _logger) = (configSnapshot, logger);

		private readonly IConfigSnapshot _configSnapshot;
		private readonly IApmLogger _logger;


		private volatile bool _initialized;
		public bool Initialized => _initialized;
		public Version Version { get; set; }

		public async Task GetServerInfoAsync()
		{
			try
			{


				var httpClient = new HttpClient();
				var result = await httpClient.GetAsync(_configSnapshot.ServerUrls[0]).ConfigureAwait(false);

				if (result.IsSuccessStatusCode)
				{
					using var stream = await result.Content.ReadAsStreamAsync().ConfigureAwait(false);
					using var streamReader = new StreamReader(stream, Encoding.UTF8);
					using var jsonReader = new JsonTextReader(streamReader);

					var serializer = new JsonSerializer();
					var metadata = serializer.Deserialize<JObject>(jsonReader);
					var version = metadata["version"];
					var strVersion = version?.Value<string>();
					if (strVersion != null)
					{
						var versionStrArray = strVersion.Split('.');
						if (versionStrArray.Length >= 3)
						{
							if (int.TryParse(versionStrArray[0], out var major) && int.TryParse(versionStrArray[1], out var minor)
								&& int.TryParse(versionStrArray[2], out var patch))
								Version = new Version(major, minor, patch);
						}
					}
				}
			}
			catch (Exception e)
			{
				_logger.Warning()?.LogException(e, "Failed reading APM Server info");
			}
			finally
			{
				_initialized = true;
			}
		}
	}
}
