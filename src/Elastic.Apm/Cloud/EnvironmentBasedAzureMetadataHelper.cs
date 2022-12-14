// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Cloud
{
	internal static class AzureEnvironmentVariables
	{
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
		internal const string FunctionsWorkerRuntime = "FUNCTIONS_WORKER_RUNTIME";
	}

	/// <summary>
	/// Helper type for Azure metadata providers that operate on environment variables.
	/// </summary>
	internal struct EnvironmentBasedAzureMetadataHelper
	{
		private readonly string _name;
		private readonly IApmLogger _logger;
		private readonly IDictionary _environmentVariables;

		internal EnvironmentBasedAzureMetadataHelper(string name, IApmLogger logger, IDictionary environmentVariables)
		{
			_name = name;
			_logger = logger;
			_environmentVariables = environmentVariables ?? new EnvironmentVariables(_logger).GetEnvironmentVariables();
		}

		internal string GetEnvironmentVariable(string key) =>
			_environmentVariables.Contains(key) ? _environmentVariables[key]?.ToString() : null;

		internal bool NullOrEmptyVariable(string key, string value)
		{
			if (!string.IsNullOrEmpty(value)) return false;

			_logger.Trace()?.Log(
				$"Unable to get '{_name}' cloud metadata as no '{key}' environment variable exists. The application is likely not running in '{_name}'");
			return true;
		}

		internal WebSiteOwnerNameTokens? TokenizeWebSiteOwnerName(string websiteOwnerName)
		{
			var websiteOwnerNameParts = websiteOwnerName.Split('+');
			if (websiteOwnerNameParts.Length != 2)
			{
				_logger.Trace()
					?.Log(
						$"Unable to get '{_name}' cloud metadata as '{AzureEnvironmentVariables.WebsiteOwnerName}' does not contain expected format");
				return null;
			}

			var subscriptionId = websiteOwnerNameParts[0];

			var webSpaceIndex = websiteOwnerNameParts[1].LastIndexOf("webspace", StringComparison.Ordinal);
			if (webSpaceIndex != -1)
				websiteOwnerNameParts[1] = websiteOwnerNameParts[1].Substring(0, webSpaceIndex);

			var lastHyphenIndex = websiteOwnerNameParts[1].LastIndexOf('-');
			if (lastHyphenIndex == -1)
			{
				_logger.Trace()
					?.Log(
						$"Unable to get '{_name}' cloud metadata as '{AzureEnvironmentVariables.WebsiteOwnerName}' does not contain expected format");
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
