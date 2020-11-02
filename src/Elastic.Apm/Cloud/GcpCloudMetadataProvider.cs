// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

		internal GcpCloudMetadataProvider(HttpMessageHandler handler, IApmLogger logger)
		{
			_handler = handler;
			_logger = logger.Scoped(nameof(GcpCloudMetadataProvider));
		}

		public GcpCloudMetadataProvider(IApmLogger logger) : this(new HttpClientHandler(), logger)
		{
		}

		/// <inheritdoc />
		public string Provider { get; } = Name;

		/// <inheritdoc />
		public async Task<Api.Cloud> GetMetadataAsync()
		{
			var client = new HttpClient(_handler, false);
			try
			{
				JObject metadata;
				using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, MetadataUri))
				{
					requestMessage.Headers.Add("Metadata-Flavor", "Google");
					using var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
					var responseMessage = await client.SendAsync(requestMessage, tokenSource.Token).ConfigureAwait(false);

					using var stream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
					using var streamReader = new StreamReader(stream, Encoding.UTF8);
					using var jsonReader = new JsonTextReader(streamReader);

					var serializer = new JsonSerializer();
					metadata = serializer.Deserialize<JObject>(jsonReader);
				}

				var availabilityZone = Path.GetFileName(metadata["instance"]["zone"].Value<string>());

				var lastHyphen = availabilityZone.LastIndexOf('-');
				var region = lastHyphen > -1
					? availabilityZone.Substring(0, lastHyphen)
					: availabilityZone;

				return new Api.Cloud
				{
					Instance =
						new CloudInstance
						{
							Id = metadata["instance"]["id"].Value<long>().ToString(CultureInfo.InvariantCulture),
							Name = metadata["instance"]["name"].Value<string>()
						},
					Project = new CloudProject
					{
						Id = metadata["project"]["numericProjectId"].Value<long>().ToString(CultureInfo.InvariantCulture),
						Name = metadata["project"]["projectId"].Value<string>()
					},
					AvailabilityZone = availabilityZone,
					Machine = new CloudMachine { Type = metadata["instance"]["machineType"].Value<string>() },
					Provider = Provider,
					Region = region
				};
			}
			catch (Exception e)
			{
				_logger.Warning()?.LogException(e, "Unable to get {Provider} cloud metadata", Provider);
				return null;
			}
		}
	}
}
