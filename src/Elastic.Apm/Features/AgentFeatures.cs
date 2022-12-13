// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Globalization;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Features
{
	[Flags]
	internal enum AgentFeature
	{
		MetricsCollection = 1 << 0,
		RemoteConfiguration = 1 << 1,
		ContainerInfo = 1 << 2,
		AzureFunctionsCloudMetaDataDiscovery = 1 << 3,
	}
	internal abstract class AgentFeatures
	{
		private readonly IApmLogger _logger;
		private readonly AgentFeature _enabledFeatures;

		internal AgentFeatures(IApmLogger logger, string name, AgentFeature featureMask)
		{
			_logger = logger;
			Name = name;
			_enabledFeatures = featureMask;
		}

		internal bool Check(AgentFeature agentFeature)
		{
			var enabled = (_enabledFeatures & agentFeature) == agentFeature;
			_logger?.Trace()?.Log($"[Agent Feature] '{agentFeature}' enabled: {enabled.ToString(DateTimeFormatInfo.InvariantInfo)}");
			return enabled;
		}

		internal string Name { get;  }
	}

	internal class DefaultAgentFeatures : AgentFeatures
	{
		public DefaultAgentFeatures(IApmLogger logger) :
			base(logger, "Default",
				AgentFeature.MetricsCollection |
				AgentFeature.RemoteConfiguration |
				AgentFeature.ContainerInfo)
		{
		}
	}

	internal class AzureFunctionsAgentFeatures : AgentFeatures
	{
		internal AzureFunctionsAgentFeatures(IApmLogger logger) :
			base(logger, "Azure Functions", AgentFeature.AzureFunctionsCloudMetaDataDiscovery)
		{
		}
	}
}
