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
	public class AzureAppServiceMetadataProvider : EnvironmentBasedAzureMetadataProvider
	{
		internal const string Name = "azure-app-service";

		internal AzureAppServiceMetadataProvider(IApmLogger logger, IDictionary environmentVariables) : base(Name,
			logger,
			environmentVariables)
		{
		}

		public override Task<Api.Cloud> GetMetadataAsync()
		{
			var websiteOwnerName = GetEnvironmentVariable(WebsiteOwnerName);
			var websiteResourceGroup = GetEnvironmentVariable(WebsiteResourceGroup);
			var websiteSiteName = GetEnvironmentVariable(WebsiteSiteName);
			var websiteInstanceId = GetEnvironmentVariable(WebsiteInstanceId);

			if (NullOrEmptyVariable(WebsiteOwnerName, websiteOwnerName) ||
				NullOrEmptyVariable(WebsiteResourceGroup, websiteResourceGroup) ||
				NullOrEmptyVariable(WebsiteSiteName, websiteSiteName) ||
				NullOrEmptyVariable(WebsiteInstanceId, websiteInstanceId))
				return Task.FromResult<Api.Cloud>(null);

			var tokens = TokenizeWebSiteOwnerName(websiteOwnerName);
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
