// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Elastic.Apm.Features;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using static Elastic.Apm.Config.ConfigConsts;

namespace Elastic.Apm.Cloud
{
	/// <summary>
	/// A collection of <see cref="ICloudMetadataProvider"/> that provide metadata for cloud platforms
	/// </summary>
	public class CloudMetadataProviderCollection : KeyedCollection<string, ICloudMetadataProvider>
	{
		protected override string GetKeyForItem(ICloudMetadataProvider item) => item.Provider;

		public CloudMetadataProviderCollection(string cloudProvider, IApmLogger logger)
			: this(cloudProvider, logger, new EnvironmentVariables(logger))
		{
		}

		internal CloudMetadataProviderCollection(string cloudProvider, IApmLogger logger, IEnvironmentVariables environmentVariables)
		{
			environmentVariables ??= new EnvironmentVariables(logger);
			//
			// First check, whether we are in a specific habitat (e.g. Azure Functions).
			//
			if (AgentFeaturesProvider.Get(logger).Check(AgentFeature.AzureFunctionsCloudMetaDataDiscovery))
			{
				Add(new AzureFunctionsMetadataProvider(logger, environmentVariables.GetEnvironmentVariables()));
				return;
			}
			//
			// Now, let's look at the (generic) configuration.
			//
			switch (cloudProvider?.ToLowerInvariant())
			{
				case SupportedValues.CloudProviderAws:
					Add(new AwsCloudMetadataProvider(logger));
					break;
				case SupportedValues.CloudProviderGcp:
					Add(new GcpCloudMetadataProvider(logger));
					break;
				case SupportedValues.CloudProviderAzure:
				{
					Add(new AzureCloudMetadataProvider(logger));
					var environment = environmentVariables.GetEnvironmentVariables();
					Add(new AzureAppServiceMetadataProvider(logger, environment));
					Add(new AzureFunctionsMetadataProvider(logger, environment));
					break;
				}
				case SupportedValues.CloudProviderNone:
					break;
				case SupportedValues.CloudProviderAuto:
				case "":
				case null:
				{
					// keyed collection is ordered
					Add(new AwsCloudMetadataProvider(logger));
					Add(new GcpCloudMetadataProvider(logger));
					Add(new AzureCloudMetadataProvider(logger));
					var environment = environmentVariables.GetEnvironmentVariables();
					Add(new AzureAppServiceMetadataProvider(logger, environment));
					Add(new AzureFunctionsMetadataProvider(logger, environment));
					break;
				}
				default:
					throw new ArgumentException($"Unknown cloud provider {cloudProvider}", nameof(cloudProvider));
			}
		}

		/// <summary>
		/// Retrieves the cloud metadata for the given provider(s)
		/// </summary>
		/// <returns></returns>
		public async Task<Api.Cloud> GetMetadataAsync()
		{
			foreach (var provider in this)
			{
				var cloud = await provider.GetMetadataAsync().ConfigureAwait(false);
				if (cloud != null)
					return cloud;
			}

			return null;
		}

		public override string ToString() => string.Join(",", Dictionary.Keys);
	}
}
