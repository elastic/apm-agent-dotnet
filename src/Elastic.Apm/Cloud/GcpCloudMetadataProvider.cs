// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Globalization;
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
	/// Provides cloud metadata for Google Cloud Platform (GCP)
	/// </summary>
	internal class GcpCloudMetadataProvider : ICloudMetadataProvider
	{
		internal const string MetadataUri = "http://metadata.google.internal/computeMetadata/v1/?recursive=true";
		internal const string Name = "gcp";

		private readonly HttpMessageHandler _handler;
		private readonly IApmLogger _logger;

		internal GcpCloudMetadataProvider(IApmLogger logger, HttpMessageHandler handler)
		{
			_handler = handler;
			_logger = logger.Scoped(nameof(GcpCloudMetadataProvider));
		}

		public GcpCloudMetadataProvider(IApmLogger logger) : this(logger, new HttpClientHandler())
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
					requestMessage.Headers.Add("Metadata-Flavor", "Google");
					var responseMessage = await client.SendAsync(requestMessage).ConfigureAwait(false);

					using var stream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
					metadata = PayloadItemSerializer.Default.Deserialize<JsonObject>(stream);
				}

				var zoneParts = metadata["instance"]?["zone"]?.GetValue<string>()?.Split('/');
				var availabilityZone = zoneParts is { Length: > 0 } ? zoneParts[zoneParts.Length - 1] : null;

				var lastHyphen = availabilityZone?.LastIndexOf('-') ?? -1;
				var region = availabilityZone != null && lastHyphen > -1
					? availabilityZone.Substring(0, lastHyphen)
					: availabilityZone;

				var machineTypeParts = metadata["instance"]?["machineType"]?.GetValue<string>()?.Split('/');
				var machineType = machineTypeParts is { Length: > 0 } ? machineTypeParts[machineTypeParts.Length - 1] : null;
				var instanceId = metadata["instance"]?["id"]?.GetValue<long>().ToString(CultureInfo.InvariantCulture);
				var instanceName = metadata["instance"]?["name"]?.GetValue<string>();
				var projectId = metadata["project"]?["projectId"]?.GetValue<string>();

				return new Api.Cloud
				{
					Instance =
						new CloudInstance
						{
							Id = instanceId,
							Name = instanceName
						},
					Project = new CloudProject
					{
						Id = projectId
					},
					AvailabilityZone = availabilityZone,
					Machine = new CloudMachine { Type = machineType },
					Provider = Provider,
					Region = region
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
