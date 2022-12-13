// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Features
{
	[Flags]
	internal enum AgentFeature
	{
		None = 0,
		MetricsCollection = 1 << 0,
		RemoteConfiguration = 1 << 1,
		CloudMetaDataDiscovery = 1 << 2,
		ContainerInfo = 1 << 3,
	}
	internal class AgentFeatures
	{
		private readonly IApmLogger _logger;
		private readonly AgentFeature _enabledFeatures;

		internal AgentFeatures(IApmLogger logger)
		{
			_logger = logger;
			_enabledFeatures |= AgentFeature.MetricsCollection;
			_enabledFeatures |= AgentFeature.RemoteConfiguration;
			_enabledFeatures |= AgentFeature.CloudMetaDataDiscovery;
			_enabledFeatures |= AgentFeature.ContainerInfo;
		}

		internal bool Check(AgentFeature agentFeature)
		{
			var enabled = (_enabledFeatures & agentFeature) == agentFeature;
			_logger?.Trace()?.Log($"[Agent Feature] '{agentFeature}' enabled: {enabled}");
			return enabled;
		}
	}
}
