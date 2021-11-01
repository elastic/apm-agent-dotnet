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
	/// Provides cloud metadata for Amazon Web Services (AWS)
	/// </summary>
	internal class AwsCloudMetadataProvider : ICloudMetadataProvider
	{
		internal const string TokenUri = "http://169.254.169.254/latest/api/token";
		internal const string MetadataUri = "http://169.254.169.254/latest/dynamic/instance-identity/document";
		internal const string Name = "aws";

		private readonly HttpMessageHandler _handler;
		private readonly IApmLogger _logger;

		public AwsCloudMetadataProvider(IApmLogger logger) : this(logger, new HttpClientHandler())
		{
		}

		internal AwsCloudMetadataProvider(IApmLogger logger, HttpMessageHandler handler)
		{
			_handler = handler;
			_logger = logger.Scoped(nameof(AwsCloudMetadataProvider));
		}

		/// <inheritdoc />
		public string Provider { get; } = Name;

		/// <inheritdoc />
		public async Task<Api.Cloud> GetMetadataAsync()
		{
			var client = new HttpClient(_handler, false) { Timeout = TimeSpan.FromSeconds(3) };
			try
			{
				string awsToken;
				using (var requestMessage = new HttpRequestMessage(HttpMethod.Put, TokenUri))
				{
					requestMessage.Headers.Add("X-aws-ec2-metadata-token-ttl-seconds", "300");
					var responseMessage = await client.SendAsync(requestMessage).ConfigureAwait(false);
					awsToken = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
				}

				JObject metadata;
				using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, MetadataUri))
				{
					requestMessage.Headers.Add("X-aws-ec2-metadata-token", awsToken);
					var responseMessage = await client.SendAsync(requestMessage).ConfigureAwait(false);

					using var stream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
					using var streamReader = new StreamReader(stream, Encoding.UTF8);
					using var jsonReader = new JsonTextReader(streamReader);

					var serializer = new JsonSerializer();
					metadata = serializer.Deserialize<JObject>(jsonReader);
				}

				return new Api.Cloud
				{
					Account = new CloudAccount { Id = metadata["accountId"].Value<string>() },
					Instance = new CloudInstance { Id = metadata["instanceId"].Value<string>() },
					AvailabilityZone = metadata["availabilityZone"]?.Value<string>(),
					Machine = new CloudMachine { Type = metadata["instanceType"].Value<string>() },
					Provider = Provider,
					Region = metadata["region"].Value<string>()
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
