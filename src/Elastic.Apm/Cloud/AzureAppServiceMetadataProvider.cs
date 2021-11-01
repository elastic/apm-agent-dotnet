// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Cloud
{
	/// <summary>
	/// Provides cloud metadata for Microsoft Azure App Services
	/// </summary>
	public class AzureAppServiceMetadataProvider : ICloudMetadataProvider
	{
		internal const string Name = "azure-app-service";

		private readonly IApmLogger _logger;
		private readonly IDictionary _environmentVariables;

		/// <summary>
		/// Value of the form {subscription id}+{app service plan resource group}-{region}webspace
		/// </summary>
		/// <example>
		/// f5940f10-2e30-3e4d-a259-63451ba6dae4+elastic-apm-AustraliaEastwebspace
		/// </example>
		internal static readonly string WebsiteOwnerName = "WEBSITE_OWNER_NAME";

		internal static readonly string WebsiteResourceGroup = "WEBSITE_RESOURCE_GROUP";

		internal static readonly string WebsiteSiteName = "WEBSITE_SITE_NAME";

		internal static readonly string WebsiteInstanceId = "WEBSITE_INSTANCE_ID";

		private static readonly string Webspace = "webspace";

		public AzureAppServiceMetadataProvider(IApmLogger logger, IDictionary environmentVariables)
		{
			_logger = logger;
			_environmentVariables = environmentVariables;
		}

		public string Provider { get; } = Name;

		public Task<Api.Cloud> GetMetadataAsync()
		{
			if (_environmentVariables is null)
			{
				_logger.Trace()?.Log("Unable to get {Provider} cloud metadata as no environment variables available", Provider);
				return Task.FromResult<Api.Cloud>(null);
			}

			var websiteOwnerName = GetEnvironmentVariable(WebsiteOwnerName);
			var websiteResourceGroup = GetEnvironmentVariable(WebsiteResourceGroup);
			var websiteSiteName = GetEnvironmentVariable(WebsiteSiteName);
			var websiteInstanceId = GetEnvironmentVariable(WebsiteInstanceId);

			bool NullOrEmptyVariable(string key, string value)
			{
				if (!string.IsNullOrEmpty(value)) return false;

				_logger.Trace()?.Log(
					"Unable to get {Provider} cloud metadata as no {EnvironmentVariable} environment variable exists. The application is likely not running in {Provider}",
					Provider,
					key,
					Provider);

				return true;
			}

			if (NullOrEmptyVariable(WebsiteOwnerName, websiteOwnerName) ||
				NullOrEmptyVariable(WebsiteResourceGroup, websiteResourceGroup) ||
				NullOrEmptyVariable(WebsiteSiteName, websiteSiteName) ||
				NullOrEmptyVariable(WebsiteInstanceId, websiteInstanceId))
				return Task.FromResult<Api.Cloud>(null);

			var websiteOwnerNameParts = websiteOwnerName.Split('+');
			if (websiteOwnerNameParts.Length != 2)
			{
				_logger.Trace()?.Log(
					"Unable to get {Provider} cloud metadata as {EnvironmentVariable} does not contain expected format",
					Provider,
					WebsiteOwnerName);
				return Task.FromResult<Api.Cloud>(null);
			}

			var subscriptionId = websiteOwnerNameParts[0];

			var webspaceIndex = websiteOwnerNameParts[1].LastIndexOf(Webspace, StringComparison.Ordinal);
			if (webspaceIndex != -1)
				websiteOwnerNameParts[1] = websiteOwnerNameParts[1].Substring(0, webspaceIndex);

			var lastHyphenIndex = websiteOwnerNameParts[1].LastIndexOf('-');
			if (lastHyphenIndex == -1)
			{
				_logger.Trace()?.Log(
					"Unable to get {Provider} cloud metadata as {EnvironmentVariable} does not contain expected format",
					Provider,
					WebsiteOwnerName);
				return Task.FromResult<Api.Cloud>(null);
			}

			var region = websiteOwnerNameParts[1].Substring(lastHyphenIndex + 1);

			return Task.FromResult(new Api.Cloud
			{
				Account = new CloudAccount { Id = subscriptionId },
				Instance = new CloudInstance { Id = websiteInstanceId, Name = websiteSiteName },
				Project = new CloudProject { Name = websiteResourceGroup },
				Provider = "azure",
				Region = region
			});
		}

		private string GetEnvironmentVariable(string key) =>
			_environmentVariables.Contains(key)
				? _environmentVariables[key]?.ToString()
				: null;
	}
}
