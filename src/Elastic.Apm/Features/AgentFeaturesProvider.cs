// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Features
{
	internal static class AgentFeaturesProvider
	{
		private static AgentFeatures AgentFeatures;

		/// <summary>
		/// Returns the agent feature set based on the current habitat (e.g. Azure, AWS Lambda, ...).
		/// </summary>
		/// <returns>The agent features.</returns>
		internal static AgentFeatures Get(IApmLogger logger, IEnvironmentVariables environmentVariables = null)
		{
			if (AgentFeatures == null)
			{
				environmentVariables ??= new EnvironmentVariables(logger);
				if (environmentVariables.GetEnvironmentVariables().Contains("FUNCTIONS_WORKER_RUNTIME"))
					AgentFeatures = new AzureFunctionsAgentFeatures(logger);
				else
					AgentFeatures = new DefaultAgentFeatures(logger);
				logger?.Trace()?.Log($"[Agent Features] Using '{AgentFeatures.Name}' feature set]");
			}
			return AgentFeatures;
		}

		/// <summary>
		/// Meant for testing purposes only.
		/// </summary>
		/// <param name="agentFeatures">The agent features.</param>
		internal static void Set(AgentFeatures agentFeatures) => AgentFeatures = agentFeatures;
	}
}
