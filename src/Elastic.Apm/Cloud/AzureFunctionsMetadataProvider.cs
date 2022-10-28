// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Cloud
{
	/// <summary>
	/// Provides cloud metadata for Microsoft Azure Functions
	/// </summary>
	internal class AzureFunctionsMetadataProvider : EnvironmentBasedAzureMetadataProvider
	{
		internal const string Name = "azure-functions";

		public AzureFunctionsMetadataProvider(IApmLogger logger, IDictionary environmentVariables) : base(Name, logger,
			environmentVariables)
		{
		}

		public override Task<Api.Cloud> GetMetadataAsync()
		{
			var functionsExtensionVersion = GetEnvironmentVariable(FunctionsExtensionVersion);
			var websiteOwnerName = GetEnvironmentVariable(WebsiteOwnerName);
			var websiteResourceGroup = GetEnvironmentVariable(WebsiteResourceGroup);
			var websiteSiteName = GetEnvironmentVariable(WebsiteSiteName);
			var regionName = GetEnvironmentVariable(RegionName);

			if (NullOrEmptyVariable(FunctionsExtensionVersion, functionsExtensionVersion) ||
			    NullOrEmptyVariable(WebsiteOwnerName, websiteOwnerName) ||
			    NullOrEmptyVariable(WebsiteSiteName, websiteSiteName))
				return Task.FromResult<Api.Cloud>(null);

			var tokens = TokenizeWebSiteOwnerName(websiteOwnerName);
			if (!tokens.HasValue) return Task.FromResult<Api.Cloud>(null);

			if (string.IsNullOrEmpty(regionName))
				regionName = tokens.Value.Region;

			if (string.IsNullOrEmpty(websiteResourceGroup))
				websiteResourceGroup = tokens.Value.ResourceGroup;

			return Task.FromResult(new Api.Cloud
			{
				Provider = "azure",
				Account = new CloudAccount { Id = tokens.Value.SubscriptionId },
				Instance = new CloudInstance { Name = websiteSiteName },
				Project = new CloudProject { Name = websiteResourceGroup },
				Region = regionName
			});
		}
	}
}
