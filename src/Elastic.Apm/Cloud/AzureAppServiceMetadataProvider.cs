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
	/// Provides cloud metadata for Microsoft Azure App Services
	/// </summary>
	public class AzureAppServiceMetadataProvider : ICloudMetadataProvider
	{
		internal const string Name = "azure-app-service";
		private readonly IDictionary _environmentVariables;
		private readonly IApmLogger _logger;

		internal AzureAppServiceMetadataProvider(IApmLogger logger, IDictionary environmentVariables)
		{
			_logger = logger;
			_environmentVariables = environmentVariables;
		}

		public string Provider => Name;

		public Task<Api.Cloud> GetMetadataAsync()
		{
			var helper = new EnvironmentBasedAzureMetadataHelper(Provider, _logger, _environmentVariables);
			var websiteOwnerName = helper.GetEnvironmentVariable(AzureEnvironmentVariables.WebsiteOwnerName);
			var websiteResourceGroup = helper.GetEnvironmentVariable(AzureEnvironmentVariables.WebsiteResourceGroup);
			var websiteSiteName = helper.GetEnvironmentVariable(AzureEnvironmentVariables.WebsiteSiteName);
			var websiteInstanceId = helper.GetEnvironmentVariable(AzureEnvironmentVariables.WebsiteInstanceId);

			if (helper.NullOrEmptyVariable(AzureEnvironmentVariables.WebsiteOwnerName, websiteOwnerName) ||
			    helper.NullOrEmptyVariable(AzureEnvironmentVariables.WebsiteResourceGroup, websiteResourceGroup) ||
			    helper.NullOrEmptyVariable(AzureEnvironmentVariables.WebsiteSiteName, websiteSiteName) ||
			    helper.NullOrEmptyVariable(AzureEnvironmentVariables.WebsiteInstanceId, websiteInstanceId))
				return Task.FromResult<Api.Cloud>(null);

			var tokens = helper.TokenizeWebSiteOwnerName(websiteOwnerName);
			if (!tokens.HasValue)
				return Task.FromResult<Api.Cloud>(null);

			return Task.FromResult(new Api.Cloud
			{
				Account = new CloudAccount { Id = tokens.Value.SubscriptionId },
				Instance = new CloudInstance { Id = websiteInstanceId, Name = websiteSiteName },
				Project = new CloudProject { Name = websiteResourceGroup },
				Provider = "azure",
				Region = tokens.Value.Region
			});
		}
	}
}
