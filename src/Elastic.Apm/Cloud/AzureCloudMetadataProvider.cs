// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elastic.Apm.Report.Serialization;

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
				JsonObject metadata;
				using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, MetadataUri))
				{
					requestMessage.Headers.Add("Metadata", "true");
					var responseMessage = await client.SendAsync(requestMessage).ConfigureAwait(false);

					using var stream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
					metadata = PayloadItemSerializer.Default.Deserialize<JsonObject>(stream);
				}

				return new Api.Cloud
				{
					Account = new CloudAccount { Id = metadata["subscriptionId"]?.GetValue<string>() },
					Instance = new CloudInstance { Id = metadata["vmId"]?.GetValue<string>(), Name = metadata["name"]?.GetValue<string>() },
					Project = new CloudProject { Name = metadata["resourceGroupName"]?.GetValue<string>() },
					AvailabilityZone = metadata["zone"]?.GetValue<string>(),
					Machine = new CloudMachine { Type = metadata["vmSize"]?.GetValue<string>() },
					Provider = Provider,
					Region = metadata["location"]?.GetValue<string>()
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
