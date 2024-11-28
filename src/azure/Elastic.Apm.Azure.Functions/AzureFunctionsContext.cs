// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading;
using Elastic.Apm.Api;
using Elastic.Apm.Cloud;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Azure.Functions;

internal class AzureFunctionsContext
{
	private static int ColdStart = 1;

	internal AzureFunctionsContext(string loggerScopeName)
	{
		Logger = Agent.Instance.Logger.Scoped(loggerScopeName) ?? Agent.Instance.Logger;
		MetaData = AzureFunctionsMetadataProvider.GetAzureFunctionsMetaData(Logger);
		UpdateServiceInformation(Agent.Instance.Service);
		FaasIdPrefix =
			$"/subscriptions/{MetaData.SubscriptionId}/resourceGroups/{MetaData.WebsiteResourceGroup}/providers/Microsoft.Web/sites/{MetaData.WebsiteSiteName}/functions/";
		Logger.Trace()?.Log("FaasIdPrefix: {FaasIdPrefix}", FaasIdPrefix);
	}

	internal IApmLogger Logger { get; }

	internal AzureFunctionsMetaData MetaData { get; }

	internal string FaasIdPrefix { get; }

	internal static bool IsColdStart() => Interlocked.Exchange(ref ColdStart, 0) == 1;

	private void UpdateServiceInformation(Service? service)
	{
		if (service == null)
		{
			Logger.Warning()?.Log($"{nameof(UpdateServiceInformation)}: service is null");
			return;
		}

		if (service.Name == AbstractConfigurationReader.AdaptServiceName(AbstractConfigurationReader.DiscoverDefaultServiceName()))
		{
			// Only override the service name if it was set to default.
			service.Name = MetaData.WebsiteSiteName;
		}
		service.Framework = new() { Name = "Azure Functions", Version = MetaData.FunctionsExtensionVersion };
		var runtimeVersion = service.Runtime?.Version ?? "n/a";
		service.Runtime = new() { Name = MetaData.FunctionsWorkerRuntime, Version = runtimeVersion };
		service.Node ??= new Node();
		if (!string.IsNullOrEmpty(Agent.Config.ServiceNodeName))
		{
			Logger.Warning()
				?.Log(
					$"The configured {ConfigurationOption.ServiceNodeName.ToEnvironmentVariable()} value '{Agent.Config.ServiceNodeName}' will be overwritten with '{MetaData.WebsiteInstanceId}'");
		}
		service.Node.ConfiguredName = MetaData.WebsiteInstanceId;
	}
}
