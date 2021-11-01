// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elastic.Apm.Libraries.Newtonsoft.Json;
using Elastic.Apm.Libraries.Newtonsoft.Json.Linq;

namespace Elastic.Apm.Cloud
{
	/// <summary>
	/// Provides cloud metadata for Microsoft Azure VM
	/// </summary>
	internal class AzureCloudMetadataProvider : ICloudMetadataProvider
	{
		internal const string MetadataUri = "http://169.254.169.254/metadata/instance/compute?api-version=2019-08-15";
		internal const string Name = "azure";

		private readonly HttpMessageHandler _handler;
		private readonly IApmLogger _logger;

		internal AzureCloudMetadataProvider(IApmLogger logger, HttpMessageHandler handler)
		{
			_handler = handler;
			_logger = logger.Scoped(nameof(AzureCloudMetadataProvider));
		}

		public AzureCloudMetadataProvider(IApmLogger logger) : this(logger, new HttpClientHandler())
		{
		}

		/// <inheritdoc />
		public string Provider { get; } = Name;

		/// <inheritdoc />
		public async Task<Api.Cloud> GetMetadataAsync()
		{
			var client = new HttpClient(_handler, false) { Timeout = TimeSpan.FromSeconds(3) };
			try
			{
				JObject metadata;
				using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, MetadataUri))
				{
					requestMessage.Headers.Add("Metadata", "true");
					var responseMessage = await client.SendAsync(requestMessage).ConfigureAwait(false);

					using var stream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
					using var streamReader = new StreamReader(stream, Encoding.UTF8);
					using var jsonReader = new JsonTextReader(streamReader);

					var serializer = new JsonSerializer();
					metadata = serializer.Deserialize<JObject>(jsonReader);
				}

				return new Api.Cloud
				{
					Account = new CloudAccount { Id = metadata["subscriptionId"].Value<string>() },
					Instance = new CloudInstance { Id = metadata["vmId"].Value<string>(), Name = metadata["name"].Value<string>() },
					Project = new CloudProject { Name = metadata["resourceGroupName"].Value<string>() },
					AvailabilityZone = metadata["zone"]?.Value<string>(),
					Machine = new CloudMachine { Type = metadata["vmSize"].Value<string>() },
					Provider = Provider,
					Region = metadata["location"].Value<string>()
				};
			}
			catch (Exception e)
			{
				_logger.Trace()?.LogException(
					e,
					"Unable to get {Provider} cloud metadata. The application is likely not running in {Provider}", Provider, Provider);
				return null;
			}
		}
	}
}
