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
	internal struct AzureFunctionsMetaData
	{
		internal bool IsValid { get; set; }
		internal string RegionName { get; set; }
		internal string FunctionsExtensionVersion { get; set; }
		internal string WebsiteSiteName { get; set; }
		internal string WebsiteResourceGroup { get; set; }
		internal string SubscriptionId { get; set; }
		internal string FunctionsWorkerRuntime { get; set; }
	}

	/// <summary>
	/// Provides cloud metadata for Microsoft Azure Functions
	/// </summary>
	internal class AzureFunctionsMetadataProvider : ICloudMetadataProvider
	{
		internal const string Name = "azure-functions";
		private readonly IDictionary _environmentVariables;
		private readonly IApmLogger _logger;

		public AzureFunctionsMetadataProvider(IApmLogger logger, IDictionary environmentVariables)
		{
			_logger = logger;
			_environmentVariables = environmentVariables;
		}

		public string Provider => Name;

		public Task<Api.Cloud> GetMetadataAsync() => Task.FromResult(GetMetadata());

		internal static AzureFunctionsMetaData GetAzureFunctionsMetaData(IApmLogger logger,
			IDictionary environmentVariables = null)
		{
			var helper = new EnvironmentBasedAzureMetadataHelper(Name, logger, environmentVariables);
			var functionsExtensionVersion =
				helper.GetEnvironmentVariable(AzureEnvironmentVariables.FunctionsExtensionVersion);
			var websiteOwnerName = helper.GetEnvironmentVariable(AzureEnvironmentVariables.WebsiteOwnerName);
			var websiteResourceGroup = helper.GetEnvironmentVariable(AzureEnvironmentVariables.WebsiteResourceGroup);
			var websiteSiteName = helper.GetEnvironmentVariable(AzureEnvironmentVariables.WebsiteSiteName);
			var regionName = helper.GetEnvironmentVariable(AzureEnvironmentVariables.RegionName);
			var functionsWorkerRuntime =
				helper.GetEnvironmentVariable(AzureEnvironmentVariables.FunctionsWorkerRuntime);

			if (helper.NullOrEmptyVariable(AzureEnvironmentVariables.FunctionsExtensionVersion,
				    functionsExtensionVersion) ||
			    helper.NullOrEmptyVariable(AzureEnvironmentVariables.WebsiteOwnerName, websiteOwnerName) ||
			    helper.NullOrEmptyVariable(AzureEnvironmentVariables.WebsiteSiteName, websiteSiteName))
				return new AzureFunctionsMetaData { IsValid = false };

			var tokens = helper.TokenizeWebSiteOwnerName(websiteOwnerName);
			if (!tokens.HasValue) return new AzureFunctionsMetaData { IsValid = false };

			if (string.IsNullOrEmpty(regionName))
				regionName = tokens.Value.Region;

			if (string.IsNullOrEmpty(websiteResourceGroup))
				websiteResourceGroup = tokens.Value.ResourceGroup;

			return new AzureFunctionsMetaData
			{
				IsValid = true,
				RegionName = regionName,
				FunctionsExtensionVersion = functionsExtensionVersion,
				FunctionsWorkerRuntime = functionsWorkerRuntime,
				WebsiteSiteName = websiteSiteName,
				WebsiteResourceGroup = websiteResourceGroup,
				SubscriptionId = tokens.Value.SubscriptionId
			};
		}

		private Api.Cloud GetMetadata()
		{
			var data = GetAzureFunctionsMetaData(_logger, _environmentVariables);
			return data.IsValid
				? new Api.Cloud
				{
					Provider = "azure",
					Account = new CloudAccount { Id = data.SubscriptionId },
					Instance = new CloudInstance { Name = data.WebsiteSiteName },
					Project = new CloudProject { Name = data.WebsiteResourceGroup },
					Region = data.RegionName
				}
				: null;
		}
	}
}
