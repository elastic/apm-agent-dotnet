// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Elastic.Apm.Tests.Utilities;
using Newtonsoft.Json;

namespace Elastic.Apm.Azure.Messaging.ServiceBus.Tests.Azure
{
	public class AzureCredentials
	{
		// ReSharper disable InconsistentNaming
		private const string ARM_CLIENT_ID = nameof(ARM_CLIENT_ID);
		private const string ARM_CLIENT_SECRET = nameof(ARM_CLIENT_SECRET);
		private const string ARM_TENANT_ID = nameof(ARM_TENANT_ID);
		private const string ARM_SUBSCRIPTION_ID = nameof(ARM_SUBSCRIPTION_ID);

		private const string CredentialsJsonFile = ".credentials.json";
		// ReSharper restore InconsistentNaming

		private static readonly Lazy<AzureCredentials> _lazyCredentials =
			new Lazy<AzureCredentials>(LoadCredentials, LazyThreadSafetyMode.ExecutionAndPublication);

		[JsonConstructor]
		private AzureCredentials() { }

		private AzureCredentials(string clientId, string clientSecret, string tenantId, string subscriptionId)
		{
			ClientId = clientId;
			ClientSecret = clientSecret;
			TenantId = tenantId;
			SubscriptionId = subscriptionId;
		}

		private static AzureCredentials LoadCredentials()
		{
			var runningInCi = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUILD_ID"));
			if (runningInCi)
			{
				var clientId = GetEnvironmentVariable(ARM_CLIENT_ID);
				var clientSecret = GetEnvironmentVariable(ARM_CLIENT_SECRET);
				var tenantId = GetEnvironmentVariable(ARM_TENANT_ID);
				var subscriptionId = GetEnvironmentVariable(ARM_SUBSCRIPTION_ID);
				return new AzureCredentials(clientId, clientSecret, tenantId, subscriptionId);
			}

			return LoadCredentialsFromFile();
		}

		private static AzureCredentials LoadCredentialsFromFile()
		{
			var path = Path.Combine(SolutionPaths.Root, CredentialsJsonFile);

			if (!File.Exists(path))
				throw new FileNotFoundException($"{CredentialsJsonFile} file does not exist at ${path}", CredentialsJsonFile);

			using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			using var streamReader = new StreamReader(fileStream);
			using var jsonTextReader = new JsonTextReader(streamReader);

			var serializer = new JsonSerializer();
			return serializer.Deserialize<AzureCredentials>(jsonTextReader);
		}

		private static string GetEnvironmentVariable(string name)
		{
			var value = Environment.GetEnvironmentVariable(name);
			if (string.IsNullOrEmpty(value))
				throw new ArgumentException($"{name} environment variable is null or empty");

			return value;
		}

		/// <summary>
		/// A set of Azure credentials obtained from environment variables or a .credentials.json configuration file
		/// </summary>
		public static AzureCredentials Instance => _lazyCredentials.Value;

		[JsonProperty]
		public string ClientId { get; private set; }

		[JsonProperty]
		public string ClientSecret { get; private set; }

		[JsonProperty]
		public string TenantId { get; private set; }

		[JsonProperty]
		public string SubscriptionId { get; private set; }

		public IDictionary<string, string> ToTerraformEnvironmentVariables() =>
			new Dictionary<string, string>
			{
				[ARM_CLIENT_ID] = ClientId,
				[ARM_CLIENT_SECRET] = ClientSecret,
				[ARM_SUBSCRIPTION_ID] = SubscriptionId,
				[ARM_TENANT_ID] = TenantId,
			};
	}
}
