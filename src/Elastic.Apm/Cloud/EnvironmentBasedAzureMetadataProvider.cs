// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections;
using System.Threading.Tasks;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Cloud
{
	/// <summary>
	/// Base blass for Azure metadata providers that operate on environment variables.
	/// </summary>
	public abstract class EnvironmentBasedAzureMetadataProvider : ICloudMetadataProvider
	{
		protected readonly IApmLogger _logger;
		protected readonly IDictionary _environmentVariables;

		/// <summary>
		/// Value of the form {subscription id}+{app service plan resource group}-{region}webspace
		/// </summary>
		/// <example>
		/// f5940f10-2e30-3e4d-a259-63451ba6dae4+elastic-apm-AustraliaEastwebspace
		/// </example>
		internal const string WebsiteOwnerName = "WEBSITE_OWNER_NAME";
		internal const string WebsiteResourceGroup = "WEBSITE_RESOURCE_GROUP";
		internal const string WebsiteSiteName = "WEBSITE_SITE_NAME";
		internal const string WebsiteInstanceId = "WEBSITE_INSTANCE_ID";
		internal const string RegionName = "REGION_NAME";
		internal const string FunctionsExtensionVersion = "FUNCTIONS_EXTENSION_VERSION";

		public string Provider { get; }

		public abstract Task<Api.Cloud> GetMetadataAsync();

		protected EnvironmentBasedAzureMetadataProvider(string name, IApmLogger logger,
			IDictionary environmentVariables)
		{
			Provider = name;
			_logger = logger;
			_environmentVariables = environmentVariables;

			if (_environmentVariables is null)
			{
				_logger.Trace()?.Log("Unable to get {Provider} cloud metadata as no environment variables available",
					Provider);
			}
		}

		protected string GetEnvironmentVariable(string key) =>
			_environmentVariables != null && _environmentVariables.Contains(key)
				? _environmentVariables[key]?.ToString()
				: null;

		protected bool NullOrEmptyVariable(string key, string value)
		{
			if (!string.IsNullOrEmpty(value)) return false;

			_logger.Trace()?.Log(
				"Unable to get {Provider} cloud metadata as no {EnvironmentVariable} environment variable exists. The application is likely not running in {Provider}",
				Provider,
				key,
				Provider);

			return true;
		}

		internal WebSiteOwnerNameTokens? TokenizeWebSiteOwnerName(string websiteOwnerName)
		{
			var websiteOwnerNameParts = websiteOwnerName.Split('+');
			if (websiteOwnerNameParts.Length != 2)
			{
				_logger.Trace()?.Log(
					"Unable to get {Provider} cloud metadata as {EnvironmentVariable} does not contain expected format",
					Provider,
					WebsiteOwnerName);
				return null;
			}

			var subscriptionId = websiteOwnerNameParts[0];

			var webSpaceIndex = websiteOwnerNameParts[1].LastIndexOf("webspace", StringComparison.Ordinal);
			if (webSpaceIndex != -1)
				websiteOwnerNameParts[1] = websiteOwnerNameParts[1].Substring(0, webSpaceIndex);

			var lastHyphenIndex = websiteOwnerNameParts[1].LastIndexOf('-');
			if (lastHyphenIndex == -1)
			{
				_logger.Trace()?.Log(
					"Unable to get {Provider} cloud metadata as {EnvironmentVariable} does not contain expected format",
					Provider,
					WebsiteOwnerName);
				return null;
			}

			var region = websiteOwnerNameParts[1].Substring(lastHyphenIndex + 1);
			var resourceGroup = websiteOwnerNameParts[1].Substring(0, lastHyphenIndex);

			return new WebSiteOwnerNameTokens(subscriptionId, resourceGroup, region);
		}
	}

	internal struct WebSiteOwnerNameTokens
	{
		internal WebSiteOwnerNameTokens(string subscriptionId, string resourceGroup, string region)
		{
			SubscriptionId = subscriptionId;
			ResourceGroup = resourceGroup;
			Region = region;
		}

		internal string Region { get; }

		internal string ResourceGroup { get; }

		internal string SubscriptionId { get; }
	}
}
